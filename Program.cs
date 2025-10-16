using ProxyServer;
using ProxyServer.Pipelines;
using ProxyServer.Pipelines.RequestProcessors;
using ProxyServer.Pipelines.ResponseProcessors;
using ProxyServer.Configuration;
using Microsoft.AspNetCore.Cors;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Регистрируем конфигурацию пайплайнов
builder.Services.Configure<PipelineConfiguration>(builder.Configuration.GetSection("ProxyPipeline"));

// Регистрируем обработчики запросов
builder.Services.AddScoped<IRequestProcessor, ValidationProcessor>();
builder.Services.AddScoped<IRequestProcessor, AuthorizationProcessor>();
builder.Services.AddScoped<IRequestProcessor, HeadersProcessor>();

// Регистрируем обработчики ответов
builder.Services.AddScoped<IResponseProcessor, ContentDetectionProcessor>();
builder.Services.AddScoped<IResponseProcessor, HeadersResponseProcessor>();
builder.Services.AddScoped<IResponseProcessor, HtmlContentProcessor>();
builder.Services.AddScoped<IResponseProcessor, CssContentProcessor>();
builder.Services.AddScoped<IResponseProcessor, JsContentProcessor>();

// Регистрируем пайплайны
builder.Services.AddScoped<IRequestPipeline, DefaultRequestPipeline>();
builder.Services.AddScoped<IResponsePipeline, DefaultResponsePipeline>();

// Регистрируем сервис с пайплайнами
builder.Services.AddScoped<ProxyService>();

// Регистрируем простой CORS прокси-сервис
builder.Services.AddScoped<SimpleCorsProxyService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});
builder.Services.AddHttpClient("", client =>
{
    client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
}).ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
{
    AutomaticDecompression = System.Net.DecompressionMethods.All
});

var app = builder.Build();

var logger = app.Logger;

// Configure the HTTP request pipeline.

app.UseSwagger();
app.UseSwaggerUI();


app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseStaticFiles();

app.MapGet("/", () => Results.Redirect("/index.html"))
    .ExcludeFromDescription();

app.MapGet("/web", async (ProxyService proxyService, HttpContext context, string url, string? token) =>
{
    return await proxyService.ProxyRequestAsync(context, url, token);
})
.WithName("ProxyWithPipeline")
.WithSummary("Прокси-сервис с обработкой контента")
.WithDescription(@"Прокси-сервис с полным пайплайном обработки запросов и ответов.

Возможности:
- Валидация и авторизация запросов
- Обработка заголовков
- Автоматическая обработка HTML, CSS и JS контента
- Преобразование относительных URL в абсолютные
- Поддержка токенов авторизации

Примеры использования:
- Простой запрос: /web?url=https://example.com
- С токеном авторизации: /web?url=https://api.example.com/data&token=your-token-here")
.WithOpenApi(operation =>
{
    operation.Parameters[0].Description = "Целевой URL для проксирования (обязательный параметр)";
    operation.Parameters[0].Example = new Microsoft.OpenApi.Any.OpenApiString("https://example.com");
    operation.Parameters[1].Description = "Токен авторизации для доступа к защищенным ресурсам (необязательный)";
    operation.Parameters[1].Example = new Microsoft.OpenApi.Any.OpenApiString("Bearer your-jwt-token");
    return operation;
})
.Produces<string>(StatusCodes.Status200OK, "text/html", "text/css", "application/javascript", "application/json", "text/plain")
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status401Unauthorized)
.Produces(StatusCodes.Status500InternalServerError);

app.MapGet("/proxy", async (SimpleCorsProxyService simpleCorsProxyService, HttpContext context, string? url) =>
{
    if (string.IsNullOrWhiteSpace(url))
    {
        return Results.BadRequest("URL parameter is required");
    }
    return await simpleCorsProxyService.ProxyRequestAsync(context, url);
})
.WithName("SimpleCorsProxy")
.WithSummary("Простой CORS прокси")
.WithDescription(@"Простой CORS прокси-сервис без обработки контента.

Возвращает контент 'as-is' с добавлением только CORS заголовков.
Подходит для:
- Обхода CORS ограничений
- Проксирования статических ресурсов
- Быстрого получения контента без дополнительной обработки

Примеры использования:
- Изображение: /proxy?url=https://example.com/image.jpg
- API данные: /proxy?url=https://api.example.com/data.json
- Любой ресурс: /proxy?url=https://example.com/resource")
.WithOpenApi(operation =>
{
    operation.Parameters[0].Description = "Целевой URL для проксирования (обязательный параметр). Должен быть корректным HTTP/HTTPS URL.";
    operation.Parameters[0].Example = new Microsoft.OpenApi.Any.OpenApiString("https://api.example.com/data");
    operation.Parameters[0].Required = true;
    return operation;
})
.Produces<string>(StatusCodes.Status200OK, "application/json", "text/html", "text/plain", "application/octet-stream")
.Produces(StatusCodes.Status400BadRequest)
.Produces(StatusCodes.Status500InternalServerError);

app.Run();
