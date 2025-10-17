# Ручное обновление SimpleCorsProxyService.cs

Файл `SimpleCorsProxyService.cs` нужно обновить, добавив поддержку авторизации.

## Изменения:

### 1. Добавить поля в класс (после существующих полей):
```csharp
private readonly IConfiguration _configuration;
private readonly string[] _apiKeys;
private readonly bool _requireAuth;
```

### 2. Обновить конструктор:
```csharp
public SimpleCorsProxyService(
    IHttpClientFactory httpClientFactory,
    ILogger<SimpleCorsProxyService> logger,
    IConfiguration configuration)  // <- Добавлен параметр
{
    _httpClientFactory = httpClientFactory;
    _logger = logger;
    _configuration = configuration;  // <- Новое
    
    // Загружаем API ключи <- Новое
    _apiKeys = configuration.GetSection("ApiKeys").GetChildren()
        .Select(c => c.Value ?? string.Empty)
        .Where(v => !string.IsNullOrEmpty(v))
        .ToArray();
        
    // Проверяем, требуется ли авторизация для SimpleCorsProxy <- Новое
    _requireAuth = configuration.GetValue<bool>("SimpleCorsProxy:RequireAuth", false);
}
```

### 3. Обновить сигнатуру метода ProxyRequestAsync:
```csharp
public async Task<IResult> ProxyRequestAsync(HttpContext context, string url, string? token = null)  // <- Добавлен параметр token
```

### 4. Добавить проверку авторизации в начало метода ProxyRequestAsync (после try):
```csharp
try
{
    // Проверка авторизации, если требуется <- Новое
    if (_requireAuth && !IsAuthorized(context, token))
    {
        _logger.LogWarning("Unauthorized SimpleCorsProxy request to: {Url}", url);
        return Results.Unauthorized();
    }
    
    // ... остальной код ...
}
```

### 5. Добавить метод IsAuthorized в конец класса (перед закрывающей скобкой класса):
```csharp
/// <summary>
/// Проверяет авторизацию запроса
/// </summary>
private bool IsAuthorized(HttpContext context, string? tokenParam = null)
{
    // Если авторизация не требуется, всегда разрешаем
    if (!_requireAuth)
    {
        return true;
    }

    // Если API ключей нет, разрешаем (для обратной совместимости)
    if (_apiKeys.Length == 0)
    {
        return true;
    }

    string? token = null;

    // Проверяем Bearer токен в заголовке Authorization
    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        token = authHeader.Substring("Bearer ".Length);
    }

    // Если токен не найден в заголовке, используем параметр запроса
    if (string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(tokenParam))
    {
        token = tokenParam;
    }

    // Проверяем токен
    return !string.IsNullOrEmpty(token) && _apiKeys.Contains(token);
}
```

## Или скопируйте полный файл из репозитория

Если проще, можно полностью заменить содержимое файла кодом из commits.
