namespace ProxyServer.Pipelines;

/// <summary>
/// Интерфейс для обработчиков ответов в пайплайне постобработки
/// </summary>
public interface IResponseProcessor
{
    /// <summary>
    /// Порядок выполнения обработчика в пайплайне
    /// </summary>
    int Order { get; }

    /// <summary>
    /// Обрабатывает контекст ответа после получения от целевого сервера
    /// </summary>
    /// <param name="context">Контекст ответа</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Задача обработки</returns>
    Task ProcessAsync(ResponseProcessingContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Контекст обработки ответа
/// </summary>
public class ResponseProcessingContext
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
    /// HTTP ответ от целевого сервера
    /// </summary>
    public HttpResponseMessage ProxyResponse { get; set; } = null!;

    /// <summary>
    /// Содержимое ответа
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Поток содержимого ответа (для бинарных данных)
    /// </summary>
    public Stream? ContentStream { get; set; }

    /// <summary>
    /// Content-Type ответа
    /// </summary>
    public string? ContentType { get; set; }

    /// <summary>
    /// Дополнительные данные для передачи между обработчиками
    /// </summary>
    public Dictionary<string, object> Properties { get; set; } = new();

    /// <summary>
    /// Флаг, указывающий, следует ли прервать выполнение пайплайна
    /// </summary>
    public bool AbortPipeline { get; set; }

    /// <summary>
    /// Флаг, указывающий, что контент был изменен и нужно использовать свойство Content
    /// </summary>
    public bool ContentModified { get; set; }
}