using AutoMapper;
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
 private readonly IMapper _mapper;

    /// <summary>
    /// Constructor with dependencies
    /// </summary>
    public ClaimResponseService(IClaimResponseRepository responseRepository, IMapper mapper)
    {
 _responseRepository = responseRepository ?? throw new ArgumentNullException(nameof(responseRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
  }

    /// <summary>
 /// Create a new claim response
    /// </summary>
  public async Task<ClaimResponseDto> CreateClaimResponseAsync(CreateClaimResponseDto dto)
    {
   try
   {
    var response = _mapper.Map<ClaimResponse>(dto);
      response.CreatedAt = DateTime.UtcNow;

  await _responseRepository.AddAsync(response);
         await _responseRepository.SaveChangesAsync();

            return _mapper.Map<ClaimResponseDto>(response);
}
        catch (Exception ex)
      {
       throw new InvalidOperationException("Failed to create claim response", ex);
        }
  }

    /// <summary>
    /// Get claim response by ID
   /// </summary>
    public async Task<ClaimResponseDto?> GetClaimResponseAsync(string responseId)
    {
        try
        {
      var response = await _responseRepository.GetWithDetailsAsync(responseId);
   return response != null ? _mapper.Map<ClaimResponseDto>(response) : null;
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
   return response != null ? _mapper.Map<ClaimResponseDto>(response) : null;
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
    return _mapper.Map<IEnumerable<ClaimResponseDto>>(responses);
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
     return _mapper.Map<IEnumerable<ClaimResponseDto>>(responses);
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

  _mapper.Map(dto, response);
        response.UpdatedAt = DateTime.UtcNow;

      _responseRepository.Update(response);
            await _responseRepository.SaveChangesAsync();

            return _mapper.Map<ClaimResponseDto>(response);
        }
  catch (Exception ex)
        {
       throw new InvalidOperationException("Failed to update claim response", ex);
  }
    }
}
