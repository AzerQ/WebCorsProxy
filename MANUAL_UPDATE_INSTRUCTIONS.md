# ������ ���������� SimpleCorsProxyService.cs

���� `SimpleCorsProxyService.cs` ����� ��������, ������� ��������� �����������.

## ���������:

### 1. �������� ���� � ����� (����� ������������ �����):
```csharp
private readonly IConfiguration _configuration;
private readonly string[] _apiKeys;
private readonly bool _requireAuth;
```

### 2. �������� �����������:
```csharp
public SimpleCorsProxyService(
    IHttpClientFactory httpClientFactory,
    ILogger<SimpleCorsProxyService> logger,
    IConfiguration configuration)  // <- �������� ��������
{
    _httpClientFactory = httpClientFactory;
    _logger = logger;
    _configuration = configuration;  // <- �����
    
    // ��������� API ����� <- �����
    _apiKeys = configuration.GetSection("ApiKeys").GetChildren()
        .Select(c => c.Value ?? string.Empty)
        .Where(v => !string.IsNullOrEmpty(v))
        .ToArray();
        
    // ���������, ��������� �� ����������� ��� SimpleCorsProxy <- �����
    _requireAuth = configuration.GetValue<bool>("SimpleCorsProxy:RequireAuth", false);
}
```

### 3. �������� ��������� ������ ProxyRequestAsync:
```csharp
public async Task<IResult> ProxyRequestAsync(HttpContext context, string url, string? token = null)  // <- �������� �������� token
```

### 4. �������� �������� ����������� � ������ ������ ProxyRequestAsync (����� try):
```csharp
try
{
    // �������� �����������, ���� ��������� <- �����
    if (_requireAuth && !IsAuthorized(context, token))
    {
        _logger.LogWarning("Unauthorized SimpleCorsProxy request to: {Url}", url);
        return Results.Unauthorized();
    }
    
    // ... ��������� ��� ...
}
```

### 5. �������� ����� IsAuthorized � ����� ������ (����� ����������� ������� ������):
```csharp
/// <summary>
/// ��������� ����������� �������
/// </summary>
private bool IsAuthorized(HttpContext context, string? tokenParam = null)
{
    // ���� ����������� �� ���������, ������ ���������
    if (!_requireAuth)
    {
        return true;
    }

    // ���� API ������ ���, ��������� (��� �������� �������������)
    if (_apiKeys.Length == 0)
    {
        return true;
    }

    string? token = null;

    // ��������� Bearer ����� � ��������� Authorization
    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
    if (!string.IsNullOrEmpty(authHeader) && authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
    {
        token = authHeader.Substring("Bearer ".Length);
    }

    // ���� ����� �� ������ � ���������, ���������� �������� �������
    if (string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(tokenParam))
    {
        token = tokenParam;
    }

    // ��������� �����
    return !string.IsNullOrEmpty(token) && _apiKeys.Contains(token);
}
```

## ��� ���������� ������ ���� �� �����������

���� �����, ����� ��������� �������� ���������� ����� ����� �� commits.
