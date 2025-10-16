using System.Net.Http;
using System.Text;
using HtmlAgilityPack;
using Microsoft.AspNetCore.Cors;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddHttpClient("", client =>
{
    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = System.Net.DecompressionMethods.All
});

var app = builder.Build();

var logger = app.Logger;

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseStaticFiles();

app.MapGet("/", () => Results.Redirect("/index.html"));

string[] GetApiKeys(IConfiguration configuration)
{
    var apiKeysSection = configuration.GetSection("ApiKeys");
    return apiKeysSection.GetChildren().Select(c => c.Value ?? string.Empty).Where(v => !string.IsNullOrEmpty(v)).ToArray();
}

bool IsAuthorized(HttpContext context, string[] apiKeys, string? tokenParam = null)
{
    string? token = null;

    // Check Authorization header first
    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
    {
        token = authHeader.Substring("Bearer ".Length);
    }

    // If no token in header, check token parameter
    if (string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(tokenParam))
    {
        token = tokenParam;
    }

    return !string.IsNullOrEmpty(token) && apiKeys.Contains(token);
}

async Task<IResult> ProxyRequest(HttpClient httpClient, HttpContext context, string url, string[] apiKeys, string? token = null)
{
    if (!IsAuthorized(context, apiKeys, token))
    {
        return Results.Unauthorized();
    }

    if (string.IsNullOrEmpty(url))
    {
        return Results.BadRequest("URL parameter is required");
    }

    int? responseStatusCode = null;
    try
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);

        // Copy headers from original request
        foreach (var header in context.Request.Headers)
        {
            if (!header.Key.StartsWith("Host") && !header.Key.StartsWith("Origin") && !header.Key.StartsWith("Authorization"))
            {
                request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToString());
            }
        }

        var response = await httpClient.SendAsync(request);
        responseStatusCode = (int)response.StatusCode;

        // Don't ensure success status code, handle all responses
        var contentType = response.Content.Headers.ContentType?.MediaType;

        // For binary content or compressed content, return as stream without processing
        if (contentType == null || (!contentType.Contains("text/html") && !contentType.Contains("text/plain") && !contentType.Contains("application/json")))
        {
            var contentStream = await response.Content.ReadAsStreamAsync();

            // Copy content encoding header if present
            if (response.Content.Headers.ContentEncoding.Any())
            {
                context.Response.Headers.ContentEncoding = string.Join(", ", response.Content.Headers.ContentEncoding);
            }

            context.Response.Headers.AccessControlAllowOrigin = "*";
            context.Response.Headers.AccessControlAllowMethods = "*";
            context.Response.Headers.AccessControlAllowHeaders = "*";

            context.Response.StatusCode = responseStatusCode.Value;
            return Results.Stream(contentStream, contentType ?? "application/octet-stream");
        }

        // For text content, read as string and process if HTML
        var contentStreamText = await response.Content.ReadAsStreamAsync();
        string content;
        using (var reader = new StreamReader(contentStreamText, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
        {
            content = await reader.ReadToEndAsync();
        }

        if (contentType?.Contains("text/html") == true)
        {
            content = ProcessHtmlContent(content, url);
        }

        context.Response.Headers.AccessControlAllowOrigin = "*";
        context.Response.Headers.AccessControlAllowMethods = "*";
        context.Response.Headers.AccessControlAllowHeaders = "*";

        return Results.Content(content, contentType ?? "text/plain", statusCode: responseStatusCode);
    }
    catch (Exception ex)
    {
        logger.LogInformation("Response status code " + responseStatusCode);
        return Results.Problem($"Error proxying request: {ex.Message}", statusCode:responseStatusCode);
    }
}

string ProcessHtmlContent(string htmlContent, string baseUrl)
{
    var doc = new HtmlDocument();
    doc.LoadHtml(htmlContent);

    // Rewrite links
    var links = doc.DocumentNode.SelectNodes("//a[@href]");
    if (links != null)
    {
        foreach (var link in links)
        {
            var href = link.GetAttributeValue("href", "");
            if (!string.IsNullOrEmpty(href) && !href.StartsWith("http") && !href.StartsWith("#"))
            {
                link.SetAttributeValue("href", $"/web?url={Uri.EscapeDataString(new Uri(new Uri(baseUrl), href).AbsoluteUri)}");
            }
        }
    }

    // Inject JS to override fetch
    var script = @"
<script>
(function() {
    const originalFetch = window.fetch;
    window.fetch = function(input, init) {
        if (typeof input === 'string' && input.startsWith('/')) {
            input = window.location.origin + input;
        }
        return originalFetch.call(this, input, init);
    };
})();
</script>
";
    var head = doc.DocumentNode.SelectSingleNode("//head");
    if (head != null)
    {
        head.AppendChild(HtmlNode.CreateNode(script));
    }

    return doc.DocumentNode.OuterHtml;
}

var apiKeys = GetApiKeys(app.Configuration);

app.MapGet("/web", async (HttpClient httpClient, HttpContext context, string url, string? token) =>
{
    return await ProxyRequest(httpClient, context, url, apiKeys, token);
});

app.Run();
