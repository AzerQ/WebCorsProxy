using System.Text.RegularExpressions;
using System.Text;
using HtmlAgilityPack;

namespace ProxyServer;

public class ProxyService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProxyService> _logger;
    private readonly string[] _apiKeys;

    public ProxyService(IHttpClientFactory httpClientFactory, IConfiguration configuration, ILogger<ProxyService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _apiKeys = configuration.GetSection("ApiKeys").GetChildren().Select(c => c.Value ?? string.Empty).Where(v => !string.IsNullOrEmpty(v)).ToArray();
    }

    public async Task<IResult> ProxyRequestAsync(HttpContext context, string url, string? token)
    {
        if (!IsAuthorized(context, token))
        {
            return Results.Unauthorized();
        }

        if (string.IsNullOrEmpty(url))
        {
            return Results.BadRequest("URL parameter is required");
        }
        
        var httpClient = _httpClientFactory.CreateClient();
        int? responseStatusCode = null;
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            foreach (var header in context.Request.Headers)
            {
                if (!header.Key.StartsWith("Host") && !header.Key.StartsWith("Origin") && !header.Key.StartsWith("Authorization"))
                {
                    request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToString());
                }
            }

            var response = await httpClient.SendAsync(request);
            responseStatusCode = (int)response.StatusCode;
            
            var contentType = response.Content.Headers.ContentType?.MediaType;

            if (contentType == null || (!contentType.Contains("text/html") && !contentType.Contains("text/plain") && !contentType.Contains("application/json") && !contentType.Contains("text/css")))
            {
                var contentStream = await response.Content.ReadAsStreamAsync();
                
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

            var contentStreamText = await response.Content.ReadAsStreamAsync();
            string content;
            using (var reader = new StreamReader(contentStreamText, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true))
            {
                content = await reader.ReadToEndAsync();
            }

            if (contentType?.Contains("text/html") == true)
            {
                content = ProcessHtmlContent(content, url, token);
            }
            else if (contentType?.Contains("text/css") == true)
            {
                content = ProcessCssContent(content, url, token);
            }

            context.Response.Headers.AccessControlAllowOrigin = "*";
            context.Response.Headers.AccessControlAllowMethods = "*";
            context.Response.Headers.AccessControlAllowHeaders = "*";

            return Results.Content(content, contentType ?? "text/plain", statusCode: responseStatusCode);
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Response status code " + responseStatusCode);
            return Results.Problem($"Error proxying request: {ex.Message}", statusCode: responseStatusCode);
        }
    }

    private bool IsAuthorized(HttpContext context, string? tokenParam = null)
    {
        string? token = null;

        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            token = authHeader.Substring("Bearer ".Length);
        }

        if (string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(tokenParam))
        {
            token = tokenParam;
        }

        return !string.IsNullOrEmpty(token) && _apiKeys.Contains(token);
    }

    private string ProcessHtmlContent(string htmlContent, string baseUrl, string? token)
    {
        var doc = new HtmlDocument();
        doc.LoadHtml(htmlContent);
    
        var urlAttributes = new[] { "href", "src" };
        foreach (var attributeName in urlAttributes)
        {
            var nodes = doc.DocumentNode.SelectNodes($"//*[@{attributeName}]");
            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    var urlValue = node.GetAttributeValue(attributeName, "");
                    if (string.IsNullOrEmpty(urlValue) || urlValue.StartsWith("#") || urlValue.StartsWith("data:"))
                    {
                        continue;
                    }
    
                    try
                    {
                        var absoluteUri = new Uri(new Uri(baseUrl), urlValue).AbsoluteUri;
                        var tokenParam = string.IsNullOrEmpty(token) ? "" : $"&token={token}";
                        var proxyUrl = $"/web?url={Uri.EscapeDataString(absoluteUri)}{tokenParam}";
                        node.SetAttributeValue(attributeName, proxyUrl);
                    }
                    catch (UriFormatException)
                    {
                        // Ignore malformed URLs
                    }
                }
            }
        }
    
        var styleNodes = doc.DocumentNode.SelectNodes("//style");
        if (styleNodes != null)
        {
            foreach (var styleNode in styleNodes)
            {
                styleNode.InnerHtml = ProcessCssContent(styleNode.InnerHtml, baseUrl, token);
            }
        }
    
        var inlineStyleNodes = doc.DocumentNode.SelectNodes("//*[@style]");
        if (inlineStyleNodes != null)
        {
            foreach (var node in inlineStyleNodes)
            {
                var styleAttribute = node.Attributes["style"];
                styleAttribute.Value = ProcessCssContent(styleAttribute.Value, baseUrl, token);
            }
        }
    
        var tokenJs = token ?? "";
        var script = @"
    <script>
    (function() {
        const token = '" + tokenJs + @"';
        const proxyBase = `${window.location.origin}/web`;
    
        function getProxiedUrl(originalUrl) {
            const absoluteUrl = new URL(originalUrl, document.baseURI).href;
            const tokenParam = token ? `&token=${token}` : '';
            return `${proxyBase}?url=${encodeURIComponent(absoluteUrl)}${tokenParam}`;
        }
    
        const originalFetch = window.fetch;
        window.fetch = function(input, init) {
            let url = input instanceof Request ? input.url : input;
            const proxiedUrl = getProxiedUrl(url);
            
            if (input instanceof Request) {
                input = new Request(proxiedUrl, {
                    ...input,
                });
            } else {
                input = proxiedUrl;
            }
    
            return originalFetch.call(this, input, init);
        };
    
        const originalXhrOpen = window.XMLHttpRequest.prototype.open;
        window.XMLHttpRequest.prototype.open = function(method, url, async, user, password) {
            const proxiedUrl = getProxiedUrl(url);
            return originalXhrOpen.call(this, method, proxiedUrl, async, user, password);
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

    private string ProcessCssContent(string cssContent, string baseUrl, string? token)
    {
        var urlRegex = new Regex(@"url\((?!['""]?data:)([^)]+)\)", RegexOptions.IgnoreCase);
    
        return urlRegex.Replace(cssContent, match =>
        {
            var urlValue = match.Groups[1].Value.Trim('"', '\'');
            if (string.IsNullOrEmpty(urlValue) || urlValue.StartsWith("#"))
            {
                return match.Value;
            }
    
            try
            {
                var absoluteUri = new Uri(new Uri(baseUrl), urlValue).AbsoluteUri;
                var tokenParam = string.IsNullOrEmpty(token) ? "" : $"&token={token}";
                return $"url(/web?url={Uri.EscapeDataString(absoluteUri)}{tokenParam})";
            }
            catch (UriFormatException)
            {
                return match.Value;
            }
        });
    }
}