using AutoMapper;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Application.Mapping;

/// <summary>
/// AutoMapper profile for all master-data entity ? DTO pairs.
/// Without these, <c>MasterDataServiceBase.GetAllAsync</c> throws
/// AutoMapperMappingException when projecting entities to DTOs.
/// </summary>
public class MasterDataMappingProfile : Profile
{
    public MasterDataMappingProfile()
    {
        // ---- Code masters ----
        CreateMap<ServiceCodeMaster, ServiceCodeMasterDto>().ReverseMap();
        CreateMap<MedicationCodeMaster, MedicationCodeMasterDto>().ReverseMap();
        CreateMap<MedicalDeviceCodeMaster, MedicalDeviceCodeMasterDto>().ReverseMap();
        CreateMap<DiagnosisCodeMaster, DiagnosisCodeMasterDto>().ReverseMap();
   CreateMap<ModifierCodeMaster, ModifierCodeMasterDto>().ReverseMap();
        CreateMap<BenefitCodeMaster, BenefitCodeMasterDto>().ReverseMap();
        CreateMap<NphiesCodeMapping, NphiesCodeMappingDto>().ReverseMap();

    // ---- Payers & policies ----
    CreateMap<PayerMaster, PayerMasterDto>().ReverseMap();
      CreateMap<PayerPolicyMaster, PayerPolicyMasterDto>().ReverseMap();
  CreateMap<PolicyBenefitCoverage, PolicyBenefitCoverageDto>().ReverseMap();
   CreateMap<ClaimSubmissionRules, ClaimSubmissionRulesDto>().ReverseMap();

        // ---- Providers ----
     CreateMap<ClinicMaster, ClinicMasterDto>().ReverseMap();
        CreateMap<DoctorMaster, DoctorMasterDto>().ReverseMap();
   CreateMap<DoctorQualification, DoctorQualificationDto>().ReverseMap();
    }
}
