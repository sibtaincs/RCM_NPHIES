namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// ClaimResponseTotal entity - represents total adjustments in a claim response
/// FHIR Resource: ClaimResponse.total
/// Aggregates all adjudication totals (benefit, submitted, patient responsibility, etc.)
/// </summary>
public class ClaimResponseTotal : BaseEntity
{
    /// <summary>
    /// Claim Response ID this total belongs to
    /// </summary>
    public string ClaimResponseId { get; set; } = string.Empty;

    /// <summary>
    /// Claim Response
    /// </summary>
    public ClaimResponse? ClaimResponse { get; set; }

    /// <summary>
    /// Total category (e.g., "benefit", "submitted", "copay", "deductible", "coinsurance")
    /// System: http://terminology.hl7.org/CodeSystem/adjudication
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Category system URL
    /// </summary>
    public string? CategorySystem { get; set; }

    /// <summary>
    /// Category display text
    /// </summary>
    public string? CategoryDisplay { get; set; }

    /// <summary>
    /// Total amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
 /// Currency (e.g., "SAR")
    /// </summary>
    public string Currency { get; set; } = "SAR";

    /// <summary>
    /// Sequence/order of this total
    /// </summary>
    public int? Sequence { get; set; }

    /// <summary>
    /// Notes for this total
  /// </summary>
    public string? Notes { get; set; }
}
