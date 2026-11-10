namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Payment reconciliation record for RCM
/// Note: Separate from PaymentReconciliation to avoid conflicts
/// </summary>
public class RCMPaymentReconciliation
{
    public Guid Id { get; set; }
    public string ReconciliationNumber { get; set; } = string.Empty;
    public Guid ClaimId { get; set; }
    public decimal ExpectedAmount { get; set; }
  public decimal ReceivedAmount { get; set; }
    public decimal VarianceAmount { get; set; }
  public string Status { get; set; } = "Unmatched";
    public DateTime? PaymentDate { get; set; }
    public DateTime ReconciliationDate { get; set; }
    public string? Notes { get; set; }
    public string? ReconciledBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation property
    public virtual Claim Claim { get; set; } = null!;
}
