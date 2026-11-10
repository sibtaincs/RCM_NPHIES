using NPhies_FHIR_Integration.Domain.DTOs;
using NPhies_FHIR_Integration.Domain.Interfaces;

namespace NPhies_FHIR_Integration.Application.Services;

/// <summary>
/// Generic service for handling CRUD operations
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TDto">DTO type</typeparam>
public class BaseService<TEntity, TDto> : IService<TEntity, TDto>
    where TEntity : class
    where TDto : class
{
    protected readonly IRepository<TEntity> Repository;

    public BaseService(IRepository<TEntity> repository)
    {
    Repository = repository;
    }

    public virtual async Task<TDto?> GetByIdAsync(string id)
    {
        var entity = await Repository.GetByIdAsync(id);
     return entity != null ? MapToDto(entity) : null;
    }

    public virtual async Task<IEnumerable<TDto>> GetAllAsync()
    {
        var entities = await Repository.GetAllAsync();
        return entities.Select(MapToDto).ToList();
    }

    public virtual async Task<TDto> CreateAsync(TDto dto)
    {
        var entity = MapToEntity(dto);
        var createdEntity = await Repository.AddAsync(entity);
   await Repository.SaveChangesAsync();
  return MapToDto(createdEntity);
    }

    public virtual async Task<TDto> UpdateAsync(string id, TDto dto)
    {
        var entity = MapToEntity(dto);
        // Set ID for update
        var reflectionProperty = entity.GetType().GetProperty("Id");
        if (reflectionProperty != null)
   {
   reflectionProperty.SetValue(entity, id);
        }

        var updatedEntity = Repository.Update(entity);
        await Repository.SaveChangesAsync();
        return MapToDto(updatedEntity);
    }

    public virtual async Task<bool> DeleteAsync(string id)
    {
        var entity = await Repository.GetByIdAsync(id);
  if (entity == null)
            return false;

     Repository.Delete(entity);
        await Repository.SaveChangesAsync();
        return true;
    }

    /// <summary>
    /// Override this method to map Entity to DTO
    /// </summary>
    protected virtual TDto MapToDto(TEntity entity)
    {
        throw new NotImplementedException("MapToDto must be implemented in derived class");
 }

    /// <summary>
 /// Override this method to map DTO to Entity
 /// </summary>
    protected virtual TEntity MapToEntity(TDto dto)
 {
      throw new NotImplementedException("MapToEntity must be implemented in derived class");
    }
}
