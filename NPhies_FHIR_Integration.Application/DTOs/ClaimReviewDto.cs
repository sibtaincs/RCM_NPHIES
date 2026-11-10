namespace NPhies_FHIR_Integration.Application.DTOs;

/// <summary>
/// Request payload for a technical or medical claim review action.
/// </summary>
public class ClaimReviewDto
{
    /// <summary>
    /// Review decision: "Approved" or "Rejected".
    /// </summary>
    public required string Decision { get; set; }

    /// <summary>
    /// ID of the reviewer performing the action.
    /// </summary>
    public string? ReviewerId { get; set; }

    /// <summary>
    /// Optional reviewer notes / rejection reason.
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// Read model exposing the dual-approval review status of a claim.
/// </summary>
public class ClaimReviewStatusDto
{
  public string ClaimId { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;

    public string TechnicalReviewStatus { get; set; } = "Pending";
    public string? TechnicalReviewerId { get; set; }
    public DateTime? TechnicalReviewedAt { get; set; }
    public string? TechnicalReviewNotes { get; set; }

public string MedicalReviewStatus { get; set; } = "Pending";
    public string? MedicalReviewerId { get; set; }
    public DateTime? MedicalReviewedAt { get; set; }
    public string? MedicalReviewNotes { get; set; }

    /// <summary>
    /// True only when both reviews are Approved (claim is submit-eligible).
    /// </summary>
    public bool IsApprovedForSubmission { get; set; }
}
