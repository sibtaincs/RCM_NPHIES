namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// ClaimResponseAddItem entity - represents line items ADDED by insurer in advanced authorization
/// FHIR Resource: ClaimResponse.addItem
/// Used in Advanced Authorization (not in standard ClaimResponse)
/// </summary>
public class ClaimResponseAddItem : BaseEntity
{
    /// <summary>
    /// Claim Response ID this item belongs to
    /// </summary>
    public string ClaimResponseId { get; set; } = string.Empty;

    /// <summary>
    /// Claim Response
    /// </summary>
    public ClaimResponse? ClaimResponse { get; set; }

  /// <summary>
    /// Item sequence number
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Product or service code (e.g., medication code "99660000000017")
    /// </summary>
    public string? ProductOrServiceCode { get; set; }

    /// <summary>
    /// Product or service system (e.g., "http://nphies.sa/terminology/CodeSystem/medication-codes")
    /// </summary>
    public string? ProductOrServiceSystem { get; set; }

    /// <summary>
    /// Product or service display text (e.g., "Epnone")
    /// </summary>
    public string? ProductOrServiceDisplay { get; set; }

    /// <summary>
    /// Is this item approved
    /// </summary>
    public bool IsApproved { get; set; } = false;

    /// <summary>
    /// Approved quantity (e.g., 3 units)
 /// </summary>
    public int? ApprovedQuantity { get; set; }

    /// <summary>
 /// Benefit amount
    /// </summary>
    public decimal? BenefitAmount { get; set; }

    /// <summary>
    /// Benefit currency
    /// </summary>
    public string? BenefitCurrency { get; set; } = "SAR";

    /// <summary>
    /// Submitted amount
    /// </summary>
    public decimal? SubmittedAmount { get; set; }

    /// <summary>
    /// Diagnosis sequence this item relates to
    /// </summary>
    public int? DiagnosisSequence { get; set; }

    /// <summary>
/// Supporting information sequence this item relates to
    /// </summary>
    public int? InformationSequence { get; set; }

    /// <summary>
    /// Is this a maternity-related item
    /// </summary>
    public bool IsMaternity { get; set; } = false;

    /// <summary>
    /// Notes for this item
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Collection of adjudications for this item
    /// </summary>
    public ICollection<ClaimResponseAdjudication> Adjudications { get; set; } = new List<ClaimResponseAdjudication>();
}
