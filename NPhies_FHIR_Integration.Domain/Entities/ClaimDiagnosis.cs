namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// ClaimDiagnosis entity - represents diagnosis codes in a claim
/// FHIR Resource: Claim.diagnosis
/// </summary>
public class ClaimDiagnosis : BaseEntity
{
    /// <summary>
    /// Claim ID this diagnosis belongs to
    /// </summary>
    public string ClaimId { get; set; } = string.Empty;

    /// <summary>
    /// Claim
    /// </summary>
    public Claim? Claim { get; set; }

    /// <summary>
    /// Diagnosis sequence number
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
/// Diagnosis code (e.g., "R10.4" for abdominal pain)
/// </summary>
    public string DiagnosisCode { get; set; } = string.Empty;

    /// <summary>
    /// Diagnosis system (e.g., "http://hl7.org/fhir/sid/icd-10-am")
    /// </summary>
    public string? DiagnosisSystem { get; set; }

    /// <summary>
    /// Diagnosis display text
    /// </summary>
    public string? DiagnosisDisplay { get; set; }

    /// <summary>
    /// Diagnosis type (e.g., "principal", "secondary", "admitting", "discharge")
    /// </summary>
    public string? DiagnosisType { get; set; }

    /// <summary>
  /// Diagnosis type system URL
    /// </summary>
    public string? DiagnosisTypeSystem { get; set; }

    /// <summary>
    /// On admission code (e.g., "y" for yes, "n" for no, "u" for unknown)
    /// </summary>
    public string? OnAdmissionCode { get; set; }

  /// <summary>
    /// On admission system URL
    /// </summary>
    public string? OnAdmissionSystem { get; set; }

    /// <summary>
    /// NPHIES condition-onset code (COEA, CNNA, NR); required on institutional claims.
    /// </summary>
    public string? ConditionOnsetCode { get; set; }

    /// <summary>
    /// Notes for this diagnosis
    /// </summary>
    public string? Notes { get; set; }
}
