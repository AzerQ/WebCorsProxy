namespace ProxyServer.Pipelines;

/// <summary>
/// Интерфейс для пайплайна обработки запросов
/// </summary>
public interface IRequestPipeline
{
    /// <summary>
    /// Выполняет пайплайн обработки запроса
    /// </summary>
    /// <param name="context">Контекст запроса</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Задача выполнения пайплайна</returns>
    Task ExecuteAsync(RequestProcessingContext context, CancellationToken cancellationToken = default);
}

/// <summary>
/// Интерфейс для пайплайна обработки ответов
/// </summary>
public interface IResponsePipeline
{
    /// <summary>
    /// Выполняет пайплайн обработки ответа
    /// </summary>
    /// <param name="context">Контекст ответа</param>
    /// <param name="cancellationToken">Токен отмены</param>
    /// <returns>Задача выполнения пайплайна</returns>
    Task ExecuteAsync(ResponseProcessingContext context, CancellationToken cancellationToken = default);
}