namespace NPhies_FHIR_Integration.Application.DTOs;

/// <summary>
/// DTO for ClaimResponse entity - for read operations
/// </summary>
public class ClaimResponseDto
{
    public string Id { get; set; } = string.Empty;
    public string ClaimId { get; set; } = string.Empty;
    public string? ResponseIdentifierSystem { get; set; }
    public string? ResponseIdentifierValue { get; set; }
    public string? ClaimResponseStatus { get; set; }
    public string? ClaimType { get; set; }
    public string? ClaimSubType { get; set; }
    public string? Use { get; set; }
    public string? PreAuthRef { get; set; }
    public string? PatientId { get; set; }
    public string? InsurerId { get; set; }
    public string? RequestIdentifierValue { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    /// <summary>FHIR ClaimResponse.outcome: queued | complete | error | partial (falls back to the resource status).</summary>
    public string? Outcome { get; set; }
    /// <summary>NPHIES adjudication-outcome extension: approved | rejected | partial | pended.</summary>
    public string? AdjudicationOutcome { get; set; }
    public string? Disposition { get; set; }
    public string? ResponseMessageId { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public DateTime? PreAuthPeriodStart { get; set; }
    public DateTime? PreAuthPeriodEnd { get; set; }

    // Totals (SAR)
    public decimal? TotalSubmitted { get; set; }
    public decimal? TotalEligible { get; set; }
    public decimal? TotalBenefit { get; set; }
    public decimal? TotalCopay { get; set; }
    public decimal? TotalTax { get; set; }
    public decimal? TotalPatientShare { get; set; }
    public decimal? PaymentAmount { get; set; }
    public DateTime? PaymentDate { get; set; }

    /// <summary>Per-item adjudication.</summary>
    public List<ClaimResponseItemDto> Items { get; set; } = new();
    
    /// <summary>
    /// Alias for CreatedAt for compatibility
    /// </summary>
    public DateTime Created => CreatedAt;
}

/// <summary>Per-item adjudication of a ClaimResponse.</summary>
public class ClaimResponseItemDto
{
    public int ItemSequence { get; set; }
    /// <summary>approved | rejected | partial | pended</summary>
    public string? Outcome { get; set; }
    public decimal? Submitted { get; set; }
    public decimal? Eligible { get; set; }
    public decimal? Benefit { get; set; }
    public decimal? Copay { get; set; }
    public decimal? Deductible { get; set; }
    public decimal? Tax { get; set; }
    public decimal? PatientShare { get; set; }
    public decimal? ApprovedQuantity { get; set; }
    public string? ReasonCode { get; set; }
    public string? ReasonDisplay { get; set; }
}

/// <summary>
/// DTO for creating a ClaimResponse
/// </summary>
public class CreateClaimResponseDto
{
    public string ClaimId { get; set; } = string.Empty;
    public string? ResponseIdentifierSystem { get; set; }
    public string? ResponseIdentifierValue { get; set; }
    public string? ClaimResponseStatus { get; set; }
    public string? ClaimType { get; set; }
    public string? ClaimTypeSystem { get; set; }
    public string? ClaimSubType { get; set; }
    public string? Use { get; set; }
    public string? RequestIdentifierSystem { get; set; }
    public string? RequestIdentifierValue { get; set; }
    public string? PreAuthRef { get; set; }
    public string? PatientId { get; set; }
  public string? InsurerId { get; set; }
    public string? RequestorId { get; set; }
}

/// <summary>
/// DTO for updating a ClaimResponse
/// </summary>
public class UpdateClaimResponseDto
{
    public string? ClaimResponseStatus { get; set; }
    public string? PreAuthRef { get; set; }
}
