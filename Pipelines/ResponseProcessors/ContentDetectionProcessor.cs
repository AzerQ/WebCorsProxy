namespace ProxyServer.Pipelines.ResponseProcessors;

/// <summary>
/// Обработчик определения типа контента и подготовки к обработке
/// </summary>
public class ContentDetectionProcessor : IResponseProcessor
{
    private readonly ILogger<ContentDetectionProcessor> _logger;

    public int Order => 0;

    public ContentDetectionProcessor(ILogger<ContentDetectionProcessor> logger)
    {
        _logger = logger;
    }

    public async Task ProcessAsync(ResponseProcessingContext context, CancellationToken cancellationToken = default)
    {
        // Определяем Content-Type
        context.ContentType = context.ProxyResponse.Content.Headers.ContentType?.MediaType;

        // Проверяем, нужно ли обрабатывать контент
        if (ShouldProcessAsText(context.ContentType))
        {
            // Для текстового контента читаем как строку
            var contentStream = await context.ProxyResponse.Content.ReadAsStreamAsync(cancellationToken);
            contentStream.Position = 0;
            
            using var reader = new StreamReader(contentStream, System.Text.Encoding.UTF8, 
                detectEncodingFromByteOrderMarks: true, leaveOpen: true);
            context.Content = await reader.ReadToEndAsync();
            contentStream.Position = 0;
        }
        else
        {
            // Для бинарного контента оставляем как поток
            context.ContentStream = await context.ProxyResponse.Content.ReadAsStreamAsync(cancellationToken);
        }

        await Task.CompletedTask;
    }

    private static bool ShouldProcessAsText(string? contentType)
    {
        if (string.IsNullOrEmpty(contentType))
            return false;

        var textContentTypes = new[]
        {
            "text/html",
            "text/plain",
            "text/css",
            "application/json",
            "application/javascript",
            "text/javascript",
            "application/xml",
            "text/xml"
        };

        return textContentTypes.Any(type => contentType.Contains(type, StringComparison.OrdinalIgnoreCase));
    }
}