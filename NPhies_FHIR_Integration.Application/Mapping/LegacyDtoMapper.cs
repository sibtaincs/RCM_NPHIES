using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;
using Riok.Mapperly.Abstractions;
using DomainDTOs = NPhies_FHIR_Integration.Domain.DTOs;

namespace NPhies_FHIR_Integration.Application.Mapping;

/// <summary>
/// Compile-time (Mapperly) mappings between the legacy entities and their DTOs, used by the legacy services and
/// controllers. Read and create mappings live here; partial updates live in <see cref="LegacyDtoUpdateMapper"/>.
/// </summary>
/// <remarks>
/// Collections mapped from a null source become empty, and nested entities are mapped through the methods
/// declared here, so the generated code never needs a runtime type lookup.
/// </remarks>
[Mapper(RequiredMappingStrategy = RequiredMappingStrategy.Target)]
public static partial class LegacyDtoMapper
{
    // ---- Patients ----

    [MapPropertyFromSource(nameof(PatientDto.IdentifierType), Use = nameof(DescribePatientIdentifier))]
    public static partial PatientDto ToDto(Patient source);

    public static List<PatientDto> ToDtos(IEnumerable<Patient> source) => source.Select(ToDto).ToList();

    [MapPropertyFromSource(nameof(Patient.NationalIdType), Use = nameof(CreatePatientIdentifierCode))]
    [MapPropertyFromSource(nameof(Patient.FullName), Use = nameof(CreatePatientFullName))]
    [MapPropertyFromSource(nameof(Patient.FullNameArabic), Use = nameof(CreatePatientFullNameArabic))]
    [MapValue(nameof(Patient.Id), Use = nameof(NewId))]
    [MapValue(nameof(Patient.CreatedAt), Use = nameof(UtcNow))]
    [MapValue(nameof(Patient.IsActive), true)]
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public static partial Patient ToEntity(CreatePatientDto source);

    private static string DescribePatientIdentifier(Patient source) =>
        PatientIdentifierTypes.Describe(source.NationalIdType, source.NationalId);

    private static string? CreatePatientIdentifierCode(CreatePatientDto source) =>
        PatientIdentifierTypes.ToCode(source.IdentifierType, source.NationalId);

    private static string? CreatePatientFullName(CreatePatientDto source) =>
        (source.FirstName + " " + source.LastName).Trim();

    private static string? CreatePatientFullNameArabic(CreatePatientDto source) =>
        source.FullNameArabic ?? ((source.FirstNameArabic ?? "") + " " + (source.LastNameArabic ?? "")).Trim();

    // ---- Coverage ----

    public static partial CoverageDto ToDto(Coverage source);

    public static List<CoverageDto> ToDtos(IEnumerable<Coverage> source) => source.Select(ToDto).ToList();

    [MapValue(nameof(Coverage.Id), Use = nameof(NewId))]
    [MapValue(nameof(Coverage.Status), "active")]
    [MapValue(nameof(Coverage.CreatedAt), Use = nameof(UtcNow))]
    [MapValue(nameof(Coverage.DeductibleMet), Use = nameof(Zero))]
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public static partial Coverage ToEntity(CreateCoverageDto source);

    // ---- Organizations (the controller uses the Domain DTO) ----

    public static partial DomainDTOs.OrganizationDto ToDto(Organization source);

    public static List<DomainDTOs.OrganizationDto> ToDtos(IEnumerable<Organization> source) => source.Select(ToDto).ToList();

    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public static partial Organization ToEntity(DomainDTOs.OrganizationDto source);

    // ---- Claims ----

    public static partial ClaimDto ToDto(Claim source);

    public static List<ClaimDto> ToDtos(IEnumerable<Claim> source) => source.Select(ToDto).ToList();

    [MapValue(nameof(Claim.Id), Use = nameof(NewId))]
    [MapValue(nameof(Claim.Status), "draft")]
    [MapValue(nameof(Claim.CreatedAt), Use = nameof(UtcNow))]
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public static partial Claim ToEntity(CreateClaimDto source);

    public static partial ClaimItemDto ToDto(ClaimItem source);

    public static List<ClaimItemDto> ToDtos(IEnumerable<ClaimItem> source) => source.Select(ToDto).ToList();

    [MapValue(nameof(ClaimItem.Id), Use = nameof(NewId))]
    [MapperIgnoreTarget(nameof(ClaimItem.ClaimId))]
    [MapperIgnoreTarget(nameof(ClaimItem.Claim))]
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public static partial ClaimItem ToEntity(CreateClaimItemDto source);

    public static partial ClaimDiagnosisDto ToDto(ClaimDiagnosis source);

    public static List<ClaimDiagnosisDto> ToDtos(IEnumerable<ClaimDiagnosis> source) => source.Select(ToDto).ToList();

