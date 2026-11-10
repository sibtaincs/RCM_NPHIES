namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// ClaimCareTeam entity - represents care team members involved in a claim
/// FHIR Resource: Claim.careTeam
/// </summary>
public class ClaimCareTeam : BaseEntity
{
    /// <summary>
/// Claim ID this care team member belongs to
    /// </summary>
    public string ClaimId { get; set; } = string.Empty;

    /// <summary>
    /// Claim
    /// </summary>
    public Claim? Claim { get; set; }

    /// <summary>
    /// Care team member sequence number
    /// </summary>
    public int Sequence { get; set; }

/// <summary>
    /// Practitioner ID
    /// </summary>
    public string PractitionerId { get; set; } = string.Empty;

  /// <summary>
    /// Practitioner
  /// </summary>
    public Practitioner? Practitioner { get; set; }

    /// <summary>
    /// Care team role (e.g., "primary", "secondary", "surgeon", "anaesthetist", "orthopedist", "assistant")
    /// </summary>
    public string? Role { get; set; }

    /// <summary>
    /// Role system URL
    /// </summary>
    public string? RoleSystem { get; set; }

  /// <summary>
    /// Role display text
    /// </summary>
  public string? RoleDisplay { get; set; }

    /// <summary>
    /// Qualification/specialty code (e.g., "08.00" for specific medical specialty)
    /// </summary>
    public string? Qualification { get; set; }

    /// <summary>
    /// Qualification system URL
    /// </summary>
    public string? QualificationSystem { get; set; }

    /// <summary>
    /// Qualification display text
    /// </summary>
    public string? QualificationDisplay { get; set; }

 /// <summary>
    /// Notes for this care team member
    /// </summary>
    public string? Notes { get; set; }
}
