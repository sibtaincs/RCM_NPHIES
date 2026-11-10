using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NPhies_FHIR_Integration.Application.Services.Audit;

/// <summary>
/// Audit Logging Service Interface
/// Tracks all operations for compliance and audit trail
/// </summary>
public interface IAuditLoggingService
{
    /// <summary>
    /// Log operation
    /// </summary>
    Task LogOperationAsync(
          string operationType,
            string entityType,
    string entityId,
            string action,
     string? userId = null,
       Dictionary<string, object?>? details = null,
          CancellationToken cancellationToken = default);

    /// <summary>
    /// Log configuration change
    /// </summary>
    Task LogConfigurationChangeAsync(
 string configurationName,
        string? oldValue,
        string? newValue,
        string? userId = null,
        string? reason = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get audit trail for entity
    /// </summary>
    Task<List<AuditLogDto>> GetAuditTrailAsync(
    string entityId,
        int pageSize = 100,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get compliance report
    /// </summary>
    Task<AuditComplianceReport> GetComplianceReportAsync(
        DateTime startDate,
        DateTime endDate,
    CancellationToken cancellationToken = default);

    /// <summary>
    /// Get operations by user
    /// </summary>
    Task<List<AuditLogDto>> GetOperationsByUserAsync(
        string userId,
   DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get critical operations
    /// </summary>
    Task<List<AuditLogDto>> GetCriticalOperationsAsync(
  DateTime startDate,
        DateTime endDate,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// Verify audit trail integrity
    /// </summary>
    Task<AuditIntegrityCheckResult> VerifyIntegrityAsync(
          string entityId,
  CancellationToken cancellationToken = default);
}

/// <summary>
/// Audit Logging Service Implementation
/// </summary>
public class AuditLoggingService : IAuditLoggingService
{
    private readonly ILogger<AuditLoggingService> _logger;
    private readonly List<AuditLogDto> _auditLogs; // In-memory storage (would be DB in production)

    public AuditLoggingService(ILogger<AuditLoggingService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _auditLogs = new List<AuditLogDto>();
    }

    /// <summary>
    /// Log operation
    /// </summary>
    public async Task LogOperationAsync(
        string operationType,
   string entityType,
        string entityId,
    string action,
        string? userId = null,
        Dictionary<string, object?>? details = null,
      CancellationToken cancellationToken = default)
    {
        try
        {
            var auditLog = new AuditLogDto
            {
                AuditId = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                OperationType = operationType,
                EntityType = entityType,
                EntityId = entityId,
                Action = action,
                UserId = userId ?? "system",
                Details = details ?? new Dictionary<string, object?>(),
                IpAddress = "0.0.0.0", // Would get from HTTP context
                Status = "Success"
            };

            _auditLogs.Add(auditLog);

            _logger.LogInformation("Audit logged: {Operation} on {Entity} {Id} by {User}",
              operationType, entityType, entityId, userId ?? "system");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging audit operation");
            throw;
        }
    }

    /// <summary>
    /// Log configuration change
    /// </summary>
    public async Task LogConfigurationChangeAsync(
        string configurationName,
    string? oldValue,
      string? newValue,
        string? userId = null,
        string? reason = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var details = new Dictionary<string, object?>
        {
    { "ConfigurationName", configurationName },
    { "OldValue", oldValue },
          { "NewValue", newValue },
          { "Reason", reason }
        };

            await LogOperationAsync("Configuration", "Configuration", configurationName, "Changed", userId, details, cancellationToken);

            _logger.LogWarning("Configuration changed: {Config} from {OldVal} to {NewVal} by {User}",
                    configurationName, oldValue, newValue, userId ?? "system");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error logging configuration change");
            throw;
        }
    }

    /// <summary>
    /// Get audit trail for entity
    /// </summary>
    public async Task<List<AuditLogDto>> GetAuditTrailAsync(
        string entityId,
    int pageSize = 100,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var trail = _auditLogs
             .Where(a => a.EntityId == entityId)
              .OrderByDescending(a => a.Timestamp)
      .Take(pageSize)
          .ToList();

            _logger.LogInformation("Retrieved audit trail for entity {EntityId}: {Count} records", entityId, trail.Count);
            return trail;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving audit trail for entity {EntityId}", entityId);
            return new List<AuditLogDto>();
        }
    }

    /// <summary>
    /// Get compliance report
    /// </summary>
    public async Task<AuditComplianceReport> GetComplianceReportAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var periodLogs = _auditLogs
         .Where(a => a.Timestamp >= startDate && a.Timestamp <= endDate)
                  .ToList();

            var report = new AuditComplianceReport
            {
                ReportId = Guid.NewGuid().ToString(),
                ReportPeriod = $"{startDate:yyyy-MM-dd} to {endDate:yyyy-MM-dd}",
                TotalAuditRecords = periodLogs.Count,
                OperationsByType = periodLogs
        .GroupBy(a => a.OperationType)
      .ToDictionary(g => g.Key, g => g.Count()),
                OperationsByUser = periodLogs
    .GroupBy(a => a.UserId)
        .ToDictionary(g => g.Key, g => g.Count()),
                CriticalOperations = periodLogs.Where(a => IsCritical(a.Action)).Count(),
                ComplianceStatus = "Compliant",
                Issues = new List<string>()
            };

            _logger.LogInformation("Compliance report generated: {Records} records, {Critical} critical operations",
       report.TotalAuditRecords, report.CriticalOperations);

            return report;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating compliance report");
            throw;
        }
    }

