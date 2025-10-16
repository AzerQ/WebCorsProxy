using Microsoft.Extensions.Configuration;

namespace ProxyServer.Pipelines.RequestProcessors;

/// <summary>
/// Обработчик авторизации запросов
/// </summary>
public class AuthorizationProcessor : IRequestProcessor
{
    private readonly string[] _apiKeys;
    private readonly ILogger<AuthorizationProcessor> _logger;

    public int Order => 1;

    public AuthorizationProcessor(IConfiguration configuration, ILogger<AuthorizationProcessor> logger)
    {
        _logger = logger;
        _apiKeys = configuration.GetSection("ApiKeys").GetChildren()
            .Select(c => c.Value ?? string.Empty)
            .Where(v => !string.IsNullOrEmpty(v))
            .ToArray();
    }

    public async Task ProcessAsync(RequestProcessingContext context, CancellationToken cancellationToken = default)
    {
        if (!IsAuthorized(context.HttpContext, context.Token))
        {
            _logger.LogWarning("Unauthorized request to {Url}", context.TargetUrl);
            context.AbortPipeline = true;
            return;
        }

        await Task.CompletedTask;
    }

    private bool IsAuthorized(HttpContext context, string? tokenParam = null)
    {
        string? token = null;

        var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
        if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer "))
        {
            token = authHeader.Substring("Bearer ".Length);
        }

        if (string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(tokenParam))
        {
            token = tokenParam;
        }

        return !string.IsNullOrEmpty(token) && _apiKeys.Contains(token);
    }
}