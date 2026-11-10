namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// ErrorCodeMaster - NPHIES error codes (1,682 codes)
/// Represents all NPHIES adjudication error codes used in claim adjudication
/// Maps to NPHIES adjudication-error CodeSystem
/// Reference: https://portal.nphies.sa/ig/index.html
/// </summary>
public class ErrorCodeMaster : BaseEntity
{
    /// <summary>
    /// Error code identifier (e.g., "AD-1-1", "AD-2-3", "CV-1-1")
    /// NPHIES error code format: [Category]-[SubCategory]-[Number]
    /// Examples:
    ///   AD-1-1: Diagnosis inconsistent with procedure
    ///   CV-1-1: Coverage not found
    ///   AU-1-1: Prior authorization required
    ///   SB-1-1: Duplicate claim
    ///   BF-1-1: Service not covered
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable error description in English
    /// Example: "Diagnosis inconsistent with procedure"
    /// </summary>
    public string ErrorDescription { get; set; } = string.Empty;

    /// <summary>
    /// Error category classifying the type of error
    /// Values: "adjudication", "coverage", "authorization", "submission", "benefit", "network", "other"
    /// Helps organize and filter errors by type
    /// </summary>
    public string ErrorCategory { get; set; } = string.Empty;

    /// <summary>
    /// Error severity level indicating impact
    /// Values: "Error" (blocking), "Warning" (non-blocking), "Info" (informational)
    /// Default: "Error"
    /// </summary>
    public string Severity { get; set; } = "Error";

    /// <summary>
    /// Can this error be recovered by resubmission with corrections?
  /// True: Claim can be corrected and resubmitted
    /// False: Denial is permanent (e.g., service not covered)
    /// Default: false
    /// </summary>
    public bool IsRecoverable { get; set; }

  /// <summary>
    /// Can this denial be appealed?
    /// True: Provider can file appeal
    /// False: No appeal allowed
    /// Default: true (most codes allow appeal)
    /// </summary>
    public bool AllowsAppeal { get; set; } = true;

    /// <summary>
    /// Standard appeal deadline in calendar days from denial date
    /// Common values: 60 (most common), 30 (expedited), 180 (external review)
    /// Default: 60 days
 /// </summary>
    public int StandardAppealDays { get; set; } = 60;

    /// <summary>
    /// Recommended action to resolve this error
    /// Example: "Update diagnosis to be consistent with procedure"
    /// Helps providers understand corrective actions
    /// </summary>
    public string? RecommendedAction { get; set; }

    /// <summary>
 /// NPHIES CodeSystem URL for this error code
    /// Standard value: "http://nphies.sa/terminology/CodeSystem/adjudication-error"
    /// Used for FHIR compliance and code system identification
    /// </summary>
    public string NphiesCodeSystem { get; set; } = "http://nphies.sa/terminology/CodeSystem/adjudication-error";

    /// <summary>
    /// Impact on claim adjudication if this error occurs
    /// Values: "Approved", "Denied", "Pending", "Partial"
    /// </summary>
  public string? AdjudicationImpact { get; set; }

    /// <summary>
/// System-specific metadata or notes about this error code
    /// Used for internal documentation or special handling
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Whether this error code is currently active and should be used
    /// Inactive codes are kept for historical reference
    /// Default: true
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Date this error code was created
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last date this error code was updated
    /// </summary>
    public DateTime? LastModifiedDate { get; set; }
}
