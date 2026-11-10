namespace NPhies_FHIR_Integration.Application.DTOs;

public sealed class ClaimWorkflowDetailsDto
{
    public string Id { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ClaimType { get; set; } = string.Empty;
    public string? ClaimSubType { get; set; }
    public string? Priority { get; set; }
    public decimal? Total { get; set; }
    public string? TotalCurrency { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string? PatientNameArabic { get; set; }
    public string? FamilyNameArabic { get; set; }
    public string? PatientMrn { get; set; }
    public string? PatientNationalId { get; set; }
    public string? PatientNationalIdType { get; set; }
    public DateTime? PatientDateOfBirth { get; set; }
    public string? PatientGender { get; set; }
    public int? PatientAgeYears { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? ClinicName { get; set; }
    public string? DoctorReference { get; set; }
    public string? DoctorName { get; set; }
    public string? DoctorLicenseNumber { get; set; }
    public string? CoveragePolicyNumber { get; set; }
    public string? CoverageMemberId { get; set; }
    public string? CoverageStatus { get; set; }
    public string? PayerName { get; set; }
    public string? PayerPolicyName { get; set; }
    public string? PolicyHolderNumber { get; set; }
    public string? PolicyHolderName { get; set; }
    public string? IdentificationNumber { get; set; }
    public string? NationalityId { get; set; }
    public string? CoveragePlanCode { get; set; }
    public string? CoverageType { get; set; }
    public DateTime? CoverageStartDate { get; set; }
    public DateTime? CoverageEndDate { get; set; }
    public string? PreAuthorizationReference { get; set; }
    public DateTime? OfflineApprovalDate { get; set; }
    public string? OfflineApprovalReference { get; set; }
    public string? EligibilityOfflineReference { get; set; }
    public string? NphiesApprovalIdentifier { get; set; }
    public string? ApprovalUrl { get; set; }
    public DateTime? EncounterInvoiceDate { get; set; }
    public bool? IsNewborn { get; set; }
    public string? EncounterId { get; set; }
    public string? EncounterIdentifierValue { get; set; }
    public string? EncounterClass { get; set; }
    public string? EncounterStatus { get; set; }
    public string? EncounterAdmitSource { get; set; }
    public DateTime? EncounterPeriodStart { get; set; }
    public DateTime? EncounterPeriodEnd { get; set; }
    public string? EmergencyArrivalCode { get; set; }
    public string? TriageCategory { get; set; }
    public string? DischargeDisposition { get; set; }
    public string? EmergencyDepartmentDisposition { get; set; }
    public DateTime? TriageDate { get; set; }
    public string? DischargeSpecialty { get; set; }
    public string? ChiefComplaint { get; set; }
    public string? SignificantSigns { get; set; }
    public string? InvestigationResult { get; set; }
    public string? TreatmentPlan { get; set; }
    public string? PatientHistory { get; set; }
    public string? PhysicalExamination { get; set; }
    public string? HistoryOfPresentIllness { get; set; }
    public List<ClaimWorkflowVitalDto> VitalSigns { get; set; } = [];
    public List<ClaimWorkflowServiceDto> Services { get; set; } = [];
    public List<ClaimWorkflowDiagnosisDto> Diagnoses { get; set; } = [];
}

public sealed class ClaimWorkflowVitalDto
{
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public string? Unit { get; set; }
}

public sealed class ClaimWorkflowServiceDto
{
    public int Sequence { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public decimal? Quantity { get; set; }
    public DateTime? ServiceDate { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? Net { get; set; }
    public string? ReferenceInvoice { get; set; }
    public string? ApprovalNumber { get; set; }
    public decimal? ApprovedAmount { get; set; }
    public int? ApprovedQuantity { get; set; }
    public bool? IsApproved { get; set; }
    public List<ClaimWorkflowServiceDetailDto> Details { get; set; } = [];
}

public sealed class ClaimWorkflowServiceDetailDto
{
    public int Sequence { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public string? ServiceName { get; set; }
    public decimal? Quantity { get; set; }
    public decimal? UnitPrice { get; set; }
    public decimal? Net { get; set; }
}

public sealed class ClaimWorkflowDiagnosisDto
{
    public int Sequence { get; set; }
    public string DiagnosisCode { get; set; } = string.Empty;
    public string? DiagnosisDisplay { get; set; }
    public string? DiagnosisType { get; set; }
}