namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// ClaimResponseSupportingInfoExt entity - represents supporting information in Advanced Authorization response
/// FHIR Resource: ClaimResponse.extension (extension-supportingInfo)
/// Note: These are supporting info stored in extensions
/// </summary>
public class ClaimResponseSupportingInfoExt : BaseEntity
{
    /// <summary>
    /// Claim Response ID this supporting info belongs to
    /// </summary>
    public string ClaimResponseId { get; set; } = string.Empty;

    /// <summary>
    /// Claim Response
    /// </summary>
    public ClaimResponse? ClaimResponse { get; set; }

    /// <summary>
    /// Supporting information sequence number
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Supporting information category (e.g., "days-supply", "chief-complaint", "vital-signs")
    /// System: http://nphies.sa/terminology/CodeSystem/claim-information-category
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
    /// Code value (for coded supporting info like "N/A" in chief-complaint)
    /// </summary>
    public string? CodeValue { get; set; }

    /// <summary>
    /// Code system URL
    /// </summary>
    public string? CodeSystem { get; set; }

    /// <summary>
    /// String/text value
    /// </summary>
    public string? StringValue { get; set; }

    /// <summary>
    /// Quantity value (e.g., 90 for days-supply)
    /// </summary>
    public decimal? QuantityValue { get; set; }

    /// <summary>
    /// Quantity unit (e.g., "d" for days, "ml" for milliliters)
    /// </summary>
    public string? QuantityUnit { get; set; }

    /// <summary>
    /// Quantity unit system (e.g., "http://unitsofmeasure.org")
    /// </summary>
    public string? QuantitySystem { get; set; }

    /// <summary>
    /// Notes for this supporting info
    /// </summary>
    public string? Notes { get; set; }
}
