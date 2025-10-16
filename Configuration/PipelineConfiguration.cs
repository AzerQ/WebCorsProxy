namespace ProxyServer.Configuration;

/// <summary>
/// Конфигурация пайплайнов обработки
/// </summary>
public class PipelineConfiguration
{
    /// <summary>
    /// Конфигурация пайплайна обработки запросов
    /// </summary>
    public RequestPipelineConfiguration RequestPipeline { get; set; } = new();

    /// <summary>
    /// Конфигурация пайплайна обработки ответов
    /// </summary>
    public ResponsePipelineConfiguration ResponsePipeline { get; set; } = new();
}

/// <summary>
/// Конфигурация пайплайна обработки запросов
/// </summary>
public class RequestPipelineConfiguration
{
    /// <summary>
    /// Список включенных обработчиков запросов
    /// </summary>
    public List<string> EnabledProcessors { get; set; } = new();

    /// <summary>
    /// Настройки для отдельных обработчиков
    /// </summary>
    public Dictionary<string, ProcessorSettings> ProcessorSettings { get; set; } = new();
}

/// <summary>
/// Конфигурация пайплайна обработки ответов
/// </summary>
public class ResponsePipelineConfiguration
{
    /// <summary>
    /// Список включенных обработчиков ответов
    /// </summary>
    public List<string> EnabledProcessors { get; set; } = new();

    /// <summary>
    /// Настройки для отдельных обработчиков
    /// </summary>
    public Dictionary<string, ProcessorSettings> ProcessorSettings { get; set; } = new();
}

/// <summary>
/// Настройки для обработчика
/// </summary>
public class ProcessorSettings
{
    /// <summary>
    /// Порядок выполнения обработчика
    /// </summary>
    public int? Order { get; set; }

    /// <summary>
    /// Включен ли обработчик
    /// </summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Дополнительные параметры обработчика
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new();
}