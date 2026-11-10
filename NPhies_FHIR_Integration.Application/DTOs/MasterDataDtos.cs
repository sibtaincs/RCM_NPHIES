namespace NPhies_FHIR_Integration.Application.DTOs;

/// <summary>
/// Master Data DTOs for API requests/responses
/// </summary>

#region Service Code Master DTOs
public class ServiceCodeMasterDto
{
    public string? Id { get; set; }
    public required string ServiceCode { get; set; }
    public required string ServiceName { get; set; }
 public string? ServiceDescription { get; set; }
    public required string ServiceCategory { get; set; }
    public string? NphiesServiceCode { get; set; }
    public string? NphiesServiceName { get; set; }
    public string? NphiesCategoryCode { get; set; }
    public bool IsNphiesMapped { get; set; }
    public string? MappingValidationStatus { get; set; }
    public decimal DefaultPrice { get; set; }
    public string CurrencyCode { get; set; } = "SAR";
    public bool IsRequiresAuthorization { get; set; }
    public int? DefaultAuthorizationDays { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

#endregion

#region Medication Code Master DTOs
public class MedicationCodeMasterDto
{
    public string? Id { get; set; }
    public required string MedicationCode { get; set; }
    public required string MedicationName { get; set; }
    public string? ActiveIngredient { get; set; }
    public string? Strength { get; set; }
    public string? Unit { get; set; }
  public string? Form { get; set; }
    public string? Manufacturer { get; set; }
    public string? NphiesMedicationCode { get; set; }
    public bool IsNphiesMapped { get; set; }
    public decimal UnitPrice { get; set; }
    public string CurrencyCode { get; set; } = "SAR";
    public bool IsControlledSubstance { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

#endregion

#region Medical Device Code Master DTOs
public class MedicalDeviceCodeMasterDto
{
    public string? Id { get; set; }
    public required string DeviceCode { get; set; }
    public required string DeviceName { get; set; }
    public string? DeviceDescription { get; set; }
    public string? DeviceType { get; set; }
    public string? Manufacturer { get; set; }
    public string? NphiesDeviceCode { get; set; }
    public bool IsNphiesMapped { get; set; }
    public decimal UnitPrice { get; set; }
    public string CurrencyCode { get; set; } = "SAR";
    public bool IsImplantable { get; set; }
    public bool IsReusable { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

#endregion

#region Diagnosis Code Master DTOs
public class DiagnosisCodeMasterDto
{
    public string? Id { get; set; }
    public required string DiagnosisCode { get; set; }
    public required string DiagnosisName { get; set; }
    public string? DiagnosisCategory { get; set; }
    public string? DiagnosisType { get; set; }
    public bool IsOnAdmission { get; set; }
    public string? NphiesDiagnosisCode { get; set; }
    public bool IsNphiesMapped { get; set; }
    public string? Severity { get; set; }
    public bool RequiresDocumentation { get; set; }
    public bool IsActive { get; set; } = true;
  public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

#endregion

#region Modifier Code Master DTOs
public class ModifierCodeMasterDto
{
    public string? Id { get; set; }
    public required string ModifierCode { get; set; }
    public required string ModifierName { get; set; }
    public string? ModifierDescription { get; set; }
    public string? ModifierType { get; set; }
    public string? ImpactOnCharges { get; set; }
  public decimal? ChargePercentage { get; set; }
    public string? NphiesModifierCode { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

#endregion

#region Benefit Code Master DTOs
public class BenefitCodeMasterDto
{
    public string? Id { get; set; }
    public required string BenefitCode { get; set; }
    public required string BenefitName { get; set; }
    public string? BenefitCategory { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

#endregion

#region NPHIES Code Mapping DTOs
public class NphiesCodeMappingDto
{
    public string? Id { get; set; }
    public required string LocalCode { get; set; }
    public string? LocalCodeSystem { get; set; }
    public string? LocalDescription { get; set; }
    public required string NphiesCode { get; set; }
    public required string NphiesCodeSystem { get; set; }
    public string? NphiesDescription { get; set; }
    public required string CodeType { get; set; }
  public bool IsMappingValid { get; set; } = true;
    public DateTime? MappingValidationDate { get; set; }
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
  public string? ModifiedBy { get; set; }
}

#endregion

#region Payer Master DTOs
public class PayerMasterDto
{
    public string? Id { get; set; }
    public required string PayerId { get; set; }
    public required string PayerName { get; set; }
    public string? PayerType { get; set; }
    public string? NphiesConnectionStatus { get; set; }
    public string? NphiesApiEndpoint { get; set; }
    public bool IsNphiesMember { get; set; }
    public string? SupportedClaimTypes { get; set; }
    public string? SupportedEligibilityTypes { get; set; }
    public int MaxClaimsPerDay { get; set; }
    public decimal? MaxClaimAmount { get; set; }
    public string CurrencyCode { get; set; } = "SAR";
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

#endregion

#region Payer Policy Master DTOs
public class PayerPolicyMasterDto
{
    public string? Id { get; set; }
    public required string PayerMasterId { get; set; }
    public required string PolicyCode { get; set; }
    public required string PolicyName { get; set; }
    public string? PolicyType { get; set; }
    public string? CoverageType { get; set; }
    public decimal AnnualPremium { get; set; }
    public string CurrencyCode { get; set; } = "SAR";
    public decimal AnnualDeductible { get; set; }
public decimal? MaxOutOfPocket { get; set; }
    public decimal? Copay { get; set; }
    public decimal? CoinsurancePercentage { get; set; }
    public decimal? CoverageLimitPerVisit { get; set; }
 public decimal? CoverageLimitPerYear { get; set; }
    public decimal? PreAuthRequiredForAmount { get; set; }
    public DateTime EffectiveFromDate { get; set; }
    public DateTime? EffectiveToDate { get; set; }
    public bool IsPolicyActive { get; set; } = true;
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

#endregion

#region Policy Benefit Coverage DTOs
public class PolicyBenefitCoverageDto
{
    public string? Id { get; set; }
    public required string PolicyMasterId { get; set; }
    public string? ServiceCodeMasterId { get; set; }
    public string? ServiceCategory { get; set; }
    public string? BenefitType { get; set; }
public decimal CoveragePercentage { get; set; } = 100;
    public decimal? MaxCoverageAmount { get; set; }
    public bool RequiresPreAuth { get; set; }
    public bool RequiresReferral { get; set; }
    public int? PreAuthValidityDays { get; set; }
    public int? CoverageLimitPerYear { get; set; }
    public int? CoverageLimitPerLifetime { get; set; }
    public bool IsExcluded { get; set; }
    public string? ExclusionReason { get; set; }
    public bool IsWaitingPeriodApplicable { get; set; }
public int? WaitingPeriodDays { get; set; }
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
 public string? ModifiedBy { get; set; }
}

#endregion

#region Clinic Master DTOs
public class ClinicMasterDto
{
 public string? Id { get; set; }
    public required string OrganizationId { get; set; }
    public required string ClinicName { get; set; }
    public required string ClinicCode { get; set; }
    public string? ClinicType { get; set; }
    public string? SpecializedServices { get; set; }
    public int? NumberOfBeds { get; set; }
  public int? NumberOfDoctors { get; set; }
    public int? NumberOfNurses { get; set; }
    public string? IsCertifiedBy { get; set; }
    public string? AccreditationLevel { get; set; }
    public TimeOnly? WorkingHoursFrom { get; set; }
    public TimeOnly? WorkingHoursTo { get; set; }
    public bool IsEmergencyAvailable { get; set; }
    public bool PharmacyAvailable { get; set; }
    public bool LabAvailable { get; set; }
    public bool ImagingAvailable { get; set; }
    public bool AcceptsCashPayment { get; set; }
    public bool AcceptsInsurance { get; set; }
    public bool AcceptsCardPayment { get; set; }
    public int? AvailableBeds { get; set; }
public bool IsActive { get; set; } = true;
 public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

#endregion

#region Doctor Master DTOs
public class DoctorMasterDto
{
    public string? Id { get; set; }
  public required string PractitionerId { get; set; }
    public required string DoctorCode { get; set; }
    public required string DoctorName { get; set; }
    public string? Specialization { get; set; }
    public string? SubSpecialization { get; set; }
    public string? QualificationDegree { get; set; }
    public string? UniversityName { get; set; }
    public int? YearsOfExperience { get; set; }
    public bool IsConsultant { get; set; }
    public decimal? ConsultationFee { get; set; }
    public decimal? FollowupFee { get; set; }
    public string CurrencyCode { get; set; } = "SAR";
    public string? ClinicMasterId { get; set; }
    public bool IsAvailableForAppointments { get; set; }
    public int? AvailableSlotsPerDay { get; set; }
    public string? Board { get; set; }
    public string? BoardLicenseNumber { get; set; }
    public DateTime? BoardLicenseExpiry { get; set; }
    public int? ResearchPapers { get; set; }
    public bool IsTeachingMember { get; set; }
    public bool IsActive { get; set; } = true;
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

#endregion

#region Doctor Qualification DTOs
public class DoctorQualificationDto
{
    public string? Id { get; set; }
    public required string DoctorMasterId { get; set; }
    public required string QualificationType { get; set; }
    public required string QualificationName { get; set; }
    public required string UniversityName { get; set; }
    public required DateTime IssuedDate { get; set; }
    public bool IsExpiring { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? CertificateNumber { get; set; }
    public string? VerificationStatus { get; set; } = "Pending";
    public string? CreatedBy { get; set; }
}

#endregion

#region Claim Submission Rules DTOs
public class ClaimSubmissionRulesDto
{
    public string? Id { get; set; }
    public string? PayerMasterId { get; set; }
    public string? PolicyMasterId { get; set; }
    public required string RuleName { get; set; }
    public string? RuleType { get; set; }
    public string? RuleCondition { get; set; }
    public decimal? MaxClaimAmount { get; set; }
    public int? MaxItemsPerClaim { get; set; }
    public bool RequiresInvoice { get; set; }
    public bool RequiresMedicalReport { get; set; }
 public bool RequiresPhotos { get; set; }
 public int? MaxDaysForSubmission { get; set; }
    public bool IsActive { get; set; } = true;
public int Priority { get; set; } = 100;
    public string? CreatedBy { get; set; }
    public string? ModifiedBy { get; set; }
}

#endregion
