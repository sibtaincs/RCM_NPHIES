using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace NPhies_FHIR_Integration.Application.Mapping;

/// <summary>
/// Maps one master-data entity type to and from its DTO. <see cref="MasterDataServiceBase{TEntity,TDto}"/>
/// works through this interface so the generic CRUD code needs no runtime type lookup.
/// </summary>
public interface IMasterDataMapper<TEntity, TDto>
    where TEntity : class
    where TDto : class
{
    TDto ToDto(TEntity entity);
    TEntity ToEntity(TDto dto);

    /// <summary>Copies every DTO member onto the tracked entity, including nulls.</summary>
    void Update(TDto dto, TEntity entity);
}

/// <summary>Compile-time (Mapperly) mappings for all master-data entity and DTO pairs.</summary>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public sealed partial class MasterDataMapper :
    IMasterDataMapper<ServiceCodeMaster, ServiceCodeMasterDto>,
    IMasterDataMapper<MedicationCodeMaster, MedicationCodeMasterDto>,
    IMasterDataMapper<MedicalDeviceCodeMaster, MedicalDeviceCodeMasterDto>,
    IMasterDataMapper<DiagnosisCodeMaster, DiagnosisCodeMasterDto>,
    IMasterDataMapper<ModifierCodeMaster, ModifierCodeMasterDto>,
    IMasterDataMapper<BenefitCodeMaster, BenefitCodeMasterDto>,
    IMasterDataMapper<NphiesCodeMapping, NphiesCodeMappingDto>,
    IMasterDataMapper<PayerMaster, PayerMasterDto>,
    IMasterDataMapper<PayerPolicyMaster, PayerPolicyMasterDto>,
    IMasterDataMapper<PolicyBenefitCoverage, PolicyBenefitCoverageDto>,
    IMasterDataMapper<ClaimSubmissionRules, ClaimSubmissionRulesDto>,
    IMasterDataMapper<ClinicMaster, ClinicMasterDto>,
    IMasterDataMapper<DoctorMaster, DoctorMasterDto>,
    IMasterDataMapper<DoctorQualification, DoctorQualificationDto>
{
    /// <summary>The mapper is stateless, so one instance serves every service.</summary>
    public static MasterDataMapper Instance { get; } = new();

    // ---- Code masters ----
    public partial ServiceCodeMasterDto ToDto(ServiceCodeMaster entity);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial ServiceCodeMaster ToEntity(ServiceCodeMasterDto dto);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial void Update(ServiceCodeMasterDto dto, [MappingTarget] ServiceCodeMaster entity);

    public partial MedicationCodeMasterDto ToDto(MedicationCodeMaster entity);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial MedicationCodeMaster ToEntity(MedicationCodeMasterDto dto);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial void Update(MedicationCodeMasterDto dto, [MappingTarget] MedicationCodeMaster entity);

    public partial MedicalDeviceCodeMasterDto ToDto(MedicalDeviceCodeMaster entity);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial MedicalDeviceCodeMaster ToEntity(MedicalDeviceCodeMasterDto dto);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial void Update(MedicalDeviceCodeMasterDto dto, [MappingTarget] MedicalDeviceCodeMaster entity);

    public partial DiagnosisCodeMasterDto ToDto(DiagnosisCodeMaster entity);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial DiagnosisCodeMaster ToEntity(DiagnosisCodeMasterDto dto);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial void Update(DiagnosisCodeMasterDto dto, [MappingTarget] DiagnosisCodeMaster entity);

    public partial ModifierCodeMasterDto ToDto(ModifierCodeMaster entity);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial ModifierCodeMaster ToEntity(ModifierCodeMasterDto dto);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial void Update(ModifierCodeMasterDto dto, [MappingTarget] ModifierCodeMaster entity);

    public partial BenefitCodeMasterDto ToDto(BenefitCodeMaster entity);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial BenefitCodeMaster ToEntity(BenefitCodeMasterDto dto);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial void Update(BenefitCodeMasterDto dto, [MappingTarget] BenefitCodeMaster entity);

    public partial NphiesCodeMappingDto ToDto(NphiesCodeMapping entity);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial NphiesCodeMapping ToEntity(NphiesCodeMappingDto dto);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial void Update(NphiesCodeMappingDto dto, [MappingTarget] NphiesCodeMapping entity);

    // ---- Payers & policies ----
    public partial PayerMasterDto ToDto(PayerMaster entity);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial PayerMaster ToEntity(PayerMasterDto dto);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial void Update(PayerMasterDto dto, [MappingTarget] PayerMaster entity);

    public partial PayerPolicyMasterDto ToDto(PayerPolicyMaster entity);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial PayerPolicyMaster ToEntity(PayerPolicyMasterDto dto);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial void Update(PayerPolicyMasterDto dto, [MappingTarget] PayerPolicyMaster entity);

    public partial PolicyBenefitCoverageDto ToDto(PolicyBenefitCoverage entity);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial PolicyBenefitCoverage ToEntity(PolicyBenefitCoverageDto dto);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial void Update(PolicyBenefitCoverageDto dto, [MappingTarget] PolicyBenefitCoverage entity);

    public partial ClaimSubmissionRulesDto ToDto(ClaimSubmissionRules entity);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial ClaimSubmissionRules ToEntity(ClaimSubmissionRulesDto dto);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial void Update(ClaimSubmissionRulesDto dto, [MappingTarget] ClaimSubmissionRules entity);

    // ---- Providers ----
    public partial ClinicMasterDto ToDto(ClinicMaster entity);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial ClinicMaster ToEntity(ClinicMasterDto dto);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial void Update(ClinicMasterDto dto, [MappingTarget] ClinicMaster entity);

    public partial DoctorMasterDto ToDto(DoctorMaster entity);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial DoctorMaster ToEntity(DoctorMasterDto dto);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial void Update(DoctorMasterDto dto, [MappingTarget] DoctorMaster entity);

    public partial DoctorQualificationDto ToDto(DoctorQualification entity);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial DoctorQualification ToEntity(DoctorQualificationDto dto);
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public partial void Update(DoctorQualificationDto dto, [MappingTarget] DoctorQualification entity);
}
