using System.Text.RegularExpressions;

namespace ProxyServer.Pipelines.ResponseProcessors;

/// <summary>
/// Обработчик JavaScript контента
/// </summary>
public class JsContentProcessor : IResponseProcessor
{
    private readonly ILogger<JsContentProcessor> _logger;

    public int Order => 2;

    public JsContentProcessor(ILogger<JsContentProcessor> logger)
    {
        _logger = logger;
    }

    public async Task ProcessAsync(ResponseProcessingContext context, CancellationToken cancellationToken = default)
    {
        if ((context.ContentType?.Contains("javascript") != true && 
             context.ContentType?.Contains("application/json") != true) || 
            string.IsNullOrEmpty(context.Content))
        {
            await Task.CompletedTask;
            return;
        }

        try
        {
            var processedContent = ProcessJsContent(context.Content, context.TargetUrl, context.Token);
            context.Content = processedContent;
            context.ContentModified = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing JavaScript content");
        }

        await Task.CompletedTask;
    }

    private string ProcessJsContent(string jsContent, string baseUrl, string? token)
    {
        jsContent = ProxyAllHttpLinks(jsContent, token);

        // Replace location.origin with the original URL's origin
        var originalUri = new Uri(baseUrl);
        var originalOrigin = originalUri.GetLeftPart(UriPartial.Authority);
        var locationOriginRegex = new Regex(@"\blocation\.origin\b");
        jsContent = locationOriginRegex.Replace(jsContent, $"\"{originalOrigin}\"");

        // Regex to find import("...") or import '...'
        var importRegex = new Regex(@"import\((['""])([^'""]+)\1\)", RegexOptions.IgnoreCase);

        return importRegex.Replace(jsContent, match =>
        {
            var urlValue = match.Groups[2].Value;
            if (string.IsNullOrEmpty(urlValue) || urlValue.StartsWith("http") || urlValue.StartsWith("#") || urlValue.StartsWith("data:"))
            {
                return match.Value;
            }

            try
            {
                var absoluteUri = new Uri(new Uri(baseUrl), urlValue).AbsoluteUri;
                var tokenParam = string.IsNullOrEmpty(token) ? "" : $"&token={token}";
                var proxyUrl = $"/web?url={Uri.EscapeDataString(absoluteUri)}{tokenParam}";
                return $"import('{proxyUrl}')";
            }
            catch (UriFormatException)
            {
                return match.Value;
            }
        });
    }

    private string ProxyAllHttpLinks(string content, string? token)
    {
        // This regex finds absolute http/https links, ensuring they are not already part of an HTML attribute we've rewritten.
        // It looks for URLs in contexts like strings ('"...http...") or just plain text.
        var httpRegex = new Regex(@"([""'])(https?:\/\/[^""']+)\1|(?<!=)(https?:\/\/[^\s""'<`]+)", RegexOptions.IgnoreCase);

        return httpRegex.Replace(content, match =>
        {
            // Group 2 and 3 capture the URL depending on whether it was in quotes or not.
            var urlValue = match.Groups[2].Success ? match.Groups[2].Value : match.Groups[3].Value;
            
            if (string.IsNullOrEmpty(urlValue))
            {
                return match.Value;
            }

            var tokenParam = string.IsNullOrEmpty(token) ? "" : $"&token={token}";
            var proxyUrl = $"/web?url={Uri.EscapeDataString(urlValue)}{tokenParam}";

            // Reconstruct the original context (e.g., quotes) if it existed.
            if (match.Groups[1].Success)
            {
                return $"{match.Groups[1].Value}{proxyUrl}{match.Groups[1].Value}";
            }
            
            return proxyUrl;
        });
    }
}