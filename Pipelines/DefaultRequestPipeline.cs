using ProxyServer.Configuration;

namespace ProxyServer.Pipelines;

/// <summary>
/// Реализация пайплайна обработки запросов по умолчанию
/// </summary>
public class DefaultRequestPipeline : IRequestPipeline
{
    private readonly IEnumerable<IRequestProcessor> _processors;
    private readonly ILogger<DefaultRequestPipeline> _logger;
    private readonly PipelineConfiguration _configuration;

    public DefaultRequestPipeline(
        IEnumerable<IRequestProcessor> processors,
        ILogger<DefaultRequestPipeline> logger,
        IConfiguration configuration)
    {
        _processors = processors;
        _logger = logger;
        _configuration = configuration.GetSection("ProxyPipeline").Get<PipelineConfiguration>() ?? new PipelineConfiguration();
    }

    public async Task ExecuteAsync(RequestProcessingContext context, CancellationToken cancellationToken = default)
    {
        // Получаем отсортированные обработчики в соответствии с конфигурацией
        var sortedProcessors = GetSortedProcessors();

        foreach (var processor in sortedProcessors)
        {
            try
            {
                _logger.LogDebug("Executing request processor: {ProcessorType}", processor.GetType().Name);
                await processor.ProcessAsync(context, cancellationToken);

                // Если обработчик прервал пайплайн, прекращаем выполнение
                if (context.AbortPipeline)
                {
                    _logger.LogDebug("Request pipeline aborted by processor: {ProcessorType}", processor.GetType().Name);
                    break;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in request processor: {ProcessorType}", processor.GetType().Name);
                throw;
            }
        }
    }

    private IEnumerable<IRequestProcessor> GetSortedProcessors()
    {
        var processorList = _processors.ToList();

        // Применяем конфигурацию, если она есть
        if (_configuration.RequestPipeline.EnabledProcessors.Any())
        {
            processorList = processorList
                .Where(p => _configuration.RequestPipeline.EnabledProcessors.Contains(p.GetType().Name))
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

    private int GetProcessorOrder(IRequestProcessor processor)
    {
        var processorName = processor.GetType().Name;
        
        if (_configuration.RequestPipeline.ProcessorSettings.TryGetValue(processorName, out var settings) && settings.Order.HasValue)
        {
            return settings.Order.Value;
        }

        return processor.Order;
    }
}