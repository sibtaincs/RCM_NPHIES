using NPhies_FHIR_Integration.ApiService.Security.Services;

namespace NPhies_FHIR_Integration.ApiService.Security.Middleware;

/// <summary>
/// Rate limiting middleware for API requests
/// </summary>
public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RateLimitingMiddleware> _logger;

    public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger)
    {
 _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IRateLimitingService rateLimitingService)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
 var endpoint = $"{context.Request.Method} {context.Request.Path}";
        var userId = context.User?.FindFirst("sub")?.Value;

        // Check if rate limited
        var isRateLimited = await rateLimitingService.IsRateLimitedAsync(
  ipAddress, endpoint, maxRequests: 100, windowMinutes: 1);

     if (isRateLimited)
      {
         _logger.LogWarning("Rate limit exceeded for {IpAddress} on {Endpoint}", ipAddress, endpoint);
      context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
 context.Response.Headers.Add("Retry-After", "60");
     await context.Response.WriteAsJsonAsync(new
     {
      error = "Rate limit exceeded. Please try again later.",
     retryAfter = 60
   });
          return;
       }

 // Record the request
        await rateLimitingService.RecordRequestAsync(userId, ipAddress, endpoint, context.Request.Method);

        // Add rate limit headers
        var status = await rateLimitingService.GetRateLimitStatusAsync(ipAddress, endpoint);
  context.Response.Headers.Add("X-RateLimit-Limit", status.MaxRequests.ToString());
    context.Response.Headers.Add("X-RateLimit-Remaining", status.RemainingRequests.ToString());
 context.Response.Headers.Add("X-RateLimit-Reset", new DateTimeOffset(status.ResetTime).ToUnixTimeSeconds().ToString());

        await _next(context);
    }
}

/// <summary>
/// Rate limiting extension methods
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
  {
      return builder.UseMiddleware<RateLimitingMiddleware>();
   }
}
