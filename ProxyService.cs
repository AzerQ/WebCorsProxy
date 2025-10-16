using ProxyServer.Pipelines;

namespace ProxyServer;

/// <summary>
/// Прокси-сервис с использованием гибких пайплайнов обработки
/// </summary>
public class ProxyService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ProxyService> _logger;
    private readonly IRequestPipeline _requestPipeline;
    private readonly IResponsePipeline _responsePipeline;

    public ProxyService(
        IHttpClientFactory httpClientFactory,
        ILogger<ProxyService> logger,
        IRequestPipeline requestPipeline,
        IResponsePipeline responsePipeline)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _requestPipeline = requestPipeline;
        _responsePipeline = responsePipeline;
    }

    public async Task<IResult> ProxyRequestAsync(HttpContext context, string url)
    {
        var httpClient = _httpClientFactory.CreateClient();
        int? responseStatusCode = null;
        
        try
        {
            // Создаем контекст запроса
            var requestContext = new RequestProcessingContext
            {
                HttpContext = context,
                TargetUrl = url,
                ProxyRequest = new HttpRequestMessage(HttpMethod.Get, url)
            };

            // Выполняем пайплайн предобработки запроса
            await _requestPipeline.ExecuteAsync(requestContext);

            // Если пайплайн был прерван, возвращаем результат
            if (requestContext.AbortPipeline)
            {
                return Results.Unauthorized();
            }

            // Отправляем запрос к целевому серверу
            var response = await httpClient.SendAsync(requestContext.ProxyRequest);
            responseStatusCode = (int)response.StatusCode;

            // Создаем контекст ответа
            var responseContext = new ResponseProcessingContext
            {
                HttpContext = context,
                TargetUrl = url,
                ProxyResponse = response,
                ContentType = response.Content.Headers.ContentType?.MediaType
            };

            // Выполняем пайплайн постобработки ответа
            await _responsePipeline.ExecuteAsync(responseContext);

            // Если пайплайн был прерван, возвращаем статус ответа
            if (responseContext.AbortPipeline)
            {
                context.Response.StatusCode = responseStatusCode.Value;
                return Results.Empty;
            }

            // Устанавливаем статус ответа
            context.Response.StatusCode = responseStatusCode.Value;

            // Возвращаем результат в зависимости от типа контента
            if (responseContext.ContentModified && !string.IsNullOrEmpty(responseContext.Content))
            {
                // Если контент был изменен, возвращаем текстовый контент
                return Results.Content(responseContext.Content, responseContext.ContentType ?? "text/plain");
            }
            else if (responseContext.ContentStream != null)
            {
                // Если контент бинарный, возвращаем поток
                return Results.Stream(responseContext.ContentStream, responseContext.ContentType ?? "application/octet-stream");
            }
            else
            {
                // В противном случае читаем контент как поток
                var contentStream = await response.Content.ReadAsStreamAsync();
                return Results.Stream(contentStream, responseContext.ContentType ?? "application/octet-stream");
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Response status code " + responseStatusCode);
            return Results.Problem($"Error proxying request: {ex.Message}", statusCode: responseStatusCode);
        }
    }
}