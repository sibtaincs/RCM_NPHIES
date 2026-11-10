namespace NPhies_FHIR_Integration.Domain.DTOs;

/// <summary>
/// DTO for Patient entity
/// </summary>
public class PatientDto : BaseDto
{
    public string MRN { get; set; } = string.Empty;
    public string NationalId { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
 public List<CoverageDto> Coverages { get; set; } = new();
}

/// <summary>
/// DTO for Coverage entity
/// </summary>
public class CoverageDto : BaseDto
{
    public string PolicyNumber { get; set; } = string.Empty;
    public string MemberID { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string InsurerId { get; set; } = string.Empty;
    public DateTime CoverageStartDate { get; set; }
    public DateTime CoverageEndDate { get; set; }
    public string CoverageType { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public string SubscriberMRN { get; set; } = string.Empty;
    public string RelationToSubscriber { get; set; } = "Self";
    public decimal AnnualDeductible { get; set; }
    public decimal DeductibleMet { get; set; }
    public decimal Copay { get; set; }
    public decimal CoinsurancePercent { get; set; }
    public decimal OutOfPocketMax { get; set; }
}

/// <summary>
/// DTO for Organization entity
/// </summary>
public class OrganizationDto : BaseDto
{
    public string OrganizationName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string LicenseSystem { get; set; } = string.Empty;
    public string OrganizationType { get; set; } = string.Empty;
    public string SpecializationType { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
 public string PhoneNumber { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
}

/// <summary>
/// DTO for Location entity
/// </summary>
public class LocationDto : BaseDto
{
    public string LocationName { get; set; } = string.Empty;
public string LocationLicense { get; set; } = string.Empty;
  public string LicenseSystem { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
  public string FacilityType { get; set; } = string.Empty;
    public string FacilityTypeDescription { get; set; } = string.Empty;
    public string AddressLine1 { get; set; } = string.Empty;
    public string AddressLine2 { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = "SA";
public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
}

/// <summary>
/// DTO for Practitioner entity
/// </summary>
public class PractitionerDto : BaseDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string LicenseNumber { get; set; } = string.Empty;
    public string LicenseSystem { get; set; } = string.Empty;
    public string Specialization { get; set; } = string.Empty;
  public string Qualification { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string OrganizationId { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
}

/// <summary>
/// DTO for MessageHeader entity
/// </summary>
public class MessageHeaderDto : BaseDto
{
    public string MessageUUID { get; set; } = string.Empty;
    public string CorrelationId { get; set; } = string.Empty;
    public string EventCode { get; set; } = string.Empty;
    public string SenderOrganizationId { get; set; } = string.Empty;
    public string DestinationName { get; set; } = string.Empty;
    public string DestinationEndpoint { get; set; } = string.Empty;
    public string FocusResourceType { get; set; } = string.Empty;
    public string FocusResourceId { get; set; } = string.Empty;
    public string SourceName { get; set; } = string.Empty;
    public string SourceEndpoint { get; set; } = string.Empty;
    public DateTime MessageTimestamp { get; set; }
    public DateTime? ResponseTimestamp { get; set; }
    public string Status { get; set; } = "sent";
    public string ResponseStatus { get; set; } = string.Empty;
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
}

/// <summary>
/// DTO for EligibilityItem entity
/// </summary>
public class EligibilityItemDto : BaseDto
{
    public string EligibilityRequestId { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public string Category { get; set; } = string.Empty;
    public string CategoryDescription { get; set; } = string.Empty;
    public string ProductOrServiceCode { get; set; } = string.Empty;
    public string ProductOrServiceDescription { get; set; } = string.Empty;
    public List<EligibilityItemModifierDto> Modifiers { get; set; } = new();
    public string DiagnosisCodes { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

/// <summary>
/// DTO for EligibilityItemModifier entity
/// </summary>
public class EligibilityItemModifierDto : BaseDto
{
    public string EligibilityItemId { get; set; } = string.Empty;
    public string ModifierCode { get; set; } = string.Empty;
    public string ModifierDescription { get; set; } = string.Empty;
}

/// <summary>
/// DTO for CoverageEligibilityRequest entity
/// </summary>
public class CoverageEligibilityRequestDto : BaseDto
{
    public string MessageUUID { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string MessageHeaderId { get; set; } = string.Empty;
    public string RequestType { get; set; } = "validation";
    public List<string> Purpose { get; set; } = new() { "benefits" };
    public string Status { get; set; } = "active";
    public string Priority { get; set; } = "normal";
    public DateTime ServiceDate { get; set; }
    public DateTime ServicedPeriodStart { get; set; }
    public DateTime ServicedPeriodEnd { get; set; }
    public string ServiceType { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string CoverageId { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
  public string InsurerId { get; set; } = string.Empty;
    public string EntererPractitionerId { get; set; } = string.Empty;
    public List<EligibilityItemDto> Items { get; set; } = new();
    public DateTime RequestCreatedAt { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
public string ResponseId { get; set; } = string.Empty;
    public string EligibilityStatus { get; set; } = string.Empty;
    public string MessageStatus { get; set; } = "sent";
}

/// <summary>
/// DTO for BenefitBalance entity
/// </summary>
public class BenefitBalanceDto : BaseDto
{
    public string EligibilityResponseId { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public string Category { get; set; } = string.Empty;
    public string CategoryDescription { get; set; } = string.Empty;
    public List<BenefitDto> Benefits { get; set; } = new();
}

/// <summary>
/// DTO for Benefit entity
/// </summary>
public class BenefitDto : BaseDto
{
    public string BenefitBalanceId { get; set; } = string.Empty;
    public int SequenceNumber { get; set; }
    public string BenefitType { get; set; } = string.Empty;
    public string BenefitTypeDescription { get; set; } = string.Empty;
    public decimal AllowedAmount { get; set; }
    public string AllowedCurrency { get; set; } = "SAR";
    public string AllowedUnit { get; set; } = string.Empty;
    public decimal UsedAmount { get; set; }
    public decimal? PercentageAmount { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime? BenefitStartDate { get; set; }
    public DateTime? BenefitEndDate { get; set; }
}

/// <summary>
/// DTO for EligibilityError entity
/// </summary>
public class EligibilityErrorDto : BaseDto
{
    public string? EligibilityRequestId { get; set; }
    public string? EligibilityResponseId { get; set; }
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public string ErrorDetails { get; set; } = string.Empty;
    public string Severity { get; set; } = "error";
    public string ErrorLocation { get; set; } = string.Empty;
    public string ErrorField { get; set; } = string.Empty;
    public int? HttpStatusCode { get; set; }
    public DateTime ErrorOccurredAt { get; set; }
  public string AdditionalContext { get; set; } = string.Empty;
}

/// <summary>
/// DTO for CoverageEligibilityResponse entity
/// </summary>
public class CoverageEligibilityResponseDto : BaseDto
{
    public string ResponseUUID { get; set; } = string.Empty;
public string RequestId { get; set; } = string.Empty;
    public string EligibilityRequestId { get; set; } = string.Empty;
    public string MessageHeaderId { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public string Outcome { get; set; } = "complete";
    public string ProcessingStatus { get; set; } = string.Empty;
    public DateTime ResponseCreatedAt { get; set; }
    public DateTime ResponseReceivedAt { get; set; }
    public string EligibilityStatus { get; set; } = string.Empty;
    public bool IsInForce { get; set; }
    public DateTime ServicedPeriodStart { get; set; }
    public DateTime ServicedPeriodEnd { get; set; }
    public string InsurerId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string CoverageId { get; set; } = string.Empty;
    public bool IsInNetwork { get; set; }
    public string NetworkStatus { get; set; } = "unknown";
 public string NetworkName { get; set; } = string.Empty;
    public List<string> CoveredServices { get; set; } = new();
    public List<string> ExcludedServices { get; set; } = new();
    public List<string> Limitations { get; set; } = new();
    public List<BenefitBalanceDto> BenefitBalances { get; set; } = new();
    public List<EligibilityErrorDto> Errors { get; set; } = new();
    public string ExplanationOfBenefits { get; set; } = string.Empty;
}
