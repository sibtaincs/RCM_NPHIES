namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// ClaimResponseInsurance entity - represents insurance information in a claim response
/// FHIR Resource: ClaimResponse.insurance
/// </summary>
public class ClaimResponseInsurance : BaseEntity
{
    /// <summary>
    /// Claim Response ID this insurance info belongs to
    /// </summary>
    public string ClaimResponseId { get; set; } = string.Empty;

/// <summary>
    /// Claim Response
    /// </summary>
    public ClaimResponse? ClaimResponse { get; set; }

    /// <summary>
    /// Insurance sequence number
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Is this the focal/primary insurance
    /// </summary>
public bool Focal { get; set; } = true;

  /// <summary>
    /// Coverage ID
    /// </summary>
    public string? CoverageId { get; set; }

  /// <summary>
    /// Coverage
    /// </summary>
    public Coverage? Coverage { get; set; }

    /// <summary>
    /// Pre-authorization references (comma-separated or JSON)
    /// Array of pre-auth references for this insurance
    /// Example: "Pseudo-payer-Portal-Auth-02"
  /// </summary>
    public string? PreAuthReferences { get; set; }
}
