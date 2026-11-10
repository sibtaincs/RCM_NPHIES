using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM;

/// <summary>
/// Security Hardening Service Interface
/// Manages security controls, threat detection, and compliance monitoring
/// </summary>
public interface ISecurityHardeningService
{
    /// <summary>
    /// Initialize security controls
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Initialization result</returns>
    Task<SecurityInitializationResult> InitializeSecurityControlsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Enforce rate limiting
    /// </summary>
    /// <param name="clientId">Client identifier</param>
    /// <param name="maxRequestsPerMinute">Max requests per minute</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Rate limiting result</returns>
    Task<RateLimitingResult> EnforceRateLimitingAsync(
        string clientId,
        int maxRequestsPerMinute = 100,
 CancellationToken cancellationToken = default);

    /// <summary>
    /// Validate input security
    /// </summary>
    /// <param name="input">Input to validate</param>
    /// <param name="inputType">Type of input</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Validation result</returns>
    Task<InputValidationResult> ValidateInputSecurityAsync(
     string input,
    string inputType,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Detect security threats
    /// </summary>
    /// <param name="eventData">Security event data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Threat detection result</returns>
    Task<ThreatDetectionResult> DetectSecurityThreatsAsync(
        SecurityEventData eventData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate security audit report
    /// </summary>
    /// <param name="fromDate">From date</param>
    /// <param name="toDate">To date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Security audit report</returns>
    Task<SecurityAuditReport> GenerateSecurityAuditReportAsync(
        DateTime fromDate,
        DateTime toDate,
     CancellationToken cancellationToken = default);

    /// <summary>
    /// Implement data encryption
    /// </summary>
    /// <param name="dataToEncrypt">Data to encrypt</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Encryption result</returns>
    Task<EncryptionResult> EncryptSensitiveDataAsync(
        string dataToEncrypt,
      CancellationToken cancellationToken = default);
}

/// <summary>
/// Security Initialization Result
/// </summary>
public class SecurityInitializationResult
{
    /// <summary>
    /// Whether initialization was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Number of security controls initialized
  /// </summary>
    public int SecurityControlsInitialized { get; set; }

    /// <summary>
    /// Message
    /// </summary>
    public string Message { get; set; } = string.Empty;

  /// <summary>
    /// Security controls configured
    /// </summary>
    public List<string> ControlsConfigured { get; set; } = new();
}

/// <summary>
/// Rate Limiting Result
/// </summary>
public class RateLimitingResult
{
    /// <summary>
    /// Client ID
  /// </summary>
    public string ClientId { get; set; } = string.Empty;

    /// <summary>
    /// Is request allowed
    /// </summary>
    public bool IsAllowed { get; set; }

  /// <summary>
    /// Current request count
/// </summary>
    public int CurrentRequestCount { get; set; }

    /// <summary>
    /// Maximum allowed requests per minute
    /// </summary>
    public int MaxAllowedPerMinute { get; set; }

    /// <summary>
    /// Remaining requests
    /// </summary>
    public int RemainingRequests { get; set; }

    /// <summary>
    /// Reset time
    /// </summary>
    public DateTime ResetTime { get; set; }

    /// <summary>
    /// Message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}

/// <summary>
/// Input Validation Result
/// </summary>
public class InputValidationResult
{
  /// <summary>
 /// Is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Input type validated
  /// </summary>
    public string InputType { get; set; } = string.Empty;

    /// <summary>
    /// Security violations found
    /// </summary>
    public List<string> SecurityViolations { get; set; } = new();

    /// <summary>
    /// Input sanitized
    /// </summary>
    public string SanitizedInput { get; set; } = string.Empty;

    /// <summary>
    /// Validation details
  /// </summary>
    public List<string> ValidationDetails { get; set; } = new();
}

/// <summary>
/// Security Event Data
/// </summary>
public class SecurityEventData
{
    /// <summary>
    /// Event type
    /// </summary>
 public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// Source IP
    /// </summary>
    public string SourceIp { get; set; } = string.Empty;

    /// <summary>
    /// User ID
    /// </summary>
    public string UserId { get; set; } = string.Empty;

/// <summary>
    /// Event description
    /// </summary>
    public string EventDescription { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Additional context
    /// </summary>
  public Dictionary<string, object> Context { get; set; } = new();
}

/// <summary>
/// Threat Detection Result
/// </summary>
public class ThreatDetectionResult
{
    /// <summary>
    /// Threat detected
    /// </summary>
    public bool ThreatDetected { get; set; }

    /// <summary>
    /// Threat level (Low, Medium, High, Critical)
    /// </summary>
    public string ThreatLevel { get; set; } = string.Empty;

    /// <summary>
    /// Threat type
    /// </summary>
    public string ThreatType { get; set; } = string.Empty;

    /// <summary>
    /// Threat description
    /// </summary>
    public string ThreatDescription { get; set; } = string.Empty;

    /// <summary>
    /// Recommended actions
    /// </summary>
    public List<string> RecommendedActions { get; set; } = new();

    /// <summary>
    /// Detection confidence (0-100)
    /// </summary>
    public decimal ConfidencePercentage { get; set; }
}

/// <summary>
/// Security Audit Report
/// </summary>
public class SecurityAuditReport
{
    /// <summary>
    /// Report ID
    /// </summary>
 public string ReportId { get; set; } = string.Empty;

    /// <summary>
    /// Report date
    /// </summary>
    public DateTime ReportDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Reporting period
    /// </summary>
    public string ReportingPeriod { get; set; } = string.Empty;

    /// <summary>
    /// Total security events logged
    /// </summary>
    public int TotalSecurityEventsLogged { get; set; }

    /// <summary>
    /// Critical incidents
    /// </summary>
    public int CriticalIncidents { get; set; }

    /// <summary>
    /// Security score (0-100)
    /// </summary>
 public decimal SecurityScore { get; set; }

    /// <summary>
    /// Incidents by type
    /// </summary>
 public Dictionary<string, int> IncidentsByType { get; set; } = new();

    /// <summary>
    /// Access violations
    /// </summary>
    public int AccessViolations { get; set; }

    /// <summary>
    /// Data exfiltration attempts
    /// </summary>
    public int DataExfiltrationAttempts { get; set; }

    /// <summary>
    /// Authentication failures
    /// </summary>
    public int AuthenticationFailures { get; set; }

    /// <summary>
    /// Recommendations
    /// </summary>
    public List<string> Recommendations { get; set; } = new();
}

/// <summary>
/// Encryption Result
/// </summary>
public class EncryptionResult
{
  /// <summary>
    /// Whether encryption was successful
    /// </summary>
 public bool IsSuccessful { get; set; }

    /// <summary>
    /// Encrypted data (base64 encoded)
    /// </summary>
    public string EncryptedData { get; set; } = string.Empty;

    /// <summary>
    /// Encryption method used
    /// </summary>
    public string EncryptionMethod { get; set; } = string.Empty;

    /// <summary>
    /// Encryption key identifier
    /// </summary>
    public string KeyId { get; set; } = string.Empty;

    /// <summary>
    /// Message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Encryption timestamp
    /// </summary>
    public DateTime EncryptionTime { get; set; } = DateTime.UtcNow;
}
