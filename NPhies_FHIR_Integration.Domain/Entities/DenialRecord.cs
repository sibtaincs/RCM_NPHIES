namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Record of claim denials
/// </summary>
public class DenialRecord
{
    public Guid Id { get; set; }
    public Guid ClaimId { get; set; }
  public int ClaimItemSequence { get; set; }
    public string DenialCode { get; set; } = string.Empty;
    public string DenialReason { get; set; } = string.Empty;
    public string? DenialCategory { get; set; }
    public decimal DeniedAmount { get; set; }
 public DateTime DenialDate { get; set; }
    public bool CanAppeal { get; set; }
    public DateTime? AppealDeadline { get; set; }
    public bool IsAppealed { get; set; }
    public Guid? AppealId { get; set; }
    public DateTime CreatedAt { get; set; }

    // Navigation properties
    public virtual Claim Claim { get; set; } = null!;
    public virtual Appeal? Appeal { get; set; }
}
