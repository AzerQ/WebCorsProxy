namespace ProxyServer.Pipelines.RequestProcessors;

/// <summary>
/// Обработчик валидации запроса
/// </summary>
public class ValidationProcessor : IRequestProcessor
{
    private readonly ILogger<ValidationProcessor> _logger;

    public int Order => 0;

    public ValidationProcessor(ILogger<ValidationProcessor> logger)
    {
        _logger = logger;
    }

    public async Task ProcessAsync(RequestProcessingContext context, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(context.TargetUrl))
        {
            _logger.LogWarning("Empty URL in request");
            context.AbortPipeline = true;
            return;
        }

        if (!Uri.TryCreate(context.TargetUrl, UriKind.Absolute, out var uri))
        {
            _logger.LogWarning("Invalid URL format: {Url}", context.TargetUrl);
            context.AbortPipeline = true;
            return;
        }

        // Проверка поддерживаемых схем
        if (uri.Scheme != "http" && uri.Scheme != "https")
        {
            _logger.LogWarning("Unsupported URL scheme: {Scheme}", uri.Scheme);
            context.AbortPipeline = true;
            return;
        }

        await Task.CompletedTask;
    }
}