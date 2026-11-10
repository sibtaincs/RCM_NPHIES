namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// ClaimResponseDiagnosisExt entity - represents diagnosis information in Advanced Authorization response
/// FHIR Resource: ClaimResponse.extension (extension-diagnosis)
/// Note: These are diagnoses stored in extensions, not in the standard diagnosis array
/// </summary>
public class ClaimResponseDiagnosisExt : BaseEntity
{
    /// <summary>
    /// Claim Response ID this diagnosis belongs to
    /// </summary>
    public string ClaimResponseId { get; set; } = string.Empty;

    /// <summary>
    /// Claim Response
    /// </summary>
    public ClaimResponse? ClaimResponse { get; set; }

    /// <summary>
    /// Diagnosis sequence number
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Diagnosis code (e.g., "J44.8" for COPD)
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

    public string? OnAdmissionCode { get; set; }    
    public string? OnAdmissionSystem { get; set; }    

  /// <summary>
    /// Notes for this diagnosis
 /// </summary>
    public string? Notes { get; set; }
}