    [MapValue(nameof(ClaimDiagnosis.Id), Use = nameof(NewId))]
    [MapperIgnoreTarget(nameof(ClaimDiagnosis.ClaimId))]
    [MapperIgnoreTarget(nameof(ClaimDiagnosis.Claim))]
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public static partial ClaimDiagnosis ToEntity(CreateClaimDiagnosisDto source);

    [MapPropertyFromSource(nameof(ClaimCareTeamDto.PractitionerName), Use = nameof(CareTeamPractitionerName))]
    private static partial ClaimCareTeamDto ToDto(ClaimCareTeam source);

    [MapPropertyFromSource(nameof(ClaimCareTeam.Id), Use = nameof(CareTeamIdOrNew))]
    [MapperIgnoreTarget(nameof(ClaimCareTeam.ClaimId))]
    [MapperIgnoreTarget(nameof(ClaimCareTeam.Claim))]
    [MapperIgnoreTarget(nameof(ClaimCareTeam.Practitioner))]
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    private static partial ClaimCareTeam ToEntity(ClaimCareTeamDto source);

    private static partial ClaimSupportingInfoDto ToDto(ClaimSupportingInfo source);

    [MapPropertyFromSource(nameof(ClaimSupportingInfo.Id), Use = nameof(SupportingInfoIdOrNew))]
    [MapperIgnoreTarget(nameof(ClaimSupportingInfo.ClaimId))]
    [MapperIgnoreTarget(nameof(ClaimSupportingInfo.Claim))]
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    private static partial ClaimSupportingInfo ToEntity(ClaimSupportingInfoDto source);

    private static string? CareTeamPractitionerName(ClaimCareTeam source) => source.Practitioner?.GetDisplayName();

    private static string CareTeamIdOrNew(ClaimCareTeamDto source) =>
        string.IsNullOrWhiteSpace(source.Id) ? NewId() : source.Id;

    private static string SupportingInfoIdOrNew(ClaimSupportingInfoDto source) =>
        string.IsNullOrWhiteSpace(source.Id) ? NewId() : source.Id;

    // ---- Claim responses ----

    [MapPropertyFromSource(nameof(ClaimResponseDto.Outcome), Use = nameof(ClaimResponseOutcome))]
    [MapPropertyFromSource(nameof(ClaimResponseDto.Items), Use = nameof(ClaimResponseItemsInOrder))]
    public static partial ClaimResponseDto ToDto(ClaimResponse source);

    public static List<ClaimResponseDto> ToDtos(IEnumerable<ClaimResponse> source) => source.Select(ToDto).ToList();


    private static partial ClaimResponseItemDto ToDto(ClaimResponseItem source);

    private static string? ClaimResponseOutcome(ClaimResponse source) => source.Outcome ?? source.ClaimResponseStatus;

    private static List<ClaimResponseItemDto> ClaimResponseItemsInOrder(ClaimResponse source) =>
        (source.Items ?? []).OrderBy(i => i.ItemSequence).Select(ToDto).ToList();

    // ---- Eligibility (Domain DTOs) ----

    [MapPropertyFromSource(nameof(DomainDTOs.CoverageEligibilityRequestDto.Purpose), Use = nameof(EligibilityPurpose))]
    public static partial DomainDTOs.CoverageEligibilityRequestDto ToDto(CoverageEligibilityRequest source);

    public static List<DomainDTOs.CoverageEligibilityRequestDto> ToDtos(IEnumerable<CoverageEligibilityRequest> source) =>
        source.Select(ToDto).ToList();

    [MapperIgnoreTarget(nameof(CoverageEligibilityRequest.PurposeJson))]
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public static partial CoverageEligibilityRequest ToEntity(DomainDTOs.CoverageEligibilityRequestDto source);

    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    private static partial EligibilityItem ToEntity(DomainDTOs.EligibilityItemDto source);

    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    private static partial EligibilityItemModifier ToEntity(DomainDTOs.EligibilityItemModifierDto source);

    [MapPropertyFromSource(nameof(DomainDTOs.CoverageEligibilityResponseDto.CoveredServices), Use = nameof(EligibilityCoveredServices))]
    [MapPropertyFromSource(nameof(DomainDTOs.CoverageEligibilityResponseDto.ExcludedServices), Use = nameof(EligibilityExcludedServices))]
    [MapPropertyFromSource(nameof(DomainDTOs.CoverageEligibilityResponseDto.Limitations), Use = nameof(EligibilityLimitations))]
    public static partial DomainDTOs.CoverageEligibilityResponseDto ToDto(CoverageEligibilityResponse source);

    private static List<string> EligibilityPurpose(CoverageEligibilityRequest source) => source.GetPurposeArray();
    private static List<string> EligibilityCoveredServices(CoverageEligibilityResponse source) => source.GetCoveredServices();
    private static List<string> EligibilityExcludedServices(CoverageEligibilityResponse source) => source.GetExcludedServices();
    private static List<string> EligibilityLimitations(CoverageEligibilityResponse source) => source.GetLimitations();

    // ---- Communications ----

