namespace ProxyServer
{
    public class AuthorizationMiddleware(IConfiguration configuration) : IMiddleware
    {
        private string[] ApiKeys => configuration.GetSection("ApiKeys").GetChildren()
            .Select(c => c.Value ?? string.Empty)
            .Where(v => !string.IsNullOrEmpty(v))
            .ToArray();

        private bool RequireAuth => configuration.GetValue<bool>("SimpleCorsProxy:RequireAuth", false);

        public Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            if (RequireAuth && !IsAuthorized(context))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return context.Response.WriteAsync("Unauthorized");
            }
            else
            {
                return next(context);
            }
        }

        private string GetAuthHeaderToken(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue("Authorization", out var authHeader))
            {
                foreach (var headerValue in authHeader)
                {
                    // Ожидаем формат: "Bearer <token>"
                    var parts = headerValue?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                    if (parts != null && parts.Length == 2 && parts[0].Equals("Bearer", StringComparison.OrdinalIgnoreCase))
                    {
                        return parts[1];
                    }
                }
            }
            return string.Empty;
        }

        /// <summary>
        /// Проверяет авторизацию по токену или заголовку
        /// </summary>
        private bool IsAuthorized(HttpContext context)
        {
            // Получение токена из query параметра
            string urlToken = context.Request.Query["token"].ToString();

            string authHeaderToken = GetAuthHeaderToken(context);

            return ApiKeys.Contains(urlToken) || ApiKeys.Contains(authHeaderToken);
        }
    }

    // Переместить методы расширения в статический класс
    public static class AuthorizationMiddlewareExtensions
    {
        public static IServiceCollection AddTokenAuth(this IServiceCollection services) => services.AddSingleton<AuthorizationMiddleware>();
        public static IApplicationBuilder UseTokenAuth(this IApplicationBuilder app) => app.UseMiddleware<AuthorizationMiddleware>();
    }
}
