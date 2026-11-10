using AutoMapper;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Repositories;

namespace NPhies_FHIR_Integration.Application.Services;

/// <summary>
/// Service interface for ClaimDiagnosis operations
/// </summary>
public interface IClaimDiagnosisService
{
    /// <summary>
  /// Create a new claim diagnosis
  /// </summary>
    Task<ClaimDiagnosisDto> CreateClaimDiagnosisAsync(CreateClaimDiagnosisDto dto);

    /// <summary>
    /// Get claim diagnosis by ID
 /// </summary>
  Task<ClaimDiagnosisDto?> GetClaimDiagnosisAsync(string diagnosisId);

    /// <summary>
    /// Get all diagnoses for a claim
  /// </summary>
    Task<IEnumerable<ClaimDiagnosisDto>> GetClaimDiagnosesAsync(string claimId);

    /// <summary>
  /// Get diagnoses by code
    /// </summary>
    Task<IEnumerable<ClaimDiagnosisDto>> GetDiagnosesByCodeAsync(string diagnosisCode);

    /// <summary>
    /// Update claim diagnosis
    /// </summary>
    Task<ClaimDiagnosisDto> UpdateClaimDiagnosisAsync(string diagnosisId, UpdateClaimDiagnosisDto dto);

    /// <summary>
 /// Delete claim diagnosis
    /// </summary>
    Task<bool> DeleteClaimDiagnosisAsync(string diagnosisId);
}

/// <summary>
/// Implementation of IClaimDiagnosisService
/// </summary>
public class ClaimDiagnosisService : IClaimDiagnosisService
{
    private readonly IClaimDiagnosisRepository _diagnosisRepository;
  private readonly IMapper _mapper;

    /// <summary>
    /// Constructor with dependencies
    /// </summary>
    public ClaimDiagnosisService(IClaimDiagnosisRepository diagnosisRepository, IMapper mapper)
    {
 _diagnosisRepository = diagnosisRepository ?? throw new ArgumentNullException(nameof(diagnosisRepository));
  _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// Create a new claim diagnosis
    /// </summary>
    public async Task<ClaimDiagnosisDto> CreateClaimDiagnosisAsync(CreateClaimDiagnosisDto dto)
    {
        try
        {
    var diagnosis = _mapper.Map<ClaimDiagnosis>(dto);
   diagnosis.CreatedAt = DateTime.UtcNow;

            await _diagnosisRepository.AddAsync(diagnosis);
   await _diagnosisRepository.SaveChangesAsync();

        return _mapper.Map<ClaimDiagnosisDto>(diagnosis);
   }
        catch (Exception ex)
 {
            throw new InvalidOperationException("Failed to create claim diagnosis", ex);
   }
    }

    /// <summary>
    /// Get claim diagnosis by ID
    /// </summary>
    public async Task<ClaimDiagnosisDto?> GetClaimDiagnosisAsync(string diagnosisId)
    {
       try
        {
    var diagnosis = await _diagnosisRepository.GetByIdAsync(diagnosisId);
      return diagnosis != null ? _mapper.Map<ClaimDiagnosisDto>(diagnosis) : null;
        }
        catch (Exception ex)
   {
         throw new InvalidOperationException("Failed to retrieve claim diagnosis", ex);
        }
    }

    /// <summary>
    /// Get all diagnoses for a claim
    /// </summary>
    public async Task<IEnumerable<ClaimDiagnosisDto>> GetClaimDiagnosesAsync(string claimId)
    {
        try
  {
 var diagnoses = await _diagnosisRepository.GetByClaimIdAsync(claimId);
    return _mapper.Map<IEnumerable<ClaimDiagnosisDto>>(diagnoses);
        }
        catch (Exception ex)
    {
     throw new InvalidOperationException("Failed to retrieve claim diagnoses", ex);
        }
    }

    /// <summary>
    /// Get diagnoses by code
    /// </summary>
    public async Task<IEnumerable<ClaimDiagnosisDto>> GetDiagnosesByCodeAsync(string diagnosisCode)
    {
        try
      {
     var diagnoses = await _diagnosisRepository.GetByCodeAsync(diagnosisCode);
   return _mapper.Map<IEnumerable<ClaimDiagnosisDto>>(diagnoses);
  }
        catch (Exception ex)
   {
 throw new InvalidOperationException("Failed to retrieve diagnoses by code", ex);
        }
    }

    /// <summary>
    /// Update claim diagnosis
    /// </summary>
    public async Task<ClaimDiagnosisDto> UpdateClaimDiagnosisAsync(string diagnosisId, UpdateClaimDiagnosisDto dto)
    {
  try
        {
  var diagnosis = await _diagnosisRepository.GetByIdAsync(diagnosisId);
   if (diagnosis == null)
         throw new KeyNotFoundException($"Claim diagnosis with ID {diagnosisId} not found");

  _mapper.Map(dto, diagnosis);
    diagnosis.UpdatedAt = DateTime.UtcNow;

        _diagnosisRepository.Update(diagnosis);
   await _diagnosisRepository.SaveChangesAsync();

          return _mapper.Map<ClaimDiagnosisDto>(diagnosis);
 }
        catch (Exception ex)
  {
          throw new InvalidOperationException("Failed to update claim diagnosis", ex);
        }
  }

    /// <summary>
    /// Delete claim diagnosis
    /// </summary>
    public async Task<bool> DeleteClaimDiagnosisAsync(string diagnosisId)
    {
 try
        {
            var diagnosis = await _diagnosisRepository.GetByIdAsync(diagnosisId);
      if (diagnosis == null)
       return false;

   _diagnosisRepository.Delete(diagnosis);
     await _diagnosisRepository.SaveChangesAsync();

        return true;
        }
  catch (Exception ex)
        {
          throw new InvalidOperationException("Failed to delete claim diagnosis", ex);
   }
    }
}
