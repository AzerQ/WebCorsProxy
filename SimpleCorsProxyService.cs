using System.Net;

namespace ProxyServer;

/// <summary>
/// Простой CORS прокси-сервис без обработки контента
/// Возвращает контент as-is с добавлением только CORS заголовков
/// </summary>
public class SimpleCorsProxyService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SimpleCorsProxyService> _logger;

    public SimpleCorsProxyService(
        IHttpClientFactory httpClientFactory,
        ILogger<SimpleCorsProxyService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Проксирует запрос к целевому URL и возвращает контент as-is
    /// </summary>
    public async Task<IResult> ProxyRequestAsync(HttpContext context, string url)
    {
        try
        {
            // Валидация URL
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                return Results.BadRequest("Invalid URL format");
            }

            // Проверка схемы URL
            if (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
            {
                return Results.BadRequest("Only HTTP and HTTPS protocols are supported");
            }

            _logger.LogInformation("Proxying request to: {Url}", url);

            var httpClient = _httpClientFactory.CreateClient();

            // Создаем запрос
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            // Копируем некоторые заголовки из исходного запроса
            if (context.Request.Headers.TryGetValue("User-Agent", out var userAgent))
            {
                request.Headers.TryAddWithoutValidation("User-Agent", userAgent.ToString());
            }
            else
            {
                request.Headers.TryAddWithoutValidation("User-Agent", "SimpleCorsProxy/1.0");
            }

            if (context.Request.Headers.TryGetValue("Accept", out var accept))
            {
                request.Headers.TryAddWithoutValidation("Accept", accept.ToString());
            }

            if (context.Request.Headers.TryGetValue("Accept-Language", out var acceptLanguage))
            {
                request.Headers.TryAddWithoutValidation("Accept-Language", acceptLanguage.ToString());
            }

            // Отправляем запрос
            var response = await httpClient.SendAsync(request);

            // Устанавливаем статус ответа
            context.Response.StatusCode = (int)response.StatusCode;

            // Добавляем CORS заголовки
            context.Response.Headers.Append("Access-Control-Allow-Origin", "*");
            context.Response.Headers.Append("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            context.Response.Headers.Append("Access-Control-Allow-Headers", "*");

            // Копируем Content-Type из ответа
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
            
            // Копируем некоторые важные заголовки
            if (response.Headers.TryGetValues("Cache-Control", out var cacheControl))
            {
                context.Response.Headers.Append("Cache-Control", string.Join(", ", cacheControl));
            }

            if (response.Content.Headers.TryGetValues("Content-Encoding", out var contentEncoding))
            {
                context.Response.Headers.Append("Content-Encoding", string.Join(", ", contentEncoding));
            }

            // Возвращаем контент as-is
            var contentStream = await response.Content.ReadAsStreamAsync();
            return Results.Stream(contentStream, contentType);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "HTTP request error while proxying to {Url}", url);
            return Results.Problem($"Error proxying request: {ex.Message}", statusCode: StatusCodes.Status502BadGateway);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error while proxying to {Url}", url);
            return Results.Problem($"Error proxying request: {ex.Message}", statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
