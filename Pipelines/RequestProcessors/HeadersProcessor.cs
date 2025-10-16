namespace ProxyServer.Pipelines.RequestProcessors;

/// <summary>
/// Обработчик заголовков запроса
/// </summary>
public class HeadersProcessor : IRequestProcessor
{
    private readonly ILogger<HeadersProcessor> _logger;

    public int Order => 2;

    public HeadersProcessor(ILogger<HeadersProcessor> logger)
    {
        _logger = logger;
    }

    public async Task ProcessAsync(RequestProcessingContext context, CancellationToken cancellationToken = default)
    {
        // Копируем заголовки из оригинального запроса, исключая некоторые системные
        foreach (var header in context.HttpContext.Request.Headers)
        {
            if (!ShouldSkipHeader(header.Key))
            {
                context.ProxyRequest.Headers.TryAddWithoutValidation(header.Key, header.Value.ToString());
            }
        }

        await Task.CompletedTask;
    }

    private static bool ShouldSkipHeader(string headerName)
    {
        // Пропускаем заголовки, которые могут вызвать проблемы при проксировании
        var skippedHeaders = new[]
        {
            "Host",
            "Origin",
            "Authorization",
            "Content-Length",
            "Transfer-Encoding"
        };

        return skippedHeaders.Any(skip => headerName.StartsWith(skip, StringComparison.OrdinalIgnoreCase));
    }
}