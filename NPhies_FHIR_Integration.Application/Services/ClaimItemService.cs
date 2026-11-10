using AutoMapper;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Repositories;

namespace NPhies_FHIR_Integration.Application.Services;

/// <summary>
/// Service interface for ClaimItem operations
/// </summary>
public interface IClaimItemService
{
    /// <summary>
    /// Create a new claim item
    /// </summary>
    Task<ClaimItemDto> CreateClaimItemAsync(CreateClaimItemDto dto);

    /// <summary>
    /// Get claim item by ID
    /// </summary>
    Task<ClaimItemDto?> GetClaimItemAsync(string itemId);

    /// <summary>
    /// Get all items for a claim
    /// </summary>
    Task<IEnumerable<ClaimItemDto>> GetClaimItemsAsync(string claimId);

    /// <summary>
    /// Update claim item
    /// </summary>
    Task<ClaimItemDto> UpdateClaimItemAsync(string itemId, UpdateClaimItemDto dto);

    /// <summary>
    /// Delete claim item
    /// </summary>
    Task<bool> DeleteClaimItemAsync(string itemId);
}

/// <summary>
/// Implementation of IClaimItemService
/// </summary>
public class ClaimItemService : IClaimItemService
{
    private readonly IClaimItemRepository _itemRepository;
    private readonly IMapper _mapper;

    /// <summary>
    /// Constructor with dependencies
    /// </summary>
    public ClaimItemService(IClaimItemRepository itemRepository, IMapper mapper)
    {
        _itemRepository = itemRepository ?? throw new ArgumentNullException(nameof(itemRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    /// <summary>
    /// Create a new claim item
    /// </summary>
    public async Task<ClaimItemDto> CreateClaimItemAsync(CreateClaimItemDto dto)
    {
        try
        {
            var item = _mapper.Map<ClaimItem>(dto);
            item.CreatedAt = DateTime.UtcNow;

            await _itemRepository.AddAsync(item);
            await _itemRepository.SaveChangesAsync();

            return _mapper.Map<ClaimItemDto>(item);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to create claim item", ex);
        }
    }

    /// <summary>
    /// Get claim item by ID
    /// </summary>
    public async Task<ClaimItemDto?> GetClaimItemAsync(string itemId)
    {
        try
        {
            var item = await _itemRepository.GetByIdAsync(itemId);
            return item != null ? _mapper.Map<ClaimItemDto>(item) : null;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to retrieve claim item", ex);
        }
    }

    /// <summary>
    /// Get all items for a claim
    /// </summary>
    public async Task<IEnumerable<ClaimItemDto>> GetClaimItemsAsync(string claimId)
    {
        try
        {
            var items = await _itemRepository.GetByClaimIdAsync(claimId);
            return _mapper.Map<IEnumerable<ClaimItemDto>>(items);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to retrieve claim items", ex);
        }
    }

    /// <summary>
    /// Update claim item
    /// </summary>
    public async Task<ClaimItemDto> UpdateClaimItemAsync(string itemId, UpdateClaimItemDto dto)
    {
        try
        {
            var item = await _itemRepository.GetByIdAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException($"Claim item with ID {itemId} not found");

            _mapper.Map(dto, item);
            item.UpdatedAt = DateTime.UtcNow;

            _itemRepository.Update(item);
            await _itemRepository.SaveChangesAsync();

            return _mapper.Map<ClaimItemDto>(item);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to update claim item", ex);
        }
    }

    /// <summary>
    /// Delete claim item
    /// </summary>
    public async Task<bool> DeleteClaimItemAsync(string itemId)
    {
        try
        {
            var item = await _itemRepository.GetByIdAsync(itemId);
            if (item == null)
                return false;

            _itemRepository.Delete(item);
            await _itemRepository.SaveChangesAsync();

            return true;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to delete claim item", ex);
        }
    }
}