    [MapPropertyFromSource(nameof(CommunicationDto.PayloadAttachmentSizeKB), Use = nameof(AttachmentSizeKb))]
    [MapPropertyFromSource(nameof(CommunicationDto.HasAttachment), Use = nameof(HasAttachment))]
    public static partial CommunicationDto ToDto(Communication source);

    public static List<CommunicationDto> ToDtos(IEnumerable<Communication> source) => source.Select(ToDto).ToList();

    [MapValue(nameof(Communication.Id), Use = nameof(NewId))]
    [MapPropertyFromSource(nameof(Communication.Status), Use = nameof(CommunicationStatusOrDefault))]
    [MapValue(nameof(Communication.ProcessingStatus), "received")]
    [MapPropertyFromSource(nameof(Communication.PayloadAttachmentData), Use = nameof(DecodeAttachment))]
    [MapValue(nameof(Communication.CreatedAt), Use = nameof(UtcNow))]
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public static partial Communication ToEntity(CreateCommunicationDto source);

    private static decimal AttachmentSizeKb(Communication source) =>
        source.PayloadAttachmentData != null ? Math.Round((decimal)source.PayloadAttachmentData.Length / 1024, 2) : 0m;

    private static bool HasAttachment(Communication source) =>
        source.PayloadAttachmentData != null && source.PayloadAttachmentData.Length > 0;

    private static string CommunicationStatusOrDefault(CreateCommunicationDto source) => source.Status ?? "completed";

    // An absent attachment is stored as an empty array (the behaviour before the switch to Mapperly).
    private static byte[]? DecodeAttachment(CreateCommunicationDto source) =>
        !string.IsNullOrEmpty(source.PayloadAttachmentDataBase64) ? Convert.FromBase64String(source.PayloadAttachmentDataBase64) : [];

    public static partial CommunicationRequestDto ToDto(CommunicationRequest source);

    public static List<CommunicationRequestDto> ToDtos(IEnumerable<CommunicationRequest> source) => source.Select(ToDto).ToList();

    [MapValue(nameof(CommunicationRequest.Id), Use = nameof(NewId))]
    [MapPropertyFromSource(nameof(CommunicationRequest.Status), Use = nameof(CommunicationRequestStatusOrDefault))]
    [MapValue(nameof(CommunicationRequest.CreatedAt), Use = nameof(UtcNow))]
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public static partial CommunicationRequest ToEntity(CreateCommunicationRequestDto source);

    private static string CommunicationRequestStatusOrDefault(CreateCommunicationRequestDto source) => source.Status ?? "active";

    // ---- Cancellation (Task) requests and responses ----

    public static partial TaskRequestDto ToDto(CancellationRequest source);

    public static List<TaskRequestDto> ToDtos(IEnumerable<CancellationRequest> source) => source.Select(ToDto).ToList();

    [MapValue(nameof(CancellationRequest.Id), Use = nameof(NewId))]
    [MapPropertyFromSource(nameof(CancellationRequest.Status), Use = nameof(TaskRequestStatusOrDefault))]
    [MapValue(nameof(CancellationRequest.CreatedAt), Use = nameof(UtcNow))]
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public static partial CancellationRequest ToEntity(CreateTaskRequestDto source);

    private static string TaskRequestStatusOrDefault(CreateTaskRequestDto source) => source.Status ?? "requested";

    [MapPropertyFromSource(nameof(TaskResponseDto.IsSuccessful), Use = nameof(TaskResponseIsSuccessful))]
    [MapPropertyFromSource(nameof(TaskResponseDto.IsError), Use = nameof(TaskResponseIsError))]
    [MapperIgnoreTarget(nameof(TaskResponseDto.TaskRequestId))] // the entity has no such member; it was never mapped
    public static partial TaskResponseDto ToDto(CancellationResponse source);

    public static List<TaskResponseDto> ToDtos(IEnumerable<CancellationResponse> source) => source.Select(ToDto).ToList();

    [MapValue(nameof(CancellationResponse.Id), Use = nameof(NewId))]
    [MapPropertyFromSource(nameof(CancellationResponse.Status), Use = nameof(TaskResponseStatusOrDefault))]
    [MapValue(nameof(CancellationResponse.CreatedAt), Use = nameof(UtcNow))]
    [MapperRequiredMapping(RequiredMappingStrategy.None)]
    public static partial CancellationResponse ToEntity(CreateTaskResponseDto source);

    private static bool TaskResponseIsSuccessful(CancellationResponse source) => source.IsSuccessful();
    private static bool TaskResponseIsError(CancellationResponse source) => source.IsError();
    private static string TaskResponseStatusOrDefault(CreateTaskResponseDto source) => source.Status ?? "completed";

    // ---- Shared value providers ----

    private static string NewId() => Guid.NewGuid().ToString();
    private static DateTime UtcNow() => DateTime.UtcNow;
    private static decimal Zero() => 0m;
}
