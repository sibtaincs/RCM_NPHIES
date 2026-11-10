namespace NPhies_FHIR_Integration.Application.DTOs;

/// <summary>
/// DTO for ClaimResponseInsurance entity - for read operations
/// </summary>
public class ClaimResponseInsuranceDto
{
    public int Id { get; set; }

    /// <summary>
 /// Claim Response ID
    /// </summary>
    public string ClaimResponseId { get; set; } = string.Empty;

    /// <summary>
    /// Sequence number
 /// </summary>
    public int Sequence { get; set; }

    /// <summary>
    /// Coverage ID
    /// </summary>
    public string? CoverageId { get; set; }

    /// <summary>
    /// Pre-authorization references
    /// </summary>
    public string? PreAuthReferences { get; set; }
}

/// <summary>
/// DTO for creating a ClaimResponseInsurance
/// </summary>
public class CreateClaimResponseInsuranceDto
{
    /// <summary>
    /// Claim Response ID
    /// </summary>
    public string ClaimResponseId { get; set; } = string.Empty;

    /// <summary>
    /// Sequence number
 /// </summary>
 public int Sequence { get; set; }

    /// <summary>
    /// Coverage ID
    /// </summary>
    public string? CoverageId { get; set; }

    /// <summary>
    /// Pre-authorization references
    /// </summary>
    public string? PreAuthReferences { get; set; }
}

/// <summary>
/// DTO for updating a ClaimResponseInsurance
/// </summary>
public class UpdateClaimResponseInsuranceDto
{
    /// <summary>
    /// Coverage ID
    /// </summary>
    public string? CoverageId { get; set; }

  /// <summary>
    /// Pre-authorization references
  /// </summary>
    public string? PreAuthReferences { get; set; }
}
