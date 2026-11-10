using AutoMapper;
using NPhies_FHIR_Integration.Domain.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Application.Mapping;

/// <summary>
/// AutoMapper profile for eligibility-related entity and DTO mappings
/// </summary>
public class EligibilityMappingProfile : Profile
{
    /// <summary>
    /// Constructor that configures all mappings
    /// </summary>
    public EligibilityMappingProfile()
    {
        // Patient mappings
        CreateMap<Patient, PatientDto>().ReverseMap();

        // Coverage mappings
        CreateMap<Coverage, CoverageDto>()
            .ReverseMap();

        // Organization mappings
        CreateMap<Organization, OrganizationDto>()
            .ReverseMap();

        // Location mappings
        CreateMap<Location, LocationDto>()
            .ReverseMap();

        // Practitioner mappings
        CreateMap<Practitioner, PractitionerDto>()
            .ReverseMap();

        // MessageHeader mappings
        CreateMap<MessageHeader, MessageHeaderDto>()
            .ReverseMap();

        // EligibilityItem mappings
        CreateMap<EligibilityItem, EligibilityItemDto>()
            .ReverseMap();

        // EligibilityItemModifier mappings
        CreateMap<EligibilityItemModifier, EligibilityItemModifierDto>()
            .ReverseMap();

        // CoverageEligibilityRequest mappings
        CreateMap<CoverageEligibilityRequest, CoverageEligibilityRequestDto>()
            .ForMember(dest => dest.Purpose, opt => opt.MapFrom(src => src.GetPurposeArray()))
            .ReverseMap()
            .ForMember(dest => dest.PurposeJson, opt => opt.Ignore());

        // BenefitBalance mappings
        CreateMap<BenefitBalance, BenefitBalanceDto>()
            .ReverseMap();

        // Benefit mappings
        CreateMap<Benefit, BenefitDto>()
            .ReverseMap();

        // EligibilityError mappings
        CreateMap<EligibilityError, EligibilityErrorDto>()
            .ReverseMap();

        // CoverageEligibilityResponse mappings
        CreateMap<CoverageEligibilityResponse, CoverageEligibilityResponseDto>()
            .ForMember(dest => dest.CoveredServices, opt => opt.MapFrom(src => src.GetCoveredServices()))
            .ForMember(dest => dest.ExcludedServices, opt => opt.MapFrom(src => src.GetExcludedServices()))
            .ForMember(dest => dest.Limitations, opt => opt.MapFrom(src => src.GetLimitations()))
            .ReverseMap()
            .ForMember(dest => dest.CoveredServicesJson, opt => opt.Ignore())
            .ForMember(dest => dest.ExcludedServicesJson, opt => opt.Ignore())
            .ForMember(dest => dest.LimitationsJson, opt => opt.Ignore());
    }
}
