using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Application.Services.RCM;

namespace NPhies_FHIR_Integration.Tests.RCM;

/// <summary>
/// Unit tests for DenialManagementService
/// </summary>
public class DenialManagementServiceTests
{
    private readonly Mock<ILogger<DenialManagementService>> _mockLogger;
    private readonly DenialManagementService _service;

    public DenialManagementServiceTests()
    {
        _mockLogger = new Mock<ILogger<DenialManagementService>>();
        _service = new DenialManagementService(_mockLogger.Object);
    }

    [Fact]
    public async Task GetDenialsAsync_WithValidFilter_ReturnsDenials()
    {
  // Arrange
  var filter = new DenialFilter { PageNumber = 1, PageSize = 50 };

        // Act
        var result = await _service.GetDenialsAsync(filter);

      // Assert
        Assert.NotNull(result);
        Assert.IsType<List<DenialDetail>>(result);
    }

  [Fact]
    public async Task GetDenialsAsync_WithProviderId_FiltersByProvider()
    {
        // Arrange
     var filter = new DenialFilter { ProviderId = "PROV-001", PageSize = 50 };

        // Act
        var result = await _service.GetDenialsAsync(filter);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.All(d => d.ProviderId == "PROV-001"));
    }

    [Fact]
    public async Task CategorizeDenialsAsync_WithDenials_ReturnsCategorization()
    {
        // Arrange
        var denials = new List<DenialDetail>
        {
         new DenialDetail { DenialReasonCode = "NOT_COVERED", DeniedAmount = 150m },
     new DenialDetail { DenialReasonCode = "AUTH_REQUIRED", DeniedAmount = 180m },
            new DenialDetail { DenialReasonCode = "NOT_COVERED", DeniedAmount = 100m }
        };

        // Act
        var result = await _service.CategorizeDenialsAsync(denials);

  // Assert
        Assert.NotNull(result);
   Assert.Equal(3, result.TotalDenials);
        Assert.True(result.NonCoveredServiceCount > 0);
        Assert.True(result.AuthorizationCount > 0);
    }

    [Fact]
    public async Task GenerateDenialReportAsync_WithDateRange_GeneratesReport()
    {
        // Arrange
        var fromDate = DateTime.UtcNow.AddMonths(-1);
   var toDate = DateTime.UtcNow;

     // Act
    var result = await _service.GenerateDenialReportAsync(fromDate, toDate);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalDenials > 0);
     Assert.True(result.TotalDeniedAmount > 0);
    }

  [Fact]
    public async Task GenerateDenialReportAsync_WithInvalidDates_ThrowsException()
    {
     // Arrange
        var fromDate = DateTime.UtcNow;
        var toDate = DateTime.UtcNow.AddDays(-1);

      // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _service.GenerateDenialReportAsync(fromDate, toDate));
    }

    [Fact]
    public async Task GetHighValueDenialsAsync_WithThreshold_ReturnsHighValueDenials()
    {
     // Arrange
        var threshold = 100m;

        // Act
        var result = await _service.GetHighValueDenialsAsync(threshold);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.All(d => d.DeniedAmount >= threshold));
    }

    [Fact]
    public async Task CalculateDenialMetricsAsync_WithProviderId_CalculatesMetrics()
    {
        // Arrange
        var providerId = "PROV-001";
var fromDate = DateTime.UtcNow.AddMonths(-1);
        var toDate = DateTime.UtcNow;

        // Act
    var result = await _service.CalculateDenialMetricsAsync(providerId, fromDate, toDate);

      // Assert
        Assert.NotNull(result);
  Assert.Equal(providerId, result.ProviderId);
        Assert.True(result.DenialRate >= 0);
        Assert.True(result.TotalDeniedAmount >= 0);
    }

    [Fact]
    public async Task CalculateDenialMetricsAsync_WithInvalidDates_ThrowsException()
    {
        // Arrange
        var fromDate = DateTime.UtcNow;
        var toDate = DateTime.UtcNow.AddDays(-1);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() =>
          _service.CalculateDenialMetricsAsync("PROV-001", fromDate, toDate));
    }

    [Fact]
    public async Task BulkResubmitDeniedClaimsAsync_WithValidClaimIds_ReturnsResult()
    {
        // Arrange
        var claimIds = new List<int> { 1, 2, 3, 4, 5 };

        // Act
        var result = await _service.BulkResubmitDeniedClaimsAsync(claimIds);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(claimIds.Count, result.TotalProcessed);
        Assert.True(result.SuccessCount + result.FailureCount == result.TotalProcessed);
    }

    [Fact]
    public async Task BulkResubmitDeniedClaimsAsync_WithEmptyList_ThrowsException()
    {
        // Arrange
        var claimIds = new List<int>();

        // Act & Assert
     await Assert.ThrowsAsync<ArgumentException>(() =>
       _service.BulkResubmitDeniedClaimsAsync(claimIds));
    }

    [Fact]
    public async Task BulkResubmitDeniedClaimsAsync_IncludesFailedClaims()
    {
        // Arrange
  var claimIds = new List<int> { 1, 2, 3, 4, 5, 10, 20, 30 };

        // Act
        var result = await _service.BulkResubmitDeniedClaimsAsync(claimIds);

   // Assert
        Assert.True(result.FailureCount > 0 || result.SuccessCount > 0);
        Assert.True(result.FailedClaimIds.Count == result.FailureCount);
    }
}
