namespace NPhies_FHIR_Integration.Application.DTOs;

/// <summary>
/// DTO for ClaimDiagnosis entity - for read operations
/// </summary>
public class ClaimDiagnosisDto
{
    public int Id { get; set; }

    /// <summary>
    /// Claim ID
    /// </summary>
    public string ClaimId { get; set; } = string.Empty;

  /// <summary>
    /// Sequence number
 /// </summary>
    public int Sequence { get; set; }

/// <summary>
    /// Diagnosis code (ICD-10)
    /// </summary>
    public string DiagnosisCode { get; set; } = string.Empty;

    /// <summary>
    /// Diagnosis code system
    /// </summary>
    public string? DiagnosisSystem { get; set; }

    /// <summary>
    /// Diagnosis type
  /// </summary>
    public string? DiagnosisType { get; set; }

    /// <summary>
    /// On admission code (y/n/u)
    /// </summary>
 public string? OnAdmissionCode { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
  public string? Notes { get; set; }
}

/// <summary>
/// DTO for creating a ClaimDiagnosis
/// </summary>
public class CreateClaimDiagnosisDto
{
  /// <summary>
    /// Claim ID
  /// </summary>
    public string ClaimId { get; set; } = string.Empty;

    /// <summary>
    /// Sequence number
    /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Diagnosis code (ICD-10)
    /// </summary>
    public string DiagnosisCode { get; set; } = string.Empty;

    /// <summary>
    /// Diagnosis code system
    /// </summary>
    public string? DiagnosisSystem { get; set; }

    /// <summary>
/// Diagnosis type
 /// </summary>
    public string? DiagnosisType { get; set; }

    /// <summary>
    /// On admission code (y/n/u)
    /// </summary>
    public string? OnAdmissionCode { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for updating a ClaimDiagnosis
/// </summary>
public class UpdateClaimDiagnosisDto
{
    /// <summary>
    /// Diagnosis type
    /// </summary>
    public string? DiagnosisType { get; set; }

    /// <summary>
    /// On admission code (y/n/u)
 /// </summary>
    public string? OnAdmissionCode { get; set; }

  /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }
}
