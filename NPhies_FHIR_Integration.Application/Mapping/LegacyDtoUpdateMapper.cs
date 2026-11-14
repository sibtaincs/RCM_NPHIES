using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;
using Riok.Mapperly.Abstractions;

namespace NPhies_FHIR_Integration.Application.Mapping;

/// <summary>
/// Partial updates from the legacy <c>Update*Dto</c> types onto tracked entities. A null source member leaves the
/// entity's value unchanged; every update also stamps <c>UpdatedAt</c>.
/// </summary>
[Mapper(AllowNullPropertyAssignment = false, RequiredMappingStrategy = RequiredMappingStrategy.Source)]
public static partial class LegacyDtoUpdateMapper
{
    public static void Apply(UpdatePatientDto source, Patient target)
    {
        Copy(source, target);
        var identifierType = PatientIdentifierTypes.ToCode(source.IdentifierType, source.NationalId);
        if (identifierType != null)
            target.NationalIdType = identifierType;
        target.UpdatedAt = DateTime.UtcNow;
    }

    public static void Apply(UpdateCoverageDto source, Coverage target)
    {
        Copy(source, target);
        target.UpdatedAt = DateTime.UtcNow;
    }

    public static void Apply(UpdateClaimDto source, Claim target)
    {
        Copy(source, target);
        target.UpdatedAt = DateTime.UtcNow;
    }

    public static void Apply(UpdateCommunicationDto source, Communication target)
    {
        Copy(source, target);
        target.UpdatedAt = DateTime.UtcNow;
    }

    public static void Apply(UpdateCommunicationRequestDto source, CommunicationRequest target)
    {
        Copy(source, target);
        target.UpdatedAt = DateTime.UtcNow;
    }

    public static void Apply(UpdateTaskRequestDto source, CancellationRequest target)
    {
        Copy(source, target);
        target.UpdatedAt = DateTime.UtcNow;
    }

    public static void Apply(UpdateTaskResponseDto source, CancellationResponse target)
    {
        Copy(source, target);
        target.UpdatedAt = DateTime.UtcNow;
    }

    [MapperIgnoreSource(nameof(UpdatePatientDto.IdentifierType))] // resolved to NationalIdType in Apply
    private static partial void Copy(UpdatePatientDto source, [MappingTarget] Patient target);
    private static partial void Copy(UpdateCoverageDto source, [MappingTarget] Coverage target);
    private static partial void Copy(UpdateClaimDto source, [MappingTarget] Claim target);
    private static partial void Copy(UpdateCommunicationDto source, [MappingTarget] Communication target);
    private static partial void Copy(UpdateCommunicationRequestDto source, [MappingTarget] CommunicationRequest target);
    private static partial void Copy(UpdateTaskRequestDto source, [MappingTarget] CancellationRequest target);
    private static partial void Copy(UpdateTaskResponseDto source, [MappingTarget] CancellationResponse target);
}
