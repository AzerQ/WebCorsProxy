using ProxyServer.Configuration;

namespace ProxyServer.Pipelines;

/// <summary>
/// Реализация пайплайна обработки ответов по умолчанию
/// </summary>
public class DefaultResponsePipeline : IResponsePipeline
{
    private readonly IEnumerable<IResponseProcessor> _processors;
    private readonly ILogger<DefaultResponsePipeline> _logger;
    private readonly PipelineConfiguration _configuration;

    public DefaultResponsePipeline(
        IEnumerable<IResponseProcessor> processors,
        ILogger<DefaultResponsePipeline> logger,
        IConfiguration configuration)
    {
        _processors = processors;
        _logger = logger;
        _configuration = configuration.GetSection("ProxyPipeline").Get<PipelineConfiguration>() ?? new PipelineConfiguration();
    }

    public async Task ExecuteAsync(ResponseProcessingContext context, CancellationToken cancellationToken = default)
    {
        // Получаем отсортированные обработчики в соответствии с конфигурацией
        var sortedProcessors = GetSortedProcessors();

        foreach (var processor in sortedProcessors)
        {
            try
            {
                _logger.LogDebug("Executing response processor: {ProcessorType}", processor.GetType().Name);
                await processor.ProcessAsync(context, cancellationToken);

                // Если обработчик прервал пайплайн, прекращаем выполнение
                if (context.AbortPipeline)
                {
                    _logger.LogDebug("Response pipeline aborted by processor: {ProcessorType}", processor.GetType().Name);
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in response processor: {ProcessorType}", processor.GetType().Name);
                throw;
            }
        }
    }

    private IEnumerable<IResponseProcessor> GetSortedProcessors()
    {
        var processorList = _processors.ToList();

        // Применяем конфигурацию, если она есть
        if (_configuration.ResponsePipeline.EnabledProcessors.Any())
        {
            processorList = processorList
                .Where(p => _configuration.ResponsePipeline.EnabledProcessors.Contains(p.GetType().Name))
                .ToList();
        }

        // Сортируем по Order с учетом конфигурации
        return processorList
            .Select(p => new
            {
                Processor = p,
                Order = GetProcessorOrder(p)
            })
            .OrderBy(x => x.Order)
            .Select(x => x.Processor);
    }

    private int GetProcessorOrder(IResponseProcessor processor)
    {
        var processorName = processor.GetType().Name;
        
        if (_configuration.ResponsePipeline.ProcessorSettings.TryGetValue(processorName, out var settings) && settings.Order.HasValue)
        {
            return settings.Order.Value;
        }

        return processor.Order;
    }
}