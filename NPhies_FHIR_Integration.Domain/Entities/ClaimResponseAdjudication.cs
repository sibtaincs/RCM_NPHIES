namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// ClaimResponseAdjudication entity - represents adjudication information for response items
/// FHIR Resource: ClaimResponse.addItem.adjudication
/// </summary>
public class ClaimResponseAdjudication : BaseEntity
{
  /// <summary>
    /// Claim Response Add Item ID this adjudication belongs to
    /// </summary>
    public string ClaimResponseAddItemId { get; set; } = string.Empty;

    /// <summary>
    /// Claim Response Add Item
    /// </summary>
public ClaimResponseAddItem? ClaimResponseAddItem { get; set; }

    /// <summary>
    /// Adjudication category (e.g., "benefit", "submitted", "approved-quantity", "copay")
    /// System: http://terminology.hl7.org/CodeSystem/adjudication or http://nphies.sa/terminology/CodeSystem/ksa-adjudication
    /// </summary>
    public string AdjudicationCategory { get; set; } = string.Empty;

    /// <summary>
    /// Adjudication category system URL
    /// </summary>
    public string? AdjudicationSystem { get; set; }

    /// <summary>
    /// Adjudication category display text
    /// </summary>
    public string? AdjudicationDisplay { get; set; }

    /// <summary>
    /// Amount value (for monetary adjudications like "benefit", "submitted")
    /// </summary>
    public decimal? Amount { get; set; }

    /// <summary>
  /// Amount currency (e.g., "SAR")
    /// </summary>
    public string? Currency { get; set; } = "SAR";

    /// <summary>
    /// Quantity value (for non-monetary adjudications like "approved-quantity")
    /// </summary>
    public int? QuantityValue { get; set; }

    /// <summary>
    /// Notes for this adjudication
    /// </summary>
    public string? Notes { get; set; }
}
