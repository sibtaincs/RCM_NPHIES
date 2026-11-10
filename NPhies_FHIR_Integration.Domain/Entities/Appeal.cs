namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Appeal entity for claim denials
/// </summary>
public class Appeal
{
    public Guid Id { get; set; }
    public string AppealNumber { get; set; } = string.Empty;
    public Guid ClaimId { get; set; }
    public string Status { get; set; } = "Pending"; // Pending, UnderReview, Approved, Denied, Withdrawn
    public string? DenialReason { get; set; }
    public string AppealReason { get; set; } = string.Empty;
    public DateTime SubmittedDate { get; set; }
    public DateTime? DecisionDate { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime AppealDeadline { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    // Navigation properties
public virtual Claim Claim { get; set; } = null!;
    public virtual ICollection<AppealDocument> Documents { get; set; } = new List<AppealDocument>();
}
