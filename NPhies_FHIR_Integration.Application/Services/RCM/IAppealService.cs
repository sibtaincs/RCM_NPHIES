namespace NPhies_FHIR_Integration.Application.Services.RCM;

/// <summary>
/// Appeal Service Interface
/// Manages complete appeal workflow for denied claims
/// </summary>
public interface IAppealService
{
    // ========== APPEAL CREATION ==========
    /// <summary>
    /// Create a new appeal for a denied claim
    /// </summary>
    Task<CreateAppealResult> CreateAppealAsync(
        CreateAppealRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get appeal by ID
    /// </summary>
    Task<AppealDto?> GetAppealAsync(
   string appealId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all appeals for a claim
    /// </summary>
    Task<List<AppealDto>> GetClaimAppealsAsync(
  string claimId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get all appeals for a patient
    /// </summary>
    Task<List<AppealDto>> GetPatientAppealsAsync(
     string patientId,
            CancellationToken cancellationToken = default);

    // ========== APPEAL SUBMISSION ==========
    /// <summary>
    /// Submit an appeal (finalize before deadline)
    /// </summary>
    Task<SubmitAppealResult> SubmitAppealAsync(
 string appealId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Update appeal with additional information
    /// </summary>
    Task<UpdateAppealResult> UpdateAppealAsync(
   UpdateAppealRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Withdraw an appeal
    /// </summary>
    Task<WithdrawAppealResult> WithdrawAppealAsync(
            string appealId,
            string reason,
            CancellationToken cancellationToken = default);

    // ========== APPEAL ESCALATION ==========
    /// <summary>
    /// Escalate appeal to next level
    /// </summary>
    Task<EscalateAppealResult> EscalateAppealAsync(
        string appealId,
   string escalationReason,
        CancellationToken cancellationToken = default);

    // ========== APPEAL STATUS & TIMELINE ==========
    /// <summary>
    /// Get appeal status and timeline
    /// </summary>
    Task<AppealStatusDto> GetAppealStatusAsync(
      string appealId,
   CancellationToken cancellationToken = default);

    /// <summary>
    /// Get appeals nearing deadline
    /// </summary>
    Task<List<AppealDto>> GetAppealsNearingDeadlineAsync(
      int daysThreshold = 5,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get appeal statistics
    /// </summary>
    Task<AppealStatisticsDto> GetAppealStatisticsAsync(
        CancellationToken cancellationToken = default);

    // ========== DOCUMENTS ==========
    /// <summary>
    /// Attach document to appeal
    /// </summary>
    Task<AttachDocumentResult> AttachDocumentAsync(
        AttachDocumentRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Remove document from appeal
    /// </summary>
    Task<bool> RemoveDocumentAsync(
     string appealId,
        string documentId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get appeal documents
    /// </summary>
    Task<List<AppealDocumentDto>> GetAppealDocumentsAsync(
        string appealId,
        CancellationToken cancellationToken = default);
}

// ========== DTOs - REQUESTS ==========

/// <summary>
/// Request to create new appeal
/// </summary>
public class CreateAppealRequest
{
    public string ClaimId { get; set; } = string.Empty;
    public string ClaimResponseId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string InsurerId { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string ErrorCodeBeingAppealed { get; set; } = string.Empty;
    public string AppealReason { get; set; } = string.Empty;
    public string? SupportingDocumentation { get; set; }
}

/// <summary>
/// Request to update appeal
/// </summary>
public class UpdateAppealRequest
{
    public string AppealId { get; set; } = string.Empty;
    public string? AppealReason { get; set; }
    public string? SupportingDocumentation { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Request to attach document
/// </summary>
public class AttachDocumentRequest
{
    public string AppealId { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentTitle { get; set; } = string.Empty;
    public string? DocumentDescription { get; set; }
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

// ========== DTOs - RESPONSES ==========

/// <summary>
/// Result of creating appeal
/// </summary>
public class CreateAppealResult
{
    public bool IsSuccess { get; set; }
    public string? AppealId { get; set; }
    public string? AppealNumber { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? DeadlineDate { get; set; }
}

/// <summary>
/// Result of submitting appeal
/// </summary>
public class SubmitAppealResult
{
    public bool IsSuccess { get; set; }
    public string? AppealId { get; set; }
    public string? ConfirmationNumber { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime? ExpectedDecisionDate { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Result of updating appeal
/// </summary>
public class UpdateAppealResult
{
    public bool IsSuccess { get; set; }
    public string? AppealId { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Result of withdrawing appeal
/// </summary>
public class WithdrawAppealResult
{
    public bool IsSuccess { get; set; }
    public string? AppealId { get; set; }
    public DateTime? WithdrawnDate { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Result of escalating appeal
/// </summary>
public class EscalateAppealResult
{
    public bool IsSuccess { get; set; }
    public string? EscalatedAppealId { get; set; }
    public int NewAppealLevel { get; set; }
    public DateTime? NewDeadlineDate { get; set; }
    public string? Message { get; set; }
}

/// <summary>
/// Result of attaching document
/// </summary>
public class AttachDocumentResult
{
    public bool IsSuccess { get; set; }
    public string? DocumentId { get; set; }
    public string? Message { get; set; }
}

// ========== DTOs - DATA TRANSFER ==========

/// <summary>
/// Appeal data transfer object
/// </summary>
public class AppealDto
{
    public string AppealId { get; set; } = string.Empty;
    public string AppealNumber { get; set; } = string.Empty;
    public string ClaimId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string InsurerId { get; set; } = string.Empty;
    public string ErrorCodeBeingAppealed { get; set; } = string.Empty;
    public string ErrorDescription { get; set; } = string.Empty;
    public string AppealStatus { get; set; } = string.Empty;
    public int AppealLevel { get; set; }
    public string AppealReason { get; set; } = string.Empty;
    public DateTime DenialDate { get; set; }
    public DateTime AppealDeadlineDate { get; set; }
    public DateTime? AppealSubmittedDate { get; set; }
    public DateTime? ReviewCompletedDate { get; set; }
    public DateTime? ExpectedDecisionDate { get; set; }
    public string? AppealOutcome { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public bool IsActive { get; set; }
    public bool AllowsEscalation { get; set; }
    public int DaysRemainingToAppeal { get; set; }
}

/// <summary>
/// Appeal status with timeline
/// </summary>
public class AppealStatusDto
{
    public string AppealId { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
    public DateTime DenialDate { get; set; }
    public DateTime DeadlineDate { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public DateTime? ReceivedDate { get; set; }
    public DateTime? ReviewCompletedDate { get; set; }
    public DateTime? ExpectedDecisionDate { get; set; }
    public List<StatusChangeDto> StatusHistory { get; set; } = new();
    public int DaysRemainingToAppeal { get; set; }
    public int DaysSinceSubmission { get; set; }
}

/// <summary>
/// Status change in history
/// </summary>
public class StatusChangeDto
{
    public string Status { get; set; } = string.Empty;
    public DateTime ChangedDate { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public string? Reason { get; set; }
}

/// <summary>
/// Appeal document DTO
/// </summary>
public class AppealDocumentDto
{
    public string DocumentId { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentTitle { get; set; } = string.Empty;
    public string? DocumentDescription { get; set; }
    public long FileSizeBytes { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public DateTime AttachedDate { get; set; }
    public bool IsVerified { get; set; }
}

/// <summary>
/// Appeal statistics
/// </summary>
public class AppealStatisticsDto
{
    public int TotalAppeals { get; set; }
    public int ActiveAppeals { get; set; }
    public int ApprovedAppeals { get; set; }
    public int DeniedAppeals { get; set; }
    public int PartialAppeals { get; set; }
    public int WithdrawnAppeals { get; set; }
    public decimal AverageApprovalRate { get; set; }
    public decimal TotalAmountApproved { get; set; }
    public int AppealsNearingDeadline { get; set; }
}
