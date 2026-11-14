using NPhies_FHIR_Integration.Application.Mapping;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Repositories;

namespace NPhies_FHIR_Integration.Application.Services;

/// <summary>
/// Service interface for ClaimResponse operations
/// </summary>
public interface IClaimResponseService
{
    /// <summary>
    /// Create a new claim response
    /// </summary>
    Task<ClaimResponseDto> CreateClaimResponseAsync(CreateClaimResponseDto dto);

    /// <summary>
    /// Get claim response by ID
    /// </summary>
    Task<ClaimResponseDto?> GetClaimResponseAsync(string responseId);

    /// <summary>
    /// Get response for a claim
    /// </summary>
    Task<ClaimResponseDto?> GetResponseByClaimIdAsync(string claimId);

    /// <summary>
    /// Get response for a claim (alias method)
    /// </summary>
    Task<ClaimResponseDto?> GetResponseForClaimAsync(string claimId);

    /// <summary>
   /// Get responses by status
    /// </summary>
    Task<IEnumerable<ClaimResponseDto>> GetResponsesByStatusAsync(string status);

    /// <summary>
    /// Get responses with pre-auth reference
    /// </summary>
    Task<IEnumerable<ClaimResponseDto>> GetResponsesByPreAuthRefAsync(string preAuthRef);

    /// <summary>
    /// Update claim response
    /// </summary>
    Task<ClaimResponseDto> UpdateClaimResponseAsync(string responseId, UpdateClaimResponseDto dto);
}

/// <summary>
/// Implementation of IClaimResponseService
/// </summary>
public class ClaimResponseService : IClaimResponseService
{
    private readonly IClaimResponseRepository _responseRepository;

    /// <summary>
    /// Constructor with dependencies
    /// </summary>
    public ClaimResponseService(IClaimResponseRepository responseRepository)
    {
 _responseRepository = responseRepository ?? throw new ArgumentNullException(nameof(responseRepository));
  }

    /// <summary>
 /// Create a new claim response
    /// </summary>
  public Task<ClaimResponseDto> CreateClaimResponseAsync(CreateClaimResponseDto dto) =>
        Task.FromException<ClaimResponseDto>(new InvalidOperationException("Failed to create claim response", ManualEditNotSupported()));

    /// <summary>
    /// Get claim response by ID
   /// </summary>
    public async Task<ClaimResponseDto?> GetClaimResponseAsync(string responseId)
    {
        try
        {
      var response = await _responseRepository.GetWithDetailsAsync(responseId);
   return response != null ? LegacyDtoMapper.ToDto(response) : null;
        }
        catch (Exception ex)
        {
   throw new InvalidOperationException("Failed to retrieve claim response", ex);
     }
    }

    /// <summary>
    /// Get response for a claim
    /// </summary>
    public async Task<ClaimResponseDto?> GetResponseByClaimIdAsync(string claimId)
    {
        try
  {
       var response = await _responseRepository.GetByClaimIdAsync(claimId);
   return response != null ? LegacyDtoMapper.ToDto(response) : null;
  }
        catch (Exception ex)
   {
  throw new InvalidOperationException("Failed to retrieve response by claim ID", ex);
        }
    }

    /// <summary>
    /// Get response for a claim (alias method)
    /// </summary>
    public async Task<ClaimResponseDto?> GetResponseForClaimAsync(string claimId)
    {
        return await GetResponseByClaimIdAsync(claimId);
    }

    /// <summary>
    /// Get responses by status
   /// </summary>
    public async Task<IEnumerable<ClaimResponseDto>> GetResponsesByStatusAsync(string status)
 {
        try
        {
      var responses = await _responseRepository.GetByStatusAsync(status);
    return LegacyDtoMapper.ToDtos(responses);
    }
        catch (Exception ex)
        {
    throw new InvalidOperationException("Failed to retrieve responses by status", ex);
}
    }

    /// <summary>
    /// Get responses with pre-auth reference
    /// </summary>
    public async Task<IEnumerable<ClaimResponseDto>> GetResponsesByPreAuthRefAsync(string preAuthRef)
    {
        try
        {
      var responses = await _responseRepository.GetByPreAuthRefAsync(preAuthRef);
     return LegacyDtoMapper.ToDtos(responses);
        }
       catch (Exception ex)
        {
   throw new InvalidOperationException("Failed to retrieve responses by pre-auth reference", ex);
   }
    }

    /// <summary>
  /// Update claim response
    /// </summary>
public async Task<ClaimResponseDto> UpdateClaimResponseAsync(string responseId, UpdateClaimResponseDto dto)
    {
     try
    {
   var response = await _responseRepository.GetByIdAsync(responseId);
         if (response == null)
     throw new KeyNotFoundException($"Claim response with ID {responseId} not found");

  throw ManualEditNotSupported();
        }
  catch (Exception ex)
        {
       throw new InvalidOperationException("Failed to update claim response", ex);
  }
    }

    /// <summary>
    /// No mapping for this DTO ever existed, so this operation has always failed. It stays unsupported rather than
    /// becoming a way around the RCM claim editor's edit lock, or a way to hand-enter the payer's adjudication.
    /// </summary>
    private static NotSupportedException ManualEditNotSupported() =>
        new("Creating or editing a claim response is not supported through this endpoint.");
}
