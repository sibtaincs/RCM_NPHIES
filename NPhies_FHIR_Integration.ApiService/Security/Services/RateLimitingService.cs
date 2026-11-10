using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.ApiService.Security.Services;

/// <summary>
/// Rate limiting service interface
/// </summary>
public interface IRateLimitingService
{
    Task<bool> IsRateLimitedAsync(string identifier, string endpoint, int maxRequests = 100, int windowMinutes = 1);
    Task RecordRequestAsync(string? userId, string ipAddress, string endpoint, string httpMethod);
    Task<RateLimitStatus> GetRateLimitStatusAsync(string identifier, string endpoint);
}

/// <summary>
/// Rate limiting service implementation
/// </summary>
public class RateLimitingService : IRateLimitingService
{
  private readonly ApplicationDbContext _dbContext;
 private readonly ILogger<RateLimitingService> _logger;

    public RateLimitingService(
        ApplicationDbContext dbContext,
 ILogger<RateLimitingService> logger)
    {
        _dbContext = dbContext;
    _logger = logger;
    }

    /// <summary>
  /// Check if request is rate limited
    /// </summary>
    public async Task<bool> IsRateLimitedAsync(string identifier, string endpoint, int maxRequests = 100, int windowMinutes = 1)
    {
        try
        {
    var windowStart = DateTime.UtcNow.AddMinutes(-windowMinutes);

     var recentRequests = await _dbContext.RateLimitLogs
      .Where(r => r.IpAddress == identifier
  && r.Endpoint == endpoint
  && r.CreatedAt >= windowStart)
 .CountAsync();

    var isRateLimited = recentRequests >= maxRequests;

    if (isRateLimited)
     {
           _logger.LogWarning(
 "Rate limit exceeded for {Identifier} on {Endpoint}: {RequestCount}/{MaxRequests}",
    identifier, endpoint, recentRequests, maxRequests);
           }

     return isRateLimited;
        }
      catch (Exception ex)
    {
           _logger.LogError(ex, "Error checking rate limit");
  return false;
   }
    }

    /// <summary>
    /// Record a request for rate limiting
    /// </summary>
    public async Task RecordRequestAsync(string? userId, string ipAddress, string endpoint, string httpMethod)
    {
        try
        {
 var windowStart = DateTime.UtcNow.AddMinutes(-1);
        var windowEnd = DateTime.UtcNow;

var recentRequestCount = await _dbContext.RateLimitLogs
     .Where(r => r.IpAddress == ipAddress
 && r.Endpoint == endpoint
  && r.CreatedAt >= windowStart)
     .CountAsync();

 var rateLimitLog = new Domain.Entities.ApiRateLimitLog
            {
       Id = Guid.NewGuid().ToString(),
      UserId = userId,
     IpAddress = ipAddress,
      Endpoint = endpoint,
       HttpMethod = httpMethod,
   RequestCount = recentRequestCount + 1,
     MaxRequests = 100, // Default limit
       WindowStart = windowStart,
     WindowEnd = windowEnd,
     IsRateLimited = (recentRequestCount + 1) >= 100,
 CreatedAt = DateTime.UtcNow
          };

     _dbContext.RateLimitLogs.Add(rateLimitLog);
       await _dbContext.SaveChangesAsync();
}
    catch (Exception ex)
        {
       _logger.LogError(ex, "Error recording rate limit");
        }
    }

  /// <summary>
    /// Get rate limit status
    /// </summary>
    public async Task<RateLimitStatus> GetRateLimitStatusAsync(string identifier, string endpoint)
    {
    try
      {
       var windowStart = DateTime.UtcNow.AddMinutes(-1);

   var recentLogs = await _dbContext.RateLimitLogs
  .Where(r => r.IpAddress == identifier
    && r.Endpoint == endpoint
     && r.CreatedAt >= windowStart)
            .OrderByDescending(r => r.CreatedAt)
      .FirstOrDefaultAsync();

if (recentLogs == null)
      {
     return new RateLimitStatus
      {
          IsRateLimited = false,
   RequestCount = 0,
    MaxRequests = 100,
     RemainingRequests = 100,
  ResetTime = DateTime.UtcNow.AddMinutes(1)
     };
            }

           return new RateLimitStatus
     {
       IsRateLimited = recentLogs.IsRateLimited,
      RequestCount = recentLogs.RequestCount,
     MaxRequests = recentLogs.MaxRequests,
          RemainingRequests = Math.Max(0, recentLogs.MaxRequests - recentLogs.RequestCount),
           ResetTime = recentLogs.WindowEnd
    };
        }
   catch (Exception ex)
  {
         _logger.LogError(ex, "Error getting rate limit status");
 return new RateLimitStatus { IsRateLimited = false, MaxRequests = 100 };
       }
   }
}

/// <summary>
/// Rate limit status DTO
/// </summary>
public class RateLimitStatus
{
    public bool IsRateLimited { get; set; }
    public int RequestCount { get; set; }
    public int MaxRequests { get; set; } = 100;
    public int RemainingRequests { get; set; }
    public DateTime ResetTime { get; set; }
}
