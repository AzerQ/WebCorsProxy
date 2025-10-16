namespace ProxyServer.Pipelines.ResponseProcessors;

/// <summary>
/// Обработчик заголовков ответа
/// </summary>
public class HeadersResponseProcessor : IResponseProcessor
{
    private readonly ILogger<HeadersResponseProcessor> _logger;

    public int Order => 1;

    public HeadersResponseProcessor(ILogger<HeadersResponseProcessor> logger)
    {
        _logger = logger;
    }

    public async Task ProcessAsync(ResponseProcessingContext context, CancellationToken cancellationToken = default)
    {
        // Устанавливаем CORS заголовки
        context.HttpContext.Response.Headers.AccessControlAllowOrigin = "*";
        context.HttpContext.Response.Headers.AccessControlAllowMethods = "*";
        context.HttpContext.Response.Headers.AccessControlAllowHeaders = "*";

        // Копируем заголовки кодировки, если они есть
        if (context.ProxyResponse.Content.Headers.ContentEncoding.Any())
        {
            context.HttpContext.Response.Headers.ContentEncoding = 
                string.Join(", ", context.ProxyResponse.Content.Headers.ContentEncoding);
        }

        await Task.CompletedTask;
    }
}