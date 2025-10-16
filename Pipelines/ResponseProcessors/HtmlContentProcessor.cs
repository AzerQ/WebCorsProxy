using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace ProxyServer.Pipelines.ResponseProcessors;

/// <summary>
/// Обработчик HTML контента
/// </summary>
public class HtmlContentProcessor : IResponseProcessor
{
    private readonly ILogger<HtmlContentProcessor> _logger;

    public int Order => 2;

    public HtmlContentProcessor(ILogger<HtmlContentProcessor> logger)
    {
        _logger = logger;
    }

    public async Task ProcessAsync(ResponseProcessingContext context, CancellationToken cancellationToken = default)
    {
        if (context.ContentType?.Contains("text/html") != true || string.IsNullOrEmpty(context.Content))
        {
            await Task.CompletedTask;
            return;
        }

        try
        {
            var processedContent = ProcessHtmlContent(context.Content, context.TargetUrl, context.Token);
            context.Content = processedContent;
            context.ContentModified = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing HTML content");
        }

        await Task.CompletedTask;
    }

    private string ProcessHtmlContent(string htmlContent, string baseUrl, string? token)
    {
        htmlContent = ProxyAllHttpLinks(htmlContent, token);
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

        // Process inline scripts
        var scriptNodes = doc.DocumentNode.SelectNodes("//script[not(@src)]");
        if (scriptNodes != null)
        {
            foreach (var scriptNode in scriptNodes)
            {
                scriptNode.InnerHtml = ProcessJsContent(scriptNode.InnerHtml, baseUrl, token);
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
}