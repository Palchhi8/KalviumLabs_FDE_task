using System.Text.Json;

namespace InvoicingSystem.Data
{
    public class ApiKeyAuthenticationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

        public ApiKeyAuthenticationMiddleware(RequestDelegate next, IConfiguration configuration, ILogger<ApiKeyAuthenticationMiddleware> logger)
        {
            _next = next;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
    
            var path = context.Request.Path.Value?.ToLower();
            if (path?.Contains("/swagger") == true || 
                path?.Contains("/health") == true ||
                path == "/" ||
                path == "/favicon.ico")
            {
                await _next(context);
                return;
            }

            const string ApiKeyHeaderName = "X-API-Key";

            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey))
            {
                _logger.LogWarning("API Key was not provided in request to {Path}", context.Request.Path);
                await WriteUnauthorizedResponse(context, "API Key was not provided");
                return;
            }

            var apiKeys = _configuration.GetSection("ApiSettings:ApiKeys").Get<string[]>();
            if (apiKeys == null || !apiKeys.Any(key => key == extractedApiKey.ToString()))
            {
                _logger.LogWarning("Unauthorized API Key attempted access to {Path}", context.Request.Path);
                await WriteUnauthorizedResponse(context, "Unauthorized");
                return;
            }

            _logger.LogInformation("Successful API Key authentication for {Path}", context.Request.Path);
            await _next(context);
        }

        private static async Task WriteUnauthorizedResponse(HttpContext context, string message)
        {
            context.Response.StatusCode = 401;
            context.Response.ContentType = "application/json";

            var response = new
            {
                success = false,
                message = message,
                statusCode = 401
            };

            var jsonResponse = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            await context.Response.WriteAsync(jsonResponse);
        }
    }
}