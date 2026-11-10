namespace NPhies_FHIR_Integration.Application.Services;

/// <summary>
/// Generic interface for master data services
/// </summary>
/// <typeparam name="TEntity">The entity type</typeparam>
/// <typeparam name="TDto">The DTO type</typeparam>
public interface IMasterDataService<TEntity, TDto> where TEntity : class where TDto : class
{
    /// <summary>
    /// Get all records with pagination and filtering
    /// </summary>
    Task<(List<TDto> items, int total)> GetAllAsync(int skip = 0, int take = 10, string? searchTerm = null);

    /// <summary>
    /// Get a single record by ID
    /// </summary>
    Task<TDto?> GetByIdAsync(string id);

    /// <summary>
    /// Create a new record
    /// </summary>
    Task<TDto> CreateAsync(TDto dto, string? createdBy = null);

    /// <summary>
    /// Update an existing record
    /// </summary>
    Task<TDto> UpdateAsync(string id, TDto dto, string? modifiedBy = null);

    /// <summary>
    /// Delete a record
    /// </summary>
    Task DeleteAsync(string id);

    /// <summary>
    /// Check if a record exists
    /// </summary>
Task<bool> ExistsAsync(string id);

    /// <summary>
    /// Search records by criteria
    /// </summary>
    Task<List<TDto>> SearchAsync(string criteria);

    /// <summary>
    /// Bulk create records
    /// </summary>
    Task<List<TDto>> BulkCreateAsync(List<TDto> dtos, string? createdBy = null);

    /// <summary>
    /// Activate or deactivate a record
    /// </summary>
    Task<TDto> ToggleActiveAsync(string id, bool isActive);
}

/// <summary>
/// Service code master interface
/// </summary>
public interface IServiceCodeMasterService : IMasterDataService<Domain.Entities.ServiceCodeMaster, Application.DTOs.ServiceCodeMasterDto>
{
    Task<List<Application.DTOs.ServiceCodeMasterDto>> GetByServiceCategoryAsync(string category);
    Task<List<Application.DTOs.ServiceCodeMasterDto>> GetNphiesMappedAsync();
    Task<Application.DTOs.ServiceCodeMasterDto?> GetByServiceCodeAsync(string serviceCode);
}

/// <summary>
/// Medication code master interface
/// </summary>
public interface IMedicationCodeMasterService : IMasterDataService<Domain.Entities.MedicationCodeMaster, Application.DTOs.MedicationCodeMasterDto>
{
    Task<List<Application.DTOs.MedicationCodeMasterDto>> GetByFormAsync(string form);
    Task<List<Application.DTOs.MedicationCodeMasterDto>> GetControlledSubstancesAsync();
    Task<Application.DTOs.MedicationCodeMasterDto?> GetByMedicationCodeAsync(string medicationCode);
}

/// <summary>
/// Medical device code master interface
/// </summary>
public interface IMedicalDeviceCodeMasterService : IMasterDataService<Domain.Entities.MedicalDeviceCodeMaster, Application.DTOs.MedicalDeviceCodeMasterDto>
{
    Task<List<Application.DTOs.MedicalDeviceCodeMasterDto>> GetByDeviceTypeAsync(string deviceType);
    Task<List<Application.DTOs.MedicalDeviceCodeMasterDto>> GetImplantableDevicesAsync();
    Task<List<Application.DTOs.MedicalDeviceCodeMasterDto>> GetReusableDevicesAsync();
}

/// <summary>
/// Diagnosis code master interface
/// </summary>
public interface IDiagnosisCodeMasterService : IMasterDataService<Domain.Entities.DiagnosisCodeMaster, Application.DTOs.DiagnosisCodeMasterDto>
{
    Task<List<Application.DTOs.DiagnosisCodeMasterDto>> GetByCategoryAsync(string category);
 Task<List<Application.DTOs.DiagnosisCodeMasterDto>> GetOnAdmissionDiagnosesAsync();
    Task<Application.DTOs.DiagnosisCodeMasterDto?> GetByDiagnosisCodeAsync(string diagnosisCode);
}

/// <summary>
/// Modifier code master interface
/// </summary>
public interface IModifierCodeMasterService : IMasterDataService<Domain.Entities.ModifierCodeMaster, Application.DTOs.ModifierCodeMasterDto>
{
    Task<List<Application.DTOs.ModifierCodeMasterDto>> GetByModifierTypeAsync(string modifierType);
Task<Application.DTOs.ModifierCodeMasterDto?> GetByModifierCodeAsync(string modifierCode);
}

/// <summary>
/// Benefit code master interface
/// </summary>
public interface IBenefitCodeMasterService : IMasterDataService<Domain.Entities.BenefitCodeMaster, Application.DTOs.BenefitCodeMasterDto>
{
    Task<List<Application.DTOs.BenefitCodeMasterDto>> GetByCategoryAsync(string category);
    Task<Application.DTOs.BenefitCodeMasterDto?> GetByBenefitCodeAsync(string benefitCode);
}

