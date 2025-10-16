namespace ProxyServer.Pipelines;

/// <summary>
/// Интерфейс для обработчиков запросов в пайплайне предобработки
/// </summary>
public interface IRequestProcessor
{
    /// <summary>
    /// Порядок выполнения обработчика в пайплайне
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Обрабатывает контекст запроса перед отправкой к целевому серверу
    /// </summary>
    /// <param name="context">Контекст запроса</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Задача обработки</returns>
    Task ProcessAsync(RequestProcessingContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Контекст обработки запроса
/// </summary>
public class RequestProcessingContext
{
    /// <summary>
    /// Оригинальный HTTP контекст
    /// </summary>
    public HttpContext HttpContext { get; set; } = null!;

    /// <summary>
    /// URL целевого ресурса
    /// </summary>
    public string TargetUrl { get; set; } = string.Empty;

    /// <summary>
    /// Токен авторизации
    /// </summary>
    public string? Token { get; set; }

    /// <summary>
    /// HTTP запрос к целевому серверу
    /// </summary>
    public HttpRequestMessage ProxyRequest { get; set; } = null!;

    /// <summary>
    /// Дополнительные данные для передачи между обработчиками
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Флаг, указывающий, следует ли прервать выполнение пайплайна
    /// </summary>
    public bool AbortPipeline { get; set; }
}