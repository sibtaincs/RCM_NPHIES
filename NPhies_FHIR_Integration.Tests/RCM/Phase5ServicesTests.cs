using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Application.Services.RCM;

namespace NPhies_FHIR_Integration.Tests.RCM;

/// <summary>
/// Unit tests for PerformanceOptimizationService
/// </summary>
public class PerformanceOptimizationServiceTests
{
    private readonly Mock<ILogger<PerformanceOptimizationService>> _mockLogger;
    private readonly PerformanceOptimizationService _service;

    public PerformanceOptimizationServiceTests()
    {
        _mockLogger = new Mock<ILogger<PerformanceOptimizationService>>();
_service = new PerformanceOptimizationService(_mockLogger.Object);
    }

  [Fact]
    public async Task InitializeCachingAsync_InitializeSuccessfully()
    {
        // Act
        var result = await _service.InitializeCachingAsync();

      // Assert
Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
        Assert.True(result.CachingStrategiesConfigured > 0);
        Assert.NotEmpty(result.StrategiesEnabled);
    }

    [Fact]
    public async Task GetCachedMetricsAsync_FirstCall_ReturnsCacheMiss()
    {
      // Arrange
        var cacheKey = "test_metrics_001";

        // Act
        var result = await _service.GetCachedMetricsAsync(cacheKey, 5);

        // Assert
        Assert.NotNull(result);
    Assert.False(result.FromCache);
        Assert.Equal(cacheKey, result.CacheKey);
        Assert.NotEmpty(result.Data);
    }

    [Fact]
    public async Task GetCachedMetricsAsync_SecondCall_ReturnsCacheHit()
    {
        // Arrange
        var cacheKey = "test_metrics_002";
        
        // First call - populates cache
   await _service.GetCachedMetricsAsync(cacheKey, 5);

        // Act - Second call should hit cache
        var result = await _service.GetCachedMetricsAsync(cacheKey, 5);

    // Assert
        Assert.NotNull(result);
        Assert.True(result.FromCache);
  Assert.Equal(cacheKey, result.CacheKey);
    }

    [Fact]
    public async Task OptimizeQueryAsync_WithValidQueryType_ReturnsOptimization()
  {
  // Arrange
    var queryType = "dashboard_metrics";

 // Act
        var result = await _service.OptimizeQueryAsync(queryType);

        // Assert
Assert.NotNull(result);
        Assert.Equal(queryType, result.QueryType);
  Assert.True(result.ImprovementPercentage > 0);
        Assert.NotEmpty(result.Recommendations);
    }

    [Fact]
    public async Task OptimizeQueryAsync_HasPerformanceGain()
    {
   // Arrange
     var queryType = "payment_reconciliation";

        // Act
        var result = await _service.OptimizeQueryAsync(queryType);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.OptimizedExecutionTimeMs < result.OriginalExecutionTimeMs);
        Assert.True(result.ImprovementPercentage >= 30);
    }

    [Fact]
    public async Task MonitorPerformanceAsync_ReturnsMetricsSnapshot()
    {
        // Act
        var result = await _service.MonitorPerformanceAsync();

        // Assert
        Assert.NotNull(result);
        Assert.True(result.AverageResponseTimeMs > 0);
        Assert.True(result.CacheHitRatePercentage >= 0 && result.CacheHitRatePercentage <= 100);
Assert.True(result.ErrorRatePercentage >= 0);
    }

    [Fact]
    public async Task GeneratePerformanceReportAsync_WithValidDates_ReturnsReport()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddMonths(-1);
  var toDate = DateTime.UtcNow;

        // Act
 var result = await _service.GeneratePerformanceReportAsync(fromDate, toDate);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ReportId);
        Assert.True(result.PerformanceScore >= 0 && result.PerformanceScore <= 100);
        Assert.NotEmpty(result.OptimizationRecommendations);
    }

    [Fact]
    public async Task ClearCacheAsync_ClearsAllCache()
  {
        // Arrange
        await _service.GetCachedMetricsAsync("test_cache_001", 5);

        // Act
        var result = await _service.ClearCacheAsync();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task ClearCacheAsync_ClearsSpecificCacheKey()
    {
  // Arrange
        var cacheKey = "specific_cache_key";
        await _service.GetCachedMetricsAsync(cacheKey, 5);

     // Act
        var result = await _service.ClearCacheAsync(cacheKey);

     // Assert
        Assert.True(result);
 }
}

/// <summary>
/// Unit tests for SecurityHardeningService
/// </summary>
public class SecurityHardeningServiceTests
{
    private readonly Mock<ILogger<SecurityHardeningService>> _mockLogger;
    private readonly SecurityHardeningService _service;

    public SecurityHardeningServiceTests()
    {
        _mockLogger = new Mock<ILogger<SecurityHardeningService>>();
        _service = new SecurityHardeningService(_mockLogger.Object);
    }

    [Fact]
    public async Task InitializeSecurityControlsAsync_InitializeSuccessfully()
    {
        // Act
        var result = await _service.InitializeSecurityControlsAsync();

   // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
        Assert.True(result.SecurityControlsInitialized > 0);
        Assert.NotEmpty(result.ControlsConfigured);
    }