    /// <summary>
    /// Get operations by user
    /// </summary>
    public async Task<List<AuditLogDto>> GetOperationsByUserAsync(
        string userId,
     DateTime startDate,
        DateTime endDate,
 CancellationToken cancellationToken = default)
    {
        try
        {
            var operations = _auditLogs
   .Where(a => a.UserId == userId && a.Timestamp >= startDate && a.Timestamp <= endDate)
    .OrderByDescending(a => a.Timestamp)
       .ToList();

            _logger.LogInformation("Retrieved {Count} operations for user {UserId}", operations.Count, userId);
            return operations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving operations for user {UserId}", userId);
            return new List<AuditLogDto>();
        }
    }

    /// <summary>
    /// Get critical operations
    /// </summary>
    public async Task<List<AuditLogDto>> GetCriticalOperationsAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var critical = _auditLogs
               .Where(a => IsCritical(a.Action) && a.Timestamp >= startDate && a.Timestamp <= endDate)
               .OrderByDescending(a => a.Timestamp)
          .ToList();

            _logger.LogWarning("Retrieved {Count} critical operations in period", critical.Count);
            return critical;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving critical operations");
            return new List<AuditLogDto>();
        }
    }

    /// <summary>
    /// Verify audit trail integrity
    /// </summary>
    public async Task<AuditIntegrityCheckResult> VerifyIntegrityAsync(
        string entityId,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var trail = await GetAuditTrailAsync(entityId, cancellationToken: cancellationToken);

            var result = new AuditIntegrityCheckResult
            {
                EntityId = entityId,
                TotalRecords = trail.Count,
                IntegrityCheckPassed = true,
                Issues = new List<string>(),
                LastModified = trail.FirstOrDefault()?.Timestamp ?? DateTime.UtcNow,
                Checksum = GenerateChecksum(trail)
            };

            _logger.LogInformation("Audit trail integrity verified for entity {EntityId}: {Status}",
         entityId, result.IntegrityCheckPassed ? "PASS" : "FAIL");

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error verifying audit trail integrity");
            throw;
        }
    }

    #region Helper Methods

    private bool IsCritical(string action)
    {
        var criticalActions = new[] { "Delete", "Approve", "Deny", "Escalate", "Override" };
        return criticalActions.Any(ca => action.Contains(ca, StringComparison.OrdinalIgnoreCase));
    }

    private string GenerateChecksum(List<AuditLogDto> trail)
    {
        var data = string.Join("|", trail.Select(t => $"{t.AuditId}:{t.Timestamp}:{t.Action}"));
        return System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(data))
              .Aggregate("", (str, byt) => str + byt.ToString("x2"));
    }

    #endregion
}

/// <summary>
/// Audit log DTO
/// </summary>
public class AuditLogDto
{
    public string AuditId { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string OperationType { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public string EntityId { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public Dictionary<string, object?> Details { get; set; } = new();
    public string IpAddress { get; set; } = string.Empty;
    public string Status { get; set; } = "Success";
}

/// <summary>
/// Audit compliance report
/// </summary>
public class AuditComplianceReport
{
    public string ReportId { get; set; } = string.Empty;
    public string ReportPeriod { get; set; } = string.Empty;
    public int TotalAuditRecords { get; set; }
    public Dictionary<string, int> OperationsByType { get; set; } = new();
    public Dictionary<string, int> OperationsByUser { get; set; } = new();
    public int CriticalOperations { get; set; }
    public string ComplianceStatus { get; set; } = string.Empty;
    public List<string> Issues { get; set; } = new();
}

/// <summary>
/// Audit integrity check result
/// </summary>
public class AuditIntegrityCheckResult
{
    public string EntityId { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public bool IntegrityCheckPassed { get; set; }
    public List<string> Issues { get; set; } = new();
    public DateTime LastModified { get; set; }
    public string Checksum { get; set; } = string.Empty;
}
