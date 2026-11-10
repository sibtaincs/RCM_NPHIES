namespace NPhies_FHIR_Integration.Application.DTOs;

/// <summary>
/// DTO for PreAuthorizationRequest - for read operations
/// </summary>
public class PreAuthorizationRequestDto
{
    public string Id { get; set; } = string.Empty;
    public string RequestIdentifier { get; set; } = string.Empty;
    public string? RequestIdentifierSystem { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string? Priority { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string ProviderId { get; set; } = string.Empty;
  public string? ProviderName { get; set; }
    public string InsurerId { get; set; } = string.Empty;
    public string? InsurerName { get; set; }
    public string? CoverageId { get; set; }
    public string? PolicyNumber { get; set; }
    public DateTime? ServicedPeriodStart { get; set; }
    public DateTime? ServicedPeriodEnd { get; set; }
    public DateTime? ServicedDate { get; set; }
    public DateTime EnteredDate { get; set; }
    public string? Purpose { get; set; }
    public string? MessageHeaderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsActive { get; set; }

// Related data
    public List<PreAuthorizationItemDto> Items { get; set; } = new();
    public List<PreAuthorizationDiagnosisDto> Diagnoses { get; set; } = new();
    public PreAuthorizationResponseDto? Response { get; set; }

    // Computed properties
    public string ServiceDateDisplay => GetServiceDateDisplay();
  public bool HasResponse => Response != null;
    public bool IsPending => Response == null && Status == "active";

    private string GetServiceDateDisplay()
    {
  if (ServicedDate.HasValue)
   return ServicedDate.Value.ToString("yyyy-MM-dd");

if (ServicedPeriodStart.HasValue && ServicedPeriodEnd.HasValue)
      return $"{ServicedPeriodStart.Value:yyyy-MM-dd} to {ServicedPeriodEnd.Value:yyyy-MM-dd}";

        return "Not specified";
    }
}

/// <summary>
/// DTO for creating a PreAuthorizationRequest
/// </summary>
public class CreatePreAuthorizationRequestDto
{
    public string RequestIdentifier { get; set; } = string.Empty;
    public string? RequestIdentifierSystem { get; set; }
    public string Type { get; set; } = "professional"; // oral, pharmacy, institutional, professional, vision
    public string? Priority { get; set; } = "normal"; // stat, normal, deferred
    public string PatientId { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string InsurerId { get; set; } = string.Empty;
    public string? CoverageId { get; set; }
    public DateTime? ServicedPeriodStart { get; set; }
    public DateTime? ServicedPeriodEnd { get; set; }
    public DateTime? ServicedDate { get; set; }
    public string? Purpose { get; set; } = "preauthorization"; // preauthorization, predetermination

    // Items to be pre-authorized
    public List<CreatePreAuthorizationItemDto> Items { get; set; } = new();

    // Diagnoses
    public List<CreatePreAuthorizationDiagnosisDto> Diagnoses { get; set; } = new();

    // Supporting information
    public List<CreatePreAuthorizationSupportingInfoDto> SupportingInfo { get; set; } = new();
}

/// <summary>
/// DTO for updating a PreAuthorizationRequest
/// </summary>
public class UpdatePreAuthorizationRequestDto
{
    public string? Status { get; set; }
    public string? Priority { get; set; }
    public DateTime? ServicedPeriodStart { get; set; }
    public DateTime? ServicedPeriodEnd { get; set; }
    public DateTime? ServicedDate { get; set; }
}

/// <summary>
/// DTO for PreAuthorizationItem
/// </summary>
public class PreAuthorizationItemDto
{
    public string Id { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public int? CareTeamSequence { get; set; }
    public string? DiagnosisSequence { get; set; }
    public string? InformationSequence { get; set; }
    public string ProductOrServiceCode { get; set; } = string.Empty;
    public string? ProductOrServiceSystem { get; set; }
    public string? ProductOrServiceDescription { get; set; }
    public DateTime? ServicedDate { get; set; }
  public DateTime? ServicedPeriodStart { get; set; }
 public DateTime? ServicedPeriodEnd { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? NetAmount { get; set; }
  public string? BodySiteCode { get; set; }
    public string? BodySiteSystem { get; set; }
    public string? SubSiteCode { get; set; }
    public string? CurrencyCode { get; set; }
    public string? LocationId { get; set; }
    public string? PractitionerId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for creating a PreAuthorizationItem
/// </summary>
public class CreatePreAuthorizationItemDto
{
    public int Sequence { get; set; }
    public int? CareTeamSequence { get; set; }
  public string? DiagnosisSequence { get; set; }
 public string? InformationSequence { get; set; }
    public string ProductOrServiceCode { get; set; } = string.Empty;
    public string? ProductOrServiceSystem { get; set; }
    public string? ProductOrServiceDescription { get; set; }
    public DateTime? ServicedDate { get; set; }
    public DateTime? ServicedPeriodStart { get; set; }
    public DateTime? ServicedPeriodEnd { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public string? BodySiteCode { get; set; }
    public string? BodySiteSystem { get; set; }
    public string? SubSiteCode { get; set; }
    public string? CurrencyCode { get; set; } = "SAR";
    public string? LocationId { get; set; }
    public string? PractitionerId { get; set; }
    public string? Notes { get; set; }

    // Computed property
    public decimal NetAmount => (Quantity ?? 0) * (UnitPrice ?? 0);
}

/// <summary>
/// DTO for PreAuthorizationDiagnosis
/// </summary>
public class PreAuthorizationDiagnosisDto
{
    public string Id { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string DiagnosisCode { get; set; } = string.Empty;
    public string? DiagnosisSystem { get; set; }
    public string? DiagnosisType { get; set; }
    public string? OnAdmission { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for creating a PreAuthorizationDiagnosis
/// </summary>
public class CreatePreAuthorizationDiagnosisDto
{
    public int Sequence { get; set; }
    public string DiagnosisCode { get; set; } = string.Empty;
    public string? DiagnosisSystem { get; set; } = "http://hl7.org/fhir/sid/icd-10";
    public string? DiagnosisType { get; set; } = "admitting"; // admitting, principal, discharge
 public string? OnAdmission { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for PreAuthorizationSupportingInfo
/// </summary>
public class PreAuthorizationSupportingInfoDto
{
    public string Id { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? CategorySystem { get; set; }
    public string? CodeValue { get; set; }
    public string? StringValue { get; set; }
    public DateTime? DateValue { get; set; }
    public decimal? QuantityValue { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for creating PreAuthorizationSupportingInfo
/// </summary>
public class CreatePreAuthorizationSupportingInfoDto
{
    public int Sequence { get; set; }
    public string Category { get; set; } = string.Empty;
    public string? CategorySystem { get; set; }
    public string? CodeValue { get; set; }
    public string? StringValue { get; set; }
    public DateTime? DateValue { get; set; }
    public decimal? QuantityValue { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for PreAuthorizationResponse
/// </summary>
public class PreAuthorizationResponseDto
{
 public string Id { get; set; } = string.Empty;
    public string? ResponseIdentifier { get; set; }
 public string? ResponseIdentifierSystem { get; set; }
    public string Status { get; set; } = string.Empty;
 public string Outcome { get; set; } = string.Empty;
    public string? Disposition { get; set; }
  public string? PreAuthRef { get; set; }
    public DateTime? ValidityPeriodStart { get; set; }
    public DateTime? ValidityPeriodEnd { get; set; }
    public string? PatientId { get; set; }
    public string? InsurerId { get; set; }
    public string? RequestorId { get; set; }
 public DateTime ResponseDate { get; set; }
    public List<PreAuthorizationResponseItemDto> ResponseItems { get; set; } = new();
    public List<PreAuthorizationResponseErrorDto> Errors { get; set; } = new();

    // Computed properties
    public bool IsApproved => Outcome == "complete" && !string.IsNullOrEmpty(PreAuthRef);
    public bool IsValid => CheckValidity();

    private bool CheckValidity()
    {
      if (!ValidityPeriodStart.HasValue || !ValidityPeriodEnd.HasValue)
  return false;

  var now = DateTime.UtcNow;
     return now >= ValidityPeriodStart.Value && now <= ValidityPeriodEnd.Value;
    }
}

/// <summary>
/// DTO for PreAuthorizationResponseItem
/// </summary>
public class PreAuthorizationResponseItemDto
{
    public string Id { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public int RequestSequence { get; set; }
 public string? Decision { get; set; }
    public string? AdjudicationResult { get; set; }
    public decimal? ApprovedQuantity { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public decimal? BenefitAmount { get; set; }
    public string? Currency { get; set; }
    public string? DenialReason { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for PreAuthorizationResponseError
/// </summary>
public class PreAuthorizationResponseErrorDto
{
 public string Id { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string? ErrorCodeSystem { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public string Severity { get; set; } = "error";
    public string? ErrorLocation { get; set; }
}

/// <summary>
/// DTO for Pre-Authorization summary/list view
/// </summary>
public class PreAuthorizationSummaryDto
{
    public string Id { get; set; } = string.Empty;
    public string RequestIdentifier { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string ProviderId { get; set; } = string.Empty;
    public string? ProviderName { get; set; }
    public string InsurerId { get; set; } = string.Empty;
    public string? InsurerName { get; set; }
    public DateTime EnteredDate { get; set; }
    public int ItemCount { get; set; }
    public bool HasResponse { get; set; }
    public string? PreAuthRef { get; set; }
 public string? ResponseOutcome { get; set; }
}

/// <summary>
/// DTO for pre-authorization statistics
/// </summary>
public class PreAuthStatisticsDto
{
    public int TotalRequests { get; set; }
    public int ActiveRequests { get; set; }
    public int CompletedRequests { get; set; }
    public int CancelledRequests { get; set; }
    public int PendingRequests { get; set; }
 public int ApprovedRequests { get; set; }
    public int DeniedRequests { get; set; }
    public decimal AverageProcessingTimeHours { get; set; }
    public string AverageProcessingTimeDisplay => $"{AverageProcessingTimeHours:F2} hours";
}