/// <summary>
/// NPHIES code mapping interface
/// </summary>
public interface INphiesCodeMappingService : IMasterDataService<Domain.Entities.NphiesCodeMapping, Application.DTOs.NphiesCodeMappingDto>
{
    Task<List<Application.DTOs.NphiesCodeMappingDto>> GetByCodeTypeAsync(string codeType);
    Task<Application.DTOs.NphiesCodeMappingDto?> GetByLocalCodeAsync(string localCode);
    Task<Application.DTOs.NphiesCodeMappingDto?> GetByNphiesCodeAsync(string nphiesCode);
    Task<List<Application.DTOs.NphiesCodeMappingDto>> GetValidMappingsAsync();
}

/// <summary>
/// Payer master interface
/// </summary>
public interface IPayerMasterService : IMasterDataService<Domain.Entities.PayerMaster, Application.DTOs.PayerMasterDto>
{
    Task<Application.DTOs.PayerMasterDto?> GetByPayerIdAsync(string payerId);
    Task<List<Application.DTOs.PayerMasterDto>> GetNphiesMembersAsync();
    Task<List<Application.DTOs.PayerMasterDto>> GetByPayerTypeAsync(string payerType);
}

/// <summary>
/// Payer policy master interface
/// </summary>
public interface IPayerPolicyMasterService : IMasterDataService<Domain.Entities.PayerPolicyMaster, Application.DTOs.PayerPolicyMasterDto>
{
  Task<List<Application.DTOs.PayerPolicyMasterDto>> GetByPayerAsync(string payerMasterId);
    Task<Application.DTOs.PayerPolicyMasterDto?> GetByPolicyCodeAsync(string policyCode);
    Task<List<Application.DTOs.PayerPolicyMasterDto>> GetActivePoliciesAsync();
}

/// <summary>
/// Policy benefit coverage interface
/// </summary>
public interface IPolicyBenefitCoverageService : IMasterDataService<Domain.Entities.PolicyBenefitCoverage, Application.DTOs.PolicyBenefitCoverageDto>
{
    Task<List<Application.DTOs.PolicyBenefitCoverageDto>> GetBypolicyAsync(string policyMasterId);
    Task<List<Application.DTOs.PolicyBenefitCoverageDto>> GetByCategoryAsync(string serviceCategory);
    Task<List<Application.DTOs.PolicyBenefitCoverageDto>> GetExcludedBenefitsAsync();
}

/// <summary>
/// Clinic master interface
/// </summary>
public interface IClinicMasterService : IMasterDataService<Domain.Entities.ClinicMaster, Application.DTOs.ClinicMasterDto>
{
    Task<Application.DTOs.ClinicMasterDto?> GetByClinicCodeAsync(string clinicCode);
    Task<List<Application.DTOs.ClinicMasterDto>> GetByOrganizationAsync(string organizationId);
    Task<List<Application.DTOs.ClinicMasterDto>> GetByTypeAsync(string clinicType);
}

/// <summary>
/// Doctor master interface
/// </summary>
public interface IDoctorMasterService : IMasterDataService<Domain.Entities.DoctorMaster, Application.DTOs.DoctorMasterDto>
{
    Task<Application.DTOs.DoctorMasterDto?> GetByDoctorCodeAsync(string doctorCode);
    Task<List<Application.DTOs.DoctorMasterDto>> GetBySpecializationAsync(string specialization);
    Task<List<Application.DTOs.DoctorMasterDto>> GetByClinicAsync(string clinicMasterId);
    Task<List<Application.DTOs.DoctorMasterDto>> GetAvailableForAppointmentsAsync();
}

/// <summary>
/// Doctor qualification interface
/// </summary>
public interface IDoctorQualificationService : IMasterDataService<Domain.Entities.DoctorQualification, Application.DTOs.DoctorQualificationDto>
{
    Task<List<Application.DTOs.DoctorQualificationDto>> GetByDoctorAsync(string doctorMasterId);
    Task<List<Application.DTOs.DoctorQualificationDto>> GetByTypeAsync(string qualificationType);
    Task<List<Application.DTOs.DoctorQualificationDto>> GetVerifiedQualificationsAsync();
}

/// <summary>
/// Claim submission rules interface
/// </summary>
public interface IClaimSubmissionRulesService : IMasterDataService<Domain.Entities.ClaimSubmissionRules, Application.DTOs.ClaimSubmissionRulesDto>
{
Task<List<Application.DTOs.ClaimSubmissionRulesDto>> GetByPayerAsync(string payerMasterId);
    Task<List<Application.DTOs.ClaimSubmissionRulesDto>> GetByPolicyAsync(string policyMasterId);
    Task<List<Application.DTOs.ClaimSubmissionRulesDto>> GetApplicableRulesAsync(string payerMasterId, string? policyMasterId = null);
}
