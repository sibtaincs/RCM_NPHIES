namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// AppealRequest entity - Represents a claim denial appeal
/// Tracks appeal requests submitted to insurance companies
/// Reference: NPHIES Appeal Management flows
/// </summary>
public class AppealRequest : BaseEntity
{
    /// <summary>
    /// Unique appeal identifier in the system
    /// Format: APPEAL-YYYYMMDD-XXXXXX
    /// </summary>
    public string AppealNumber { get; set; } = string.Empty;

  /// <summary>
    /// Identifier system for appeal tracking
    /// Standard: http://nphies.sa/identifier/appeal-id
    /// </summary>
    public string AppealIdentifierSystem { get; set; } = string.Empty;

    /// <summary>
    /// Appeal identifier value
    /// Unique reference number
    /// </summary>
    public string AppealIdentifierValue { get; set; } = string.Empty;

    // ========== RELATIONSHIP FIELDS ==========
    /// <summary>
    /// Reference to the original claim being appealed
    /// </summary>
    public string ClaimId { get; set; } = string.Empty;
    public Claim? Claim { get; set; }

    /// <summary>
    /// Reference to the claim response (denial)
    /// </summary>
    public string ClaimResponseId { get; set; } = string.Empty;
    public ClaimResponse? ClaimResponse { get; set; }

    /// <summary>
    /// Patient who is appealing
    /// </summary>
    public string PatientId { get; set; } = string.Empty;
    public Patient? Patient { get; set; }

    /// <summary>
/// Insurance company (respondent)
/// </summary>
    public string InsurerId { get; set; } = string.Empty;
    public Organization? Insurer { get; set; }

  /// <summary>
    /// Provider/Requestor submitting appeal
  /// </summary>
    public string ProviderId { get; set; } = string.Empty;
    public Organization? Provider { get; set; }

    // ========== APPEAL DETAILS ==========
    /// <summary>
    /// Appeal status: submitted, acknowledged, under-review, approved, denied, withdrawn
    /// </summary>
    public string AppealStatus { get; set; } = "submitted";

    /// <summary>
    /// Appeal level: 1 (first), 2 (second), 3 (external)
    /// Most appeals are Level 1
    /// </summary>
    public int AppealLevel { get; set; } = 1;

    /// <summary>
 /// Error code being appealed
 /// Example: "AD-1-1", "CV-1-1"
    /// </summary>
    public string ErrorCodeBeingAppealed { get; set; } = string.Empty;

    /// <summary>
    /// Error code description
    /// </summary>
    public string? ErrorDescription { get; set; }

    /// <summary>
    /// Reason for appeal (why provider disagrees with denial)
    /// </summary>
    public string AppealReason { get; set; } = string.Empty;

    /// <summary>
    /// Supporting documentation provided
    /// </summary>
    public string? SupportingDocumentation { get; set; }

    // ========== TIMELINE FIELDS ==========
    /// <summary>
  /// Date claim was denied
    /// </summary>
    public DateTime DenialDate { get; set; }

    /// <summary>
    /// Appeal must be submitted by this date
    /// Calculated as DenialDate + StandardAppealDays
    /// </summary>
    public DateTime AppealDeadlineDate { get; set; }

    /// <summary>
    /// Date appeal was submitted
    /// </summary>
    public DateTime? AppealSubmittedDate { get; set; }

    /// <summary>
    /// Date appeal was received/acknowledged by insurer
    /// </summary>
    public DateTime? ReceivedDate { get; set; }

    /// <summary>
    /// Date appeal review was completed
    /// </summary>
    public DateTime? ReviewCompletedDate { get; set; }

    /// <summary>
    /// Expected date for appeal decision
    /// Usually 30 days from submission
 /// </summary>
    public DateTime? ExpectedDecisionDate { get; set; }

    // ========== APPEAL DECISION ==========
    /// <summary>
    /// Appeal outcome: approved, denied, partial
    /// </summary>
    public string? AppealOutcome { get; set; }