    [Fact]
 public async Task EnforceRateLimitingAsync_AllowsRequestsWithinLimit()
    {
        // Arrange
        var clientId = "client_001";
      var maxRequests = 100;

        // Act
        var result = await _service.EnforceRateLimitingAsync(clientId, maxRequests);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsAllowed);
  Assert.True(result.CurrentRequestCount <= maxRequests);
    }

    [Fact]
    public async Task EnforceRateLimitingAsync_DeniesRequestsOverLimit()
    {
        // Arrange
        var clientId = "client_002";
        var maxRequests = 3; // Very low limit for testing

    // Act - Make more than max requests
        for (int i = 0; i < 4; i++)
        {
       var result = await _service.EnforceRateLimitingAsync(clientId, maxRequests);
          
          if (i < 3)
         {
Assert.True(result.IsAllowed);
            }
    else
{
                Assert.False(result.IsAllowed);
         }
        }
    }

    [Fact]
    public async Task ValidateInputSecurityAsync_AllowsValidInput()
    {
        // Arrange
        var input = "valid_claim_data";
        var inputType = "claim_number";

        // Act
        var result = await _service.ValidateInputSecurityAsync(input, inputType);

     // Assert
        Assert.NotNull(result);
        Assert.True(result.IsValid);
        Assert.Empty(result.SecurityViolations);
    }

    [Fact]
    public async Task ValidateInputSecurityAsync_DetectsSqlInjection()
    {
        // Arrange
        var input = "'; DROP TABLE claims;--";
        var inputType = "claim_filter";

   // Act
        var result = await _service.ValidateInputSecurityAsync(input, inputType);

      // Assert
        Assert.NotNull(result);
   Assert.False(result.IsValid);
        Assert.NotEmpty(result.SecurityViolations);
        Assert.Contains("SQL injection", result.SecurityViolations[0]);
    }

    [Fact]
    public async Task ValidateInputSecurityAsync_DetectsXSSAttack()
    {
        // Arrange
        var input = "<script>alert('xss')</script>";
        var inputType = "claim_notes";

        // Act
        var result = await _service.ValidateInputSecurityAsync(input, inputType);

        // Assert
  Assert.NotNull(result);
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.SecurityViolations);
        Assert.Contains("XSS", result.SecurityViolations[0]);
    }

    [Fact]
    public async Task ValidateInputSecurityAsync_SanitizesInput()
    {
        // Arrange
        var input = "<script>test</script>";

        // Act
  var result = await _service.ValidateInputSecurityAsync(input, "test");

        // Assert
        Assert.NotNull(result);
     Assert.NotEmpty(result.SanitizedInput);
   Assert.DoesNotContain("<script>", result.SanitizedInput);
    }

    [Fact]
    public async Task DetectSecurityThreatsAsync_DetectsBruteForceThreat()
    {
        // Arrange
        var eventData = new SecurityEventData
        {
       EventType = "failed_authentication",
            SourceIp = "192.168.1.100",
          UserId = "user@example.com"
    };

        // Act
     var result = await _service.DetectSecurityThreatsAsync(eventData);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.ThreatDetected);
        Assert.NotEmpty(result.ThreatType);
Assert.NotEmpty(result.RecommendedActions);
    }

    [Fact]
    public async Task DetectSecurityThreatsAsync_DetectsDataExfiltrationThreat()
    {
        // Arrange
        var eventData = new SecurityEventData
  {
     EventType = "data_export",
         SourceIp = "192.168.1.101",
UserId = "user@example.com"
    };

        // Act
        var result = await _service.DetectSecurityThreatsAsync(eventData);

        // Assert
        Assert.NotNull(result);
   Assert.True(result.ThreatDetected);
        Assert.Equal("data_exfiltration", result.ThreatType);
   Assert.Equal("Critical", result.ThreatLevel);
    }

    [Fact]
    public async Task GenerateSecurityAuditReportAsync_WithValidDates_ReturnsReport()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddMonths(-1);
        var toDate = DateTime.UtcNow;

   // Act
        var result = await _service.GenerateSecurityAuditReportAsync(fromDate, toDate);

   // Assert
        Assert.NotNull(result);
      Assert.NotEmpty(result.ReportId);
    Assert.True(result.SecurityScore >= 0 && result.SecurityScore <= 100);
   Assert.NotEmpty(result.Recommendations);
    }

    [Fact]
    public async Task EncryptSensitiveDataAsync_EncryptsSuccessfully()
    {
        // Arrange
        var dataToEncrypt = "sensitive_patient_data_123";

        // Act
        var result = await _service.EncryptSensitiveDataAsync(dataToEncrypt);

        // Assert
      Assert.NotNull(result);
Assert.True(result.IsSuccessful);
        Assert.NotEmpty(result.EncryptedData);
Assert.Equal("AES-256-CBC", result.EncryptionMethod);
        Assert.NotEmpty(result.KeyId);
    }

    [Fact]
    public async Task EncryptSensitiveDataAsync_WithEmptyData_ThrowsException()
    {
      // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
      _service.EncryptSensitiveDataAsync(string.Empty));
    }
}
