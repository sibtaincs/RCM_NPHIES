namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// ClaimRelated entity - represents related/prior claims
/// FHIR Resource: Claim.related
/// Used to reference prior authorization, prior claim, etc.
/// </summary>
public class ClaimRelated : BaseEntity
{
    /// <summary>
    /// Claim ID this related claim reference belongs to
    /// </summary>
    public string ClaimId { get; set; } = string.Empty;

    /// <summary>
    /// Claim
    /// </summary>
    public Claim? Claim { get; set; }

    /// <summary>
 /// Related claim identifier system (e.g., "http://hmg.com/Takhassusi/Authorization")
    /// </summary>
    public string? RelatedClaimIdentifierSystem { get; set; }

    /// <summary>
    /// Related claim identifier value
    /// </summary>
    public string? RelatedClaimIdentifierValue { get; set; }

    /// <summary>
    /// Relationship to related claim (e.g., "prior", "associated", "sequel")
    /// </summary>
    public string? Relationship { get; set; }

    /// <summary>
    /// Relationship system URL (e.g., "http://nphies.sa/terminology/CodeSystem/related-claim-relationship")
    /// </summary>
 public string? RelationshipSystem { get; set; }

    /// <summary>
    /// Relationship display text
  /// </summary>
    public string? RelationshipDisplay { get; set; }

    /// <summary>
    /// Reference claim (if stored in same system)
    /// </summary>
    public string? ReferencedClaimId { get; set; }

    /// <summary>
    /// Notes about this related claim
    /// </summary>
 public string? Notes { get; set; }
}
