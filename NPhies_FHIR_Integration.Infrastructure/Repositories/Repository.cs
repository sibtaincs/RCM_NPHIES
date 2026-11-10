using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Domain.Interfaces;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Infrastructure.Repositories;

/// <summary>
/// Generic repository implementation for data access using Entity Framework Core
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly ApplicationDbContext Context;
    protected readonly DbSet<T> DbSet;

    /// <summary>
    /// Constructor
    /// </summary>
    public Repository(ApplicationDbContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        DbSet = context.Set<T>();
    }

    /// <summary>
    /// Get all entities asynchronously
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetAllAsync()
    {
        return await DbSet.AsNoTracking().ToListAsync();
    }

    /// <summary>
    /// Get all entities with includes asynchronously
    /// </summary>
    public virtual async Task<IEnumerable<T>> GetAllAsync(params Expression<Func<T, object>>[] includes)
    {
        var query = DbSet.AsNoTracking().AsQueryable();
        query = includes.Aggregate(query, (current, include) => current.Include(include));
        return await query.ToListAsync();
    }

    /// <summary>
    /// Get entity by ID asynchronously
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(string id)
    {
        return await DbSet.AsNoTracking().FirstOrDefaultAsync(e => e.Id == id);
    }

    /// <summary>
    /// Get entity by ID with includes asynchronously
    /// </summary>
    public virtual async Task<T?> GetByIdAsync(string id, params Expression<Func<T, object>>[] includes)
    {
        var query = DbSet.AsNoTracking().AsQueryable();
        query = includes.Aggregate(query, (current, include) => current.Include(include));
        return await query.FirstOrDefaultAsync(e => e.Id == id);
    }

    /// <summary>
    /// Find entities by predicate asynchronously
    /// </summary>
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.AsNoTracking().Where(predicate).ToListAsync();
    }

    /// <summary>
    /// Find entities by predicate with includes asynchronously
    /// </summary>
    public virtual async Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
    {
        var query = DbSet.AsNoTracking().AsQueryable();
        query = includes.Aggregate(query, (current, include) => current.Include(include));
        return await query.Where(predicate).ToListAsync();
    }

    /// <summary>
    /// Find single entity by predicate asynchronously
    /// </summary>
    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.AsNoTracking().FirstOrDefaultAsync(predicate);
    }

    /// <summary>
    /// Find single entity by predicate with includes asynchronously
    /// </summary>
    public virtual async Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate, params Expression<Func<T, object>>[] includes)
    {
        var query = DbSet.AsNoTracking().AsQueryable();
        query = includes.Aggregate(query, (current, include) => current.Include(include));
        return await query.FirstOrDefaultAsync(predicate);
    }

    /// <summary>
    /// Check if entity exists
    /// </summary>
    public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.AnyAsync(predicate);
    }

    /// <summary>
    /// Get count of entities
    /// </summary>
    public virtual async Task<int> CountAsync()
    {
        return await DbSet.CountAsync();
    }

    /// <summary>
    /// Get count of entities matching predicate
    /// </summary>
    public virtual async Task<int> CountAsync(Expression<Func<T, bool>> predicate)
    {
        return await DbSet.CountAsync(predicate);
    }

    /// <summary>
    /// Add entity asynchronously
    /// </summary>
    public virtual async Task<T> AddAsync(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await DbSet.AddAsync(entity);
        return entity;
    }

    /// <summary>
    /// Add multiple entities asynchronously
    /// </summary>
    public virtual async Task AddRangeAsync(IEnumerable<T> entities)
    {
        var enumerable = entities as T[] ?? entities.ToArray();
        if (enumerable.Length == 0)
            return;

        await DbSet.AddRangeAsync(enumerable);
    }

    /// <summary>
    /// Update entity
    /// </summary>
    public virtual T Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        DbSet.Update(entity);
        return entity;
    }

    /// <summary>
    /// Delete entity
    /// </summary>
    public virtual void Delete(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        DbSet.Remove(entity);
    }

    /// <summary>
    /// Delete multiple entities
    /// </summary>
    public virtual void DeleteRange(IEnumerable<T> entities)
    {
        var enumerable = entities as T[] ?? entities.ToArray();
        if (enumerable.Length > 0)
            DbSet.RemoveRange(enumerable);
    }

    /// <summary>
    /// Save changes asynchronously
    /// </summary>
    public virtual async Task<int> SaveChangesAsync()
    {
        return await Context.SaveChangesAsync();
    }
}
