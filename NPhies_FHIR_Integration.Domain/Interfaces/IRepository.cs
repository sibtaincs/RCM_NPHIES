using System.Linq.Expressions;

namespace NPhies_FHIR_Integration.Domain.Interfaces;

/// <summary>
/// Generic repository interface for data access abstraction
/// </summary>
/// <typeparam name="T">Entity type that must be a class</typeparam>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Get all entities asynchronously
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync();

    /// <summary>
    /// Get all entities with includes asynchronously
    /// </summary>
    Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Get entity by ID asynchronously
    /// </summary>
    Task<T?> GetByIdAsync(string id);

    /// <summary>
    /// Get entity by ID with includes asynchronously
    /// </summary>
    Task<T?> GetByIdAsync(string id, params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Find entities by predicate asynchronously
    /// </summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Find entities by predicate with includes asynchronously
    /// </summary>
    Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Find single entity by predicate asynchronously
    /// </summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Find single entity by predicate with includes asynchronously
    /// </summary>
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes);

    /// <summary>
    /// Check if entity exists
    /// </summary>
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Get count of entities
    /// </summary>
    Task<int> CountAsync();

    /// <summary>
    /// Get count of entities matching predicate
    /// </summary>
    Task<int> CountAsync(Expression<Func<T, bool>> predicate);

    /// <summary>
    /// Add entity asynchronously
    /// </summary>
    Task<T> AddAsync(T entity);

    /// <summary>
    /// Add multiple entities asynchronously
    /// </summary>
    Task AddRangeAsync(IEnumerable<T> entities);

    /// <summary>
    /// Update entity
    /// </summary>
    T Update(T entity);

    /// <summary>
    /// Delete entity
    /// </summary>
    void Delete(T entity);

    /// <summary>
    /// Delete multiple entities
    /// </summary>
    void DeleteRange(IEnumerable<T> entities);

    /// <summary>
    /// Save changes asynchronously
    /// </summary>
    Task<int> SaveChangesAsync();
}
