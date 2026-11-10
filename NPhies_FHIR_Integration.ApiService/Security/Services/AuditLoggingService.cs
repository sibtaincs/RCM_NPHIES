using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.ApiService.Security.Services;

/// <summary>
/// Audit logging service
/// </summary>
public interface IAuditLoggingService
{
    Task LogActionAsync(AuditLogEntry entry);
    Task LogAuthenticationAsync(string username, bool success, string? userId = null, string? reason = null);
    Task LogEntityChangeAsync(string entityType, string entityId, object? oldValues, object? newValues, string action);
    Task LogSecurityEventAsync(string eventType, string message, string severity = "Warning");
}

/// <summary>
/// Audit logging service implementation
/// </summary>
public class AuditLoggingService : IAuditLoggingService
{
 private readonly ApplicationDbContext _dbContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ILogger<AuditLoggingService> _logger;

    public AuditLoggingService(
        ApplicationDbContext dbContext,
        IHttpContextAccessor httpContextAccessor,
        ILogger<AuditLoggingService> logger)
    {
        _dbContext = dbContext;
        _httpContextAccessor = httpContextAccessor;
        _logger = logger;
    }

    /// <summary>
    /// Log an action
    /// </summary>
    public async Task LogActionAsync(AuditLogEntry entry)
    {
        try
        {
     var httpContext = _httpContextAccessor.HttpContext;
   var auditLog = new AuditLog
     {
     Id = Guid.NewGuid().ToString(),
        UserId = entry.UserId,
  Username = entry.Username,
                Action = entry.Action,
            EntityType = entry.EntityType,
                EntityId = entry.EntityId,
       OldValues = entry.OldValues,
  NewValues = entry.NewValues,
                ChangeDetails = entry.ChangeDetails,
   IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
          UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
     CreatedAt = DateTime.UtcNow,
     AuditLevel = entry.AuditLevel,
    Endpoint = $"{httpContext?.Request.Method} {httpContext?.Request.Path}",
            HttpStatusCode = httpContext?.Response.StatusCode,
    DurationMs = entry.DurationMs
            };

            _dbContext.AuditLogs.Add(auditLog);
        await _dbContext.SaveChangesAsync();

    _logger.LogInformation(
             "Audit log created: {Action} by {Username} on {EntityType} {EntityId}",
      entry.Action, entry.Username, entry.EntityType, entry.EntityId);
        }
      catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating audit log");
        }
    }

    /// <summary>
    /// Log authentication event
    /// </summary>
    public async Task LogAuthenticationAsync(string username, bool success, string? userId = null, string? reason = null)
    {
        try
        {
        var httpContext = _httpContextAccessor.HttpContext;
       var loginAttempt = new LoginAttempt
    {
 Id = Guid.NewGuid().ToString(),
         UserId = userId,
  Username = username,
   IpAddress = httpContext?.Connection.RemoteIpAddress?.ToString() ?? "Unknown",
       UserAgent = httpContext?.Request.Headers["User-Agent"].ToString(),
      IsSuccessful = success,
         FailureReason = reason,
        AttemptAt = DateTime.UtcNow
  };

            _dbContext.LoginAttempts.Add(loginAttempt);
            await _dbContext.SaveChangesAsync();

 var eventType = success ? "Login successful" : "Login failed";
_logger.LogInformation(
        "Authentication: {EventType} for user {Username} from {IpAddress}",
                eventType, username, loginAttempt.IpAddress);
        }
   catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging authentication attempt");
        }
    }

    /// <summary>
    /// Log entity change
    /// </summary>
    public async Task LogEntityChangeAsync(string entityType, string entityId, object? oldValues, object? newValues, string action)
    {
        var httpContext = _httpContextAccessor.HttpContext;
        var userId = httpContext?.User.FindFirst("sub")?.Value;

        var entry = new AuditLogEntry
     {
            UserId = userId,
   Username = httpContext?.User.FindFirst("preferred_username")?.Value,
      Action = action,
      EntityType = entityType,
         EntityId = entityId,
 OldValues = System.Text.Json.JsonSerializer.Serialize(oldValues),
         NewValues = System.Text.Json.JsonSerializer.Serialize(newValues),
    AuditLevel = "Info"
   };

   await LogActionAsync(entry);
    }

    /// <summary>
    /// Log security event
    /// </summary>
  public async Task LogSecurityEventAsync(string eventType, string message, string severity = "Warning")
    {
var httpContext = _httpContextAccessor.HttpContext;
      var userId = httpContext?.User.FindFirst("sub")?.Value;

        var entry = new AuditLogEntry
  {
 UserId = userId,
  Username = httpContext?.User.FindFirst("preferred_username")?.Value,
    Action = eventType,
      ChangeDetails = message,
  AuditLevel = severity
        };

  await LogActionAsync(entry);
    }
}

/// <summary>
/// Audit log entry DTO
/// </summary>
public class AuditLogEntry
{
    public string? UserId { get; set; }
    public string? Username { get; set; }
 public string Action { get; set; } = string.Empty;
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ChangeDetails { get; set; }
    public string AuditLevel { get; set; } = "Info";
    public double? DurationMs { get; set; }
}
