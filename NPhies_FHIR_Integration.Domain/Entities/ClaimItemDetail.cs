namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// ClaimItemDetail entity - represents sub-items/details within a claim item
/// FHIR Resource: Claim.item.detail
/// </summary>
public class ClaimItemDetail : BaseEntity
{
    /// <summary>
    /// Claim Item ID this detail belongs to
    /// </summary>
  public string ClaimItemId { get; set; } = string.Empty;

    /// <summary>
    /// Claim Item
    /// </summary>
    public ClaimItem? ClaimItem { get; set; }

    /// <summary>
    /// Detail sequence number
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Product or service code for this detail
    /// </summary>
    public string ProductOrServiceCode { get; set; } = string.Empty;

    /// <summary>
    /// Product or service system (e.g., "http://nphies.sa/terminology/CodeSystem/services")
    /// </summary>
    public string? ProductOrServiceSystem { get; set; }

    /// <summary>
    /// Product or service display text
    /// </summary>
    public string? ProductOrServiceDisplay { get; set; }

    /// <summary>
    /// Alternative coding system (e.g., provider's own system)
    /// </summary>
    public string? AltProductOrServiceCode { get; set; }

    /// <summary>
    /// Alternative coding system URL
    /// </summary>
    public string? AltProductOrServiceSystem { get; set; }

    /// <summary>
    /// Quantity of service
  /// </summary>
    public decimal? Quantity { get; set; }

    /// <summary>
    /// Unit price
    /// </summary>
 public decimal? UnitPrice { get; set; }

    /// <summary>
    /// Total net amount for this detail
    /// </summary>
    public decimal? Net { get; set; }

    /// <summary>
    /// Notes for this detail
    /// </summary>
    public string? Notes { get; set; }
}
