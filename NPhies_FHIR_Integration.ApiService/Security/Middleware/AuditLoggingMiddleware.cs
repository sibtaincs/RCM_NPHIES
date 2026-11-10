using System.Diagnostics;
using NPhies_FHIR_Integration.ApiService.Security.Services;

namespace NPhies_FHIR_Integration.ApiService.Security.Middleware;

/// <summary>
/// Audit logging middleware
/// </summary>
public class AuditLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuditLoggingMiddleware> _logger;

    // Endpoints to skip logging
    private static readonly HashSet<string> SkipLoggingPaths = new()
    {
   "/health",
    "/swagger",
       "/api/auth/health"
  };

    public AuditLoggingMiddleware(RequestDelegate next, ILogger<AuditLoggingMiddleware> logger)
    {
     _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAuditLoggingService auditLoggingService)
    {
  // Skip logging for certain paths
        if (ShouldSkipLogging(context.Request.Path.Value))
 {
     await _next(context);
      return;
        }

    var stopwatch = Stopwatch.StartNew();
        var userId = context.User?.FindFirst("sub")?.Value;
  var username = context.User?.FindFirst("preferred_username")?.Value;

 // Log the request
        _logger.LogInformation(
      "API Request: {Method} {Path} by {Username} from {IpAddress}",
       context.Request.Method,
   context.Request.Path,
        username ?? "anonymous",
context.Connection.RemoteIpAddress?.ToString() ?? "unknown");

    // Capture response body
 var originalBodyStream = context.Response.Body;
        using (var memoryStream = new MemoryStream())
    {
   context.Response.Body = memoryStream;

     try
   {
     await _next(context);

         stopwatch.Stop();
   memoryStream.Position = 0;

     // Log audit entry
     if (!string.IsNullOrEmpty(userId))
  {
    var action = $"{context.Request.Method} {context.Request.Path}";
    var entry = new AuditLogEntry
      {
    UserId = userId,
     Username = username,
        Action = action,
    AuditLevel = GetAuditLevel(context.Response.StatusCode),
   DurationMs = stopwatch.Elapsed.TotalMilliseconds,
      ChangeDetails = $"Status: {context.Response.StatusCode}"
  };

   await auditLoggingService.LogActionAsync(entry);
  }

    _logger.LogInformation(
    "API Response: {Method} {Path} - {StatusCode} ({DurationMs}ms)",
 context.Request.Method,
    context.Request.Path,
context.Response.StatusCode,
      stopwatch.Elapsed.TotalMilliseconds);
        }
   finally
        {
    memoryStream.Position = 0;
   await memoryStream.CopyToAsync(originalBodyStream);
  context.Response.Body = originalBodyStream;
  }
        }
    }

   private static bool ShouldSkipLogging(string? path)
 {
 if (string.IsNullOrEmpty(path))
  return true;

return SkipLoggingPaths.Any(p => path.Contains(p, StringComparison.OrdinalIgnoreCase));
   }

  private static string GetAuditLevel(int statusCode)
  {
     return statusCode switch
 {
      >= 200 and < 300 => "Info",
       >= 300 and < 400 => "Info",
      >= 400 and < 500 => "Warning",
   >= 500 => "Error",
  _ => "Info"
        };
    }
}

/// <summary>
/// Audit logging middleware extension methods
/// </summary>
public static class AuditLoggingMiddlewareExtensions
{
    public static IApplicationBuilder UseAuditLogging(this IApplicationBuilder builder)
    {
     return builder.UseMiddleware<AuditLoggingMiddleware>();
   }
}
