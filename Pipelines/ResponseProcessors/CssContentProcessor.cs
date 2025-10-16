using System.Text.RegularExpressions;

namespace ProxyServer.Pipelines.ResponseProcessors;

/// <summary>
/// Обработчик CSS контента
/// </summary>
public class CssContentProcessor : IResponseProcessor
{
    private readonly ILogger<CssContentProcessor> _logger;

    public int Order => 2;

    public CssContentProcessor(ILogger<CssContentProcessor> logger)
    {
        _logger = logger;
    }

    public async Task ProcessAsync(ResponseProcessingContext context, CancellationToken cancellationToken = default)
    {
        if (context.ContentType?.Contains("text/css") != true || string.IsNullOrEmpty(context.Content))
        {
            await Task.CompletedTask;
            return;
        }

        try
        {
            var processedContent = ProcessCssContent(context.Content, context.TargetUrl, context.Token);
            context.Content = processedContent;
            context.ContentModified = true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing CSS content");
        }

        await Task.CompletedTask;
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