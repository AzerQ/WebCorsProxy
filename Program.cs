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
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseStaticFiles();

app.MapGet("/", () => Results.Redirect("/index.html"));

app.MapGet("/web", async (ProxyService proxyService, HttpContext context, string url, string? token) =>
{
    return await proxyService.ProxyRequestAsync(context, url, token);
});

app.Run();
