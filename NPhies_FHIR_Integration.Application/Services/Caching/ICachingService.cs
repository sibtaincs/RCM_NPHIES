using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace NPhies_FHIR_Integration.Application.Services.Caching;

/// <summary>
/// Caching Service Interface
/// Provides distributed caching capabilities for performance optimization
/// </summary>
public interface ICachingService
{
    /// <summary>
    /// Get value from cache
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Set value in cache
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Remove value from cache
    /// </summary>
    Task RemoveAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Invalidate cache by pattern
    /// </summary>
    Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if key exists in cache
    /// </summary>
    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get or set value (cache-aside pattern)
    /// </summary>
    Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class;

    /// <summary>
    /// Clear all cache
    /// </summary>
    Task ClearAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Get cache statistics
    /// </summary>
    Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Caching Service Implementation
/// Uses distributed cache (Redis) for performance optimization
/// </summary>
public class CachingService : ICachingService
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<CachingService> _logger;
    private readonly Dictionary<string, DateTime> _keyIndex; // Track cached keys

    public CachingService(
    IDistributedCache cache,
        ILogger<CachingService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
 _logger = logger ?? throw new ArgumentNullException(nameof(logger));
  _keyIndex = new Dictionary<string, DateTime>();
    }

    /// <summary>
    /// Get value from cache
    /// </summary>
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var cachedValue = await _cache.GetStringAsync(key, cancellationToken);

            if (cachedValue == null)
      {
    _logger.LogDebug("Cache miss for key {Key}", key);
      return null;
        }

            var value = JsonSerializer.Deserialize<T>(cachedValue);
    _logger.LogDebug("Cache hit for key {Key}", key);
     return value;
        }
        catch (Exception ex)
    {
            _logger.LogError(ex, "Error retrieving from cache for key {Key}", key);
   return null;
        }
    }

    /// <summary>
    /// Set value in cache
    /// </summary>
    public async Task SetAsync<T>(string key, T value, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
            var serialized = JsonSerializer.Serialize(value);
            var options = new DistributedCacheEntryOptions();

         if (expiration.HasValue)
            {
    options.AbsoluteExpirationRelativeToNow = expiration.Value;
 }
            else
     {
              options.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1); // Default 1 hour
            }

   await _cache.SetStringAsync(key, serialized, options, cancellationToken);
            _keyIndex[key] = DateTime.UtcNow;

         _logger.LogDebug("Set cache for key {Key} with expiration {Expiration}", key, expiration);
   }
        catch (Exception ex)
    {
            _logger.LogError(ex, "Error setting cache for key {Key}", key);
        }
    }

  /// <summary>
    /// Remove value from cache
    /// </summary>
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
 try
        {
            await _cache.RemoveAsync(key, cancellationToken);
            _keyIndex.Remove(key);
_logger.LogDebug("Removed cache for key {Key}", key);
 }
        catch (Exception ex)
        {
     _logger.LogError(ex, "Error removing cache for key {Key}", key);
        }
    }

    /// <summary>
    /// Invalidate cache by pattern
    /// </summary>
    public async Task InvalidateByPatternAsync(string pattern, CancellationToken cancellationToken = default)
    {
        try
     {
  var matchingKeys = _keyIndex.Keys
      .Where(k => k.Contains(pattern, StringComparison.OrdinalIgnoreCase))
           .ToList();

            foreach (var key in matchingKeys)
            {
 await RemoveAsync(key, cancellationToken);
    }

            _logger.LogInformation("Invalidated {Count} cache entries matching pattern {Pattern}", matchingKeys.Count, pattern);
        }
        catch (Exception ex)
        {
      _logger.LogError(ex, "Error invalidating cache by pattern {Pattern}", pattern);
        }
    }

    /// <summary>
    /// Check if key exists in cache
    /// </summary>
    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
  try
   {
      var value = await _cache.GetAsync(key, cancellationToken);
 return value != null;
        }
        catch (Exception ex)
        {
          _logger.LogError(ex, "Error checking cache existence for key {Key}", key);
    return false;
 }
    }

    /// <summary>
    /// Get or set value (cache-aside pattern)
    /// </summary>
  public async Task<T?> GetOrSetAsync<T>(string key, Func<Task<T?>> factory, TimeSpan? expiration = null, CancellationToken cancellationToken = default) where T : class
    {
        try
        {
       // Try to get from cache
         var cachedValue = await GetAsync<T>(key, cancellationToken);
            if (cachedValue != null)
  {
             return cachedValue;
            }

 // Not in cache, get from factory
     var value = await factory();
     if (value != null)
            {
                await SetAsync(key, value, expiration, cancellationToken);
       }

 return value;
   }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in cache-aside pattern for key {Key}", key);
   return await factory();
        }
    }

 /// <summary>
    /// Clear all cache
    /// </summary>
    public async Task ClearAllAsync(CancellationToken cancellationToken = default)
    {
     try
        {
    var keys = _keyIndex.Keys.ToList();
            foreach (var key in keys)
            {
          await RemoveAsync(key, cancellationToken);
      }

       _logger.LogInformation("Cleared all cache ({Count} entries)", keys.Count);
  }
        catch (Exception ex)
    {
      _logger.LogError(ex, "Error clearing all cache");
     }
  }

    /// <summary>
    /// Get cache statistics
    /// </summary>
    public async Task<CacheStatistics> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
      return new CacheStatistics
        {
     TotalCachedItems = _keyIndex.Count,
            CachedKeys = _keyIndex.Keys.ToList(),
        OldestEntryAge = _keyIndex.Any() ? DateTime.UtcNow - _keyIndex.Values.Min() : TimeSpan.Zero,
 NewestEntryAge = _keyIndex.Any() ? DateTime.UtcNow - _keyIndex.Values.Max() : TimeSpan.Zero
      };
    }
}

/// <summary>
/// Cache statistics DTO
/// </summary>
public class CacheStatistics
{
    public int TotalCachedItems { get; set; }
    public List<string> CachedKeys { get; set; } = new();
    public TimeSpan OldestEntryAge { get; set; }
    public TimeSpan NewestEntryAge { get; set; }
}
