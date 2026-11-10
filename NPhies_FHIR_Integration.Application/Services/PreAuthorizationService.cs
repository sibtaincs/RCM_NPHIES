using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Domain.Interfaces;

namespace NPhies_FHIR_Integration.Application.Services;

/// <summary>
/// Pre-Authorization Service Implementation
/// </summary>
public class PreAuthorizationService : IPreAuthorizationService
{
 private readonly IPreAuthorizationRepository _repository;
    private readonly ILogger<PreAuthorizationService> _logger;

    public PreAuthorizationService(
    IPreAuthorizationRepository repository,
        ILogger<PreAuthorizationService> logger)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<PreAuthorizationRequestDto> CreatePreAuthorizationAsync(CreatePreAuthorizationRequestDto createDto)
    {
        _logger.LogInformation("Creating pre-authorization request for patient {PatientId}", createDto.PatientId);

        // Validate request identifier uniqueness
        if (await _repository.RequestIdentifierExistsAsync(createDto.RequestIdentifier))
  {
       throw new InvalidOperationException($"Pre-authorization request identifier '{createDto.RequestIdentifier}' already exists.");
        }

        // Create entity
        var entity = new PreAuthorizationRequest
        {
    Id = Guid.NewGuid().ToString(),
            RequestIdentifier = createDto.RequestIdentifier,
     RequestIdentifierSystem = createDto.RequestIdentifierSystem,
            Status = "active",
   Type = createDto.Type,
         Priority = createDto.Priority,
         PatientId = createDto.PatientId,
            ProviderId = createDto.ProviderId,
            InsurerId = createDto.InsurerId,
            CoverageId = createDto.CoverageId,
            ServicedPeriodStart = createDto.ServicedPeriodStart,
ServicedPeriodEnd = createDto.ServicedPeriodEnd,
    ServicedDate = createDto.ServicedDate,
      EnteredDate = DateTime.UtcNow,
     Purpose = createDto.Purpose,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

   // Add items
        foreach (var itemDto in createDto.Items)
        {
  entity.Items.Add(new PreAuthorizationItem
       {
          Id = Guid.NewGuid().ToString(),
        PreAuthorizationRequestId = entity.Id,
    Sequence = itemDto.Sequence,
           CareTeamSequence = itemDto.CareTeamSequence,
       DiagnosisSequence = itemDto.DiagnosisSequence,
    InformationSequence = itemDto.InformationSequence,
        ProductOrServiceCode = itemDto.ProductOrServiceCode,
                ProductOrServiceSystem = itemDto.ProductOrServiceSystem,
                ProductOrServiceDescription = itemDto.ProductOrServiceDescription,
                ServicedDate = itemDto.ServicedDate,
           ServicedPeriodStart = itemDto.ServicedPeriodStart,
            ServicedPeriodEnd = itemDto.ServicedPeriodEnd,
              Quantity = itemDto.Quantity,
         UnitPrice = itemDto.UnitPrice,
     NetAmount = itemDto.NetAmount,
     BodySiteCode = itemDto.BodySiteCode,
       BodySiteSystem = itemDto.BodySiteSystem,
      SubSiteCode = itemDto.SubSiteCode,
    CurrencyCode = itemDto.CurrencyCode,
           LocationId = itemDto.LocationId,
                PractitionerId = itemDto.PractitionerId,
       Notes = itemDto.Notes,
     CreatedAt = DateTime.UtcNow,
          });
   }

        // Add diagnoses
        foreach (var diagnosisDto in createDto.Diagnoses)
  {
            entity.Diagnoses.Add(new PreAuthorizationDiagnosis
 {
           Id = Guid.NewGuid().ToString(),
             PreAuthorizationRequestId = entity.Id,
      Sequence = diagnosisDto.Sequence,
        DiagnosisCode = diagnosisDto.DiagnosisCode,
     DiagnosisSystem = diagnosisDto.DiagnosisSystem,
                DiagnosisType = diagnosisDto.DiagnosisType,
           OnAdmission = diagnosisDto.OnAdmission,
      Notes = diagnosisDto.Notes,
          CreatedAt = DateTime.UtcNow,
 IsActive = true
        });
        }

        // Add supporting info
        foreach (var supportingInfoDto in createDto.SupportingInfo)
        {
            entity.SupportingInfo.Add(new PreAuthorizationSupportingInfo
   {
       Id = Guid.NewGuid().ToString(),
    PreAuthorizationRequestId = entity.Id,
Sequence = supportingInfoDto.Sequence,
           Category = supportingInfoDto.Category,
       CategorySystem = supportingInfoDto.CategorySystem,
             CodeValue = supportingInfoDto.CodeValue,
      StringValue = supportingInfoDto.StringValue,
             DateValue = supportingInfoDto.DateValue,
             QuantityValue = supportingInfoDto.QuantityValue,
                Notes = supportingInfoDto.Notes,
            CreatedAt = DateTime.UtcNow,
    IsActive = true
            });
 }

        await _repository.AddAsync(entity);
        await _repository.SaveChangesAsync();

        _logger.LogInformation("Pre-authorization request created: {RequestId}", entity.Id);

        // Load the full entity with relationships
 var savedEntity = await _repository.GetByIdWithDetailsAsync(entity.Id);
        return MapToDto(savedEntity!);
 }

    public async Task<PreAuthorizationRequestDto?> GetPreAuthorizationByIdAsync(string id)
    {
        var entity = await _repository.GetByIdWithDetailsAsync(id);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<PreAuthorizationRequestDto?> GetPreAuthorizationByRequestIdentifierAsync(string requestIdentifier)
    {
    var entity = await _repository.GetByRequestIdentifierAsync(requestIdentifier);
        return entity == null ? null : MapToDto(entity);
    }

    public async Task<IEnumerable<PreAuthorizationSummaryDto>> GetPreAuthorizationsByPatientIdAsync(string patientId)
    {
        var entities = await _repository.GetByPatientIdAsync(patientId);
        return entities.Select(MapToSummaryDto);
 }

    public async Task<IEnumerable<PreAuthorizationSummaryDto>> GetPreAuthorizationsByProviderIdAsync(string providerId)
    {
        var entities = await _repository.GetByProviderIdAsync(providerId);
  return entities.Select(MapToSummaryDto);
    }

    public async Task<IEnumerable<PreAuthorizationSummaryDto>> GetActivePreAuthorizationsByPatientIdAsync(string patientId)
    {
        var entities = await _repository.GetActiveByPatientIdAsync(patientId);
        return entities.Select(MapToSummaryDto);
    }

    public async Task<IEnumerable<PreAuthorizationSummaryDto>> GetPendingPreAuthorizationsAsync()
 {
        var entities = await _repository.GetPendingRequestsAsync();
        return entities.Select(MapToSummaryDto);
  }

 public async Task<PreAuthorizationRequestDto> UpdatePreAuthorizationAsync(string id, UpdatePreAuthorizationRequestDto updateDto)
    {
        _logger.LogInformation("Updating pre-authorization request: {RequestId}", id);

        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
 {
            throw new KeyNotFoundException($"Pre-authorization request with ID '{id}' not found.");
    }

        // Update fields
        if (!string.IsNullOrEmpty(updateDto.Status))
            entity.Status = updateDto.Status;

        if (!string.IsNullOrEmpty(updateDto.Priority))
    entity.Priority = updateDto.Priority;

 if (updateDto.ServicedPeriodStart.HasValue)
            entity.ServicedPeriodStart = updateDto.ServicedPeriodStart;

        if (updateDto.ServicedPeriodEnd.HasValue)
            entity.ServicedPeriodEnd = updateDto.ServicedPeriodEnd;

        if (updateDto.ServicedDate.HasValue)
    entity.ServicedDate = updateDto.ServicedDate;

        entity.UpdatedAt = DateTime.UtcNow;

      _repository.Update(entity);
        await _repository.SaveChangesAsync();

        _logger.LogInformation("Pre-authorization request updated: {RequestId}", id);

        var updatedEntity = await _repository.GetByIdWithDetailsAsync(id);
  return MapToDto(updatedEntity!);
    }

    public async Task<bool> CancelPreAuthorizationAsync(string id, string cancellationReason)
    {
        _logger.LogInformation("Cancelling pre-authorization request: {RequestId}", id);

        var entity = await _repository.GetByIdAsync(id);
        if (entity == null)
      {
        _logger.LogWarning("Pre-authorization request not found: {RequestId}", id);
            return false;
        }

    entity.Status = "cancelled";
        entity.UpdatedAt = DateTime.UtcNow;

        _repository.Update(entity);
        await _repository.SaveChangesAsync();

        _logger.LogInformation("Pre-authorization request cancelled: {RequestId}, Reason: {Reason}", id, cancellationReason);
        return true;
    }

    public async Task<bool> RequestIdentifierExistsAsync(string requestIdentifier)
    {
     return await _repository.RequestIdentifierExistsAsync(requestIdentifier);
    }

    public async Task<PreAuthStatisticsDto> GetStatisticsAsync(string providerId, DateTime? fromDate = null, DateTime? toDate = null)
    {
        var stats = await _repository.GetStatisticsAsync(providerId, fromDate, toDate);
        
        return new PreAuthStatisticsDto
      {
    TotalRequests = stats.TotalRequests,
            ActiveRequests = stats.ActiveRequests,
     CompletedRequests = stats.CompletedRequests,
            CancelledRequests = stats.CancelledRequests,
            PendingRequests = stats.PendingRequests,
            ApprovedRequests = stats.ApprovedRequests,
       DeniedRequests = stats.DeniedRequests,
    AverageProcessingTimeHours = stats.AverageProcessingTimeHours
        };
    }

    public async Task<PreAuthorizationRequestDto> SubmitToNphiesAsync(string id)
    {
        _logger.LogInformation("Submitting pre-authorization request to NPHIES: {RequestId}", id);

        var entity = await _repository.GetByIdWithDetailsAsync(id);
        if (entity == null)
        {
            throw new KeyNotFoundException($"Pre-authorization request with ID '{id}' not found.");
   }

   // TODO: Implement actual FHIR message generation and NPHIES submission
        // This is a placeholder for the FHIR integration

        _logger.LogInformation("Pre-authorization request submitted to NPHIES: {RequestId}", id);

        return MapToDto(entity);
    }

    public async Task<PreAuthorizationResponseDto> ProcessNphiesResponseAsync(string requestId, string responseFhirJson)
    {
     _logger.LogInformation("Processing NPHIES response for request: {RequestId}", requestId);

        // TODO: Implement actual FHIR response parsing and processing
        // This is a placeholder for the FHIR integration

        throw new NotImplementedException("FHIR response processing not yet implemented");
    }

    // Mapping methods
    private PreAuthorizationRequestDto MapToDto(PreAuthorizationRequest entity)
    {
        return new PreAuthorizationRequestDto
        {
      Id = entity.Id,
            RequestIdentifier = entity.RequestIdentifier,
     RequestIdentifierSystem = entity.RequestIdentifierSystem,
   Status = entity.Status,
            Type = entity.Type,
            Priority = entity.Priority,
            PatientId = entity.PatientId,
            PatientName = entity.Patient != null ? $"{entity.Patient.FirstName} {entity.Patient.LastName}" : null,
       ProviderId = entity.ProviderId,
     ProviderName = entity.Provider?.OrganizationName,
            InsurerId = entity.InsurerId,
            InsurerName = entity.Insurer?.OrganizationName,
          CoverageId = entity.CoverageId,
            PolicyNumber = entity.Coverage?.PolicyNumber,
ServicedPeriodStart = entity.ServicedPeriodStart,
      ServicedPeriodEnd = entity.ServicedPeriodEnd,
          ServicedDate = entity.ServicedDate,
   EnteredDate = entity.EnteredDate,
            Purpose = entity.Purpose,
       MessageHeaderId = entity.MessageHeaderId,
            CreatedAt = entity.CreatedAt,
      UpdatedAt = entity.UpdatedAt,
     IsActive = entity.IsActive,
Items = entity.Items?.Select(i => new PreAuthorizationItemDto
            {
            Id = i.Id,
      Sequence = i.Sequence,
  CareTeamSequence = i.CareTeamSequence,
     DiagnosisSequence = i.DiagnosisSequence,
     InformationSequence = i.InformationSequence,
    ProductOrServiceCode = i.ProductOrServiceCode,
       ProductOrServiceSystem = i.ProductOrServiceSystem,
           ProductOrServiceDescription = i.ProductOrServiceDescription,
    ServicedDate = i.ServicedDate,
  ServicedPeriodStart = i.ServicedPeriodStart,
        ServicedPeriodEnd = i.ServicedPeriodEnd,
         Quantity = i.Quantity,
     UnitPrice = i.UnitPrice,
         NetAmount = i.NetAmount,
        BodySiteCode = i.BodySiteCode,
  BodySiteSystem = i.BodySiteSystem,
   SubSiteCode = i.SubSiteCode,
    CurrencyCode = i.CurrencyCode,
         LocationId = i.LocationId,
        PractitionerId = i.PractitionerId,
   Notes = i.Notes
         }).ToList() ?? new List<PreAuthorizationItemDto>(),
      Diagnoses = entity.Diagnoses?.Select(d => new PreAuthorizationDiagnosisDto
            {
         Id = d.Id,
         Sequence = d.Sequence,
       DiagnosisCode = d.DiagnosisCode,
     DiagnosisSystem = d.DiagnosisSystem,
       DiagnosisType = d.DiagnosisType,
        OnAdmission = d.OnAdmission,
      Notes = d.Notes
            }).ToList() ?? new List<PreAuthorizationDiagnosisDto>(),
     Response = entity.Response != null ? MapResponseToDto(entity.Response) : null
  };
    }

    private PreAuthorizationSummaryDto MapToSummaryDto(PreAuthorizationRequest entity)
    {
        return new PreAuthorizationSummaryDto
        {
      Id = entity.Id,
     RequestIdentifier = entity.RequestIdentifier,
  Status = entity.Status,
    Type = entity.Type,
            PatientId = entity.PatientId,
        PatientName = entity.Patient != null ? $"{entity.Patient.FirstName} {entity.Patient.LastName}" : null,
      ProviderId = entity.ProviderId,
      ProviderName = entity.Provider?.OrganizationName,
            InsurerId = entity.InsurerId,
        InsurerName = entity.Insurer?.OrganizationName,
    EnteredDate = entity.EnteredDate,
            ItemCount = entity.Items?.Count ?? 0,
            HasResponse = entity.Response != null,
   PreAuthRef = entity.Response?.PreAuthRef,
          ResponseOutcome = entity.Response?.Outcome
        };
 }

    private PreAuthorizationResponseDto MapResponseToDto(PreAuthorizationResponse response)
    {
        return new PreAuthorizationResponseDto
        {
   Id = response.Id,
   ResponseIdentifier = response.ResponseIdentifier,
ResponseIdentifierSystem = response.ResponseIdentifierSystem,
    Status = response.Status,
 Outcome = response.Outcome,
   Disposition = response.Disposition,
         PreAuthRef = response.PreAuthRef,
       ValidityPeriodStart = response.ValidityPeriodStart,
        ValidityPeriodEnd = response.ValidityPeriodEnd,
       PatientId = response.PatientId,
  InsurerId = response.InsurerId,
     RequestorId = response.RequestorId,
  ResponseDate = response.ResponseDate,
        ResponseItems = response.ResponseItems?.Select(i => new PreAuthorizationResponseItemDto
     {
        Id = i.Id,
            Sequence = i.Sequence,
      RequestSequence = i.RequestSequence,
          Decision = i.Decision,
          AdjudicationResult = i.AdjudicationResult,
     ApprovedQuantity = i.ApprovedQuantity,
     ApprovedAmount = i.ApprovedAmount,
       BenefitAmount = i.BenefitAmount,
 Currency = i.Currency,
         DenialReason = i.DenialReason,
       Notes = i.Notes
            }).ToList() ?? new List<PreAuthorizationResponseItemDto>(),
            Errors = response.Errors?.Select(e => new PreAuthorizationResponseErrorDto
   {
   Id = e.Id,
         ErrorCode = e.ErrorCode,
          ErrorCodeSystem = e.ErrorCodeSystem,
  ErrorMessage = e.ErrorMessage,
        Severity = e.Severity,
         ErrorLocation = e.ErrorLocation
  }).ToList() ?? new List<PreAuthorizationResponseErrorDto>()
        };
    }
}
