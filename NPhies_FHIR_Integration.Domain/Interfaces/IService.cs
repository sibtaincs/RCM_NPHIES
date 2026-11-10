namespace NPhies_FHIR_Integration.Domain.Interfaces;

/// <summary>
/// Generic service interface for business logic abstraction
/// </summary>
/// <typeparam name="TEntity">Entity type</typeparam>
/// <typeparam name="TDto">DTO type</typeparam>
public interface IService<TEntity, TDto> where TEntity : class where TDto : class
{
    Task<TDto?> GetByIdAsync(string id);
    Task<IEnumerable<TDto>> GetAllAsync();
    Task<TDto> CreateAsync(TDto dto);
    Task<TDto> UpdateAsync(string id, TDto dto);
    Task<bool> DeleteAsync(string id);
}
