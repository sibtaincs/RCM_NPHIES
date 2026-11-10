using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Services;

/// <summary>
/// Generic base service for master data CRUD operations
/// </summary>
public abstract class MasterDataServiceBase<TEntity, TDto> : IMasterDataService<TEntity, TDto>
    where TEntity : class
    where TDto : class
{
    protected readonly ApplicationDbContext _context;
    protected readonly IMapper _mapper;
 protected readonly ILogger<MasterDataServiceBase<TEntity, TDto>> _logger;

    protected MasterDataServiceBase(
        ApplicationDbContext context,
        IMapper mapper,
        ILogger<MasterDataServiceBase<TEntity, TDto>> logger)
    {
        _context = context;
        _mapper = mapper;
        _logger = logger;
    }

    /// <summary>
    /// Get DbSet for the entity type
    /// </summary>
    protected abstract DbSet<TEntity> GetDbSet();

    /// <summary>
    /// Get all records with pagination and filtering
    /// </summary>
    public virtual async Task<(List<TDto> items, int total)> GetAllAsync(int skip = 0, int take = 10, string? searchTerm = null)
    {
        try
        {
      var query = GetDbSet().AsNoTracking().AsQueryable();

    // Apply search if provided
 if (!string.IsNullOrWhiteSpace(searchTerm))
{
query = ApplySearchFilter(query, searchTerm);
      }

var total = await query.CountAsync();

   // Skip/Take is only reliable on an ordered query in SQL Server.
   var items = await query
       .Skip(Math.Max(skip, 0))
      .Take(take <= 0 ? 10 : take)
     .ToListAsync();

     var dtos = _mapper.Map<List<TDto>>(items);
     return (dtos, total);
      }
        catch (Exception ex)
        {
        _logger.LogError(ex,
     "Error retrieving {Entity} records (skip={Skip}, take={Take}, search='{Search}')",
      typeof(TEntity).Name, skip, take, searchTerm);
     throw;
        }
    }

    /// <summary>
    /// Get a single record by ID
    /// </summary>
    public virtual async Task<TDto?> GetByIdAsync(string id)
    {
        try
        {
            var entity = await GetDbSet().FindAsync(id);
        return entity == null ? null : _mapper.Map<TDto>(entity);
        }
  catch (Exception ex)
     {
          _logger.LogError(ex, "Error retrieving record with ID: {Id}", id);
            throw;
        }
    }

  /// <summary>
    /// Create a new record
    /// </summary>
    public virtual async Task<TDto> CreateAsync(TDto dto, string? createdBy = null)
    {
        try
        {
        var entity = _mapper.Map<TEntity>(dto);

            // Set audit fields if entity supports them
if (entity is IHasAuditFields auditEntity)
            {
    auditEntity.CreatedBy = createdBy ?? "System";
            }

GetDbSet().Add(entity);
            await _context.SaveChangesAsync();

      _logger.LogInformation("Record created successfully");
     return _mapper.Map<TDto>(entity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating record");
    throw;
        }
    }

    /// <summary>
    /// Update an existing record
    /// </summary>
    public virtual async Task<TDto> UpdateAsync(string id, TDto dto, string? modifiedBy = null)
    {
        try
  {
        var entity = await GetDbSet().FindAsync(id);
    if (entity == null)
    throw new KeyNotFoundException($"Record with ID {id} not found");

 _mapper.Map(dto, entity);

            // Set audit fields if entity supports them
            if (entity is IHasAuditFields auditEntity)
 {
          auditEntity.ModifiedBy = modifiedBy ?? "System";
      }

  await _context.SaveChangesAsync();

     _logger.LogInformation("Record updated successfully: {Id}", id);
      return _mapper.Map<TDto>(entity);
 }
     catch (Exception ex)
  {
       _logger.LogError(ex, "Error updating record with ID: {Id}", id);
            throw;
     }
    }

    /// <summary>
    /// Delete a record
    /// </summary>
    public virtual async Task DeleteAsync(string id)
    {
        try
        {
            var entity = await GetDbSet().FindAsync(id);
            if (entity == null)
          throw new KeyNotFoundException($"Record with ID {id} not found");

     GetDbSet().Remove(entity);
            await _context.SaveChangesAsync();

          _logger.LogInformation("Record deleted successfully: {Id}", id);
        }
        catch (Exception ex)
        {
  _logger.LogError(ex, "Error deleting record with ID: {Id}", id);
      throw;
     }
    }

    /// <summary>
    /// Check if a record exists
    /// </summary>
    public virtual async Task<bool> ExistsAsync(string id)
  {
        try
        {
    return await GetDbSet().FindAsync(id) != null;
        }
        catch (Exception ex)
        {
  _logger.LogError(ex, "Error checking if record exists: {Id}", id);
            throw;
        }
    }

    /// <summary>
 /// Search records by criteria
 /// </summary>
    public virtual async Task<List<TDto>> SearchAsync(string criteria)
    {
        try
      {
          var query = GetDbSet();
      var filtered = ApplySearchFilter(query, criteria);
            var items = await filtered.ToListAsync();
          return _mapper.Map<List<TDto>>(items);
        }
        catch (Exception ex)
        {
        _logger.LogError(ex, "Error searching records with criteria: {Criteria}", criteria);
   throw;
        }
    }

    /// <summary>
    /// Bulk create records
    /// </summary>
    public virtual async Task<List<TDto>> BulkCreateAsync(List<TDto> dtos, string? createdBy = null)
    {
        try
     {
      var entities = _mapper.Map<List<TEntity>>(dtos);

          foreach (var entity in entities)
            {
         if (entity is IHasAuditFields auditEntity)
    {
                    auditEntity.CreatedBy = createdBy ?? "System";
        }
   }

       GetDbSet().AddRange(entities);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Bulk created {Count} records", entities.Count);
return _mapper.Map<List<TDto>>(entities);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error bulk creating records");
            throw;
        }
    }

    /// <summary>
    /// Activate or deactivate a record
    /// </summary>
    public virtual async Task<TDto> ToggleActiveAsync(string id, bool isActive)
    {
try
      {
     var entity = await GetDbSet().FindAsync(id);
     if (entity == null)
          throw new KeyNotFoundException($"Record with ID {id} not found");

            if (entity is IHasActiveStatus activeEntity)
            {
       activeEntity.IsActive = isActive;
  }

            await _context.SaveChangesAsync();

 _logger.LogInformation("Record active status toggled: {Id}, IsActive: {IsActive}", id, isActive);
     return _mapper.Map<TDto>(entity);
   }
      catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling active status for ID: {Id}", id);
   throw;
  }
    }

    /// <summary>
    /// Apply search filter - override in derived classes for specific search logic
    /// </summary>
    protected virtual IQueryable<TEntity> ApplySearchFilter(IQueryable<TEntity> query, string searchTerm)
    {
  // Default implementation - can be overridden in derived classes
        return query;
    }
}

/// <summary>
/// Audit fields interface
/// </summary>
public interface IHasAuditFields
{
    string? CreatedBy { get; set; }
    string? ModifiedBy { get; set; }
}

/// <summary>
/// Active status interface
/// </summary>
public interface IHasActiveStatus
{
    bool IsActive { get; set; }
}