    /// <summary>
    /// Amount approved in appeal (if approved)
    /// </summary>
 public decimal? ApprovedAmount { get; set; }

    /// <summary>
    /// Decision rationale/explanation
    /// </summary>
    public string? DecisionExplanation { get; set; }

    /// <summary>
    /// Can this decision be appealed further (to Level 2/3)?
    /// </summary>
    public bool AllowsEscalation { get; set; } = true;

    /// <summary>
    /// If escalated, reference to next level appeal
    /// </summary>
    public string? EscalatedAppealId { get; set; }

    // ========== STATUS TRACKING ==========
  /// <summary>
    /// Is appeal active (not closed/resolved)?
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Has this appeal been withdrawn?
    /// </summary>
    public bool IsWithdrawn { get; set; } = false;

    /// <summary>
    /// Date appeal was withdrawn (if applicable)
    /// </summary>
    public DateTime? WithdrawnDate { get; set; }

    /// <summary>
    /// Reason for withdrawal
    /// </summary>
    public string? WithdrawalReason { get; set; }

    // ========== AUDIT FIELDS ==========
    /// <summary>
    /// Internal reference number for tracking
/// </summary>
    public string? InternalReferenceNumber { get; set; }

    /// <summary>
    /// Notes about this appeal
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Last status update date
    /// </summary>
    public DateTime LastStatusUpdateDate { get; set; } = DateTime.UtcNow;

    // ========== COLLECTIONS ==========
 /// <summary>
    /// Timeline/audit trail of appeal status changes
    /// </summary>
public ICollection<AppealStatusHistory> StatusHistory { get; set; } = new List<AppealStatusHistory>();

    /// <summary>
    /// Supporting documents attached to appeal
    /// </summary>
    public ICollection<AppealDocument> AttachedDocuments { get; set; } = new List<AppealDocument>();

    // ========== HELPER METHODS ==========
    /// <summary>
    /// Is appeal within deadline?
  /// </summary>
    public bool IsWithinDeadline() => DateTime.UtcNow <= AppealDeadlineDate;

    /// <summary>
    /// Days remaining to submit appeal
  /// </summary>
    public int DaysRemainingToAppeal()
    {
    var remaining = (AppealDeadlineDate - DateTime.UtcNow).Days;
      return Math.Max(0, remaining);
    }

    /// <summary>
    /// Has appeal been decided?
    /// </summary>
    public bool IsDecided() => !string.IsNullOrEmpty(AppealOutcome);

    /// <summary>
    /// Appeal age in days since submission
    /// </summary>
public int AppealAgeDays()
    {
        var submittedDate = AppealSubmittedDate ?? DateTime.UtcNow;
     return (int)(DateTime.UtcNow - submittedDate).TotalDays;
    }

    /// <summary>
    /// Get appeal summary
    /// </summary>
    public string GetSummary() =>
 $"Appeal {AppealNumber}: {ErrorCodeBeingAppealed} - {AppealStatus} (Level {AppealLevel})";
}

/// <summary>
/// AppealStatusHistory - Tracks appeal status changes over time
/// Provides audit trail of appeal lifecycle
/// </summary>
public class AppealStatusHistory : BaseEntity
{
    /// <summary>
    /// Reference to appeal
    /// </summary>
    public string AppealId { get; set; } = string.Empty;
    public AppealRequest? Appeal { get; set; }

    /// <summary>
    /// Status at this point in time
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Who/what triggered this status change
    /// Example: "system", "provider", "insurer", "automated"
    /// </summary>
    public string ChangedBy { get; set; } = string.Empty;

    /// <summary>
    /// Reason for status change
    /// </summary>
    public string? ChangeReason { get; set; }

    /// <summary>
    /// When this status change occurred
    /// </summary>
    public DateTime StatusChangeDate { get; set; } = DateTime.UtcNow;

/// <summary>
    /// Any comments about this status change
    /// </summary>
    public string? Comments { get; set; }
}

/// <summary>
/// AppealDocument - Supporting documents for appeal
/// Stores references to uploaded/attached documents
/// </summary>
