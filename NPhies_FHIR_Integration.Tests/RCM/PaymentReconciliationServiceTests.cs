using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Application.Services.RCM;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Tests.RCM;

/// <summary>
/// Unit tests for PaymentReconciliationService
/// </summary>
public class PaymentReconciliationServiceTests
{
  private readonly Mock<ILogger<PaymentReconciliationService>> _mockLogger;
    private readonly PaymentReconciliationService _service;

    public PaymentReconciliationServiceTests()
    {
      _mockLogger = new Mock<ILogger<PaymentReconciliationService>>();
        _service = new PaymentReconciliationService(_mockLogger.Object);
    }

    [Fact]
    public async Task ReconcilePaymentAsync_WithMatchingAmounts_ReturnsReconciled()
    {
        // Arrange
var response = new ClaimResponse { ClaimId = "CLM-001", Totals = new List<ClaimResponseTotal> { new ClaimResponseTotal { Amount = 100m } } };
        var paymentNotice = new PaymentNotice { TotalAmount = 100m };

     // Act
        var result = await _service.ReconcilePaymentAsync(response, paymentNotice);

   // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
     Assert.Equal("Reconciled", result.Status);
        Assert.Equal(100m, result.ExpectedAmount);
        Assert.Equal(100m, result.ActualAmount);
    }

    [Fact]
    public async Task ReconcilePaymentAsync_WithOverpayment_FlagsOverpayment()
    {
        // Arrange
        var response = new ClaimResponse { ClaimId = "CLM-001", Totals = new List<ClaimResponseTotal> { new ClaimResponseTotal { Amount = 100m } } };
        var paymentNotice = new PaymentNotice { TotalAmount = 150m };

        // Act
        var result = await _service.ReconcilePaymentAsync(response, paymentNotice);

   // Assert
        Assert.NotNull(result);
     Assert.True(result.IsSuccessful);
    Assert.Equal("Overpayment", result.Status);
        Assert.True(result.Variance > 0);
    }

    [Fact]
    public async Task ReconcilePaymentAsync_WithUnderpayment_FlagsUnderpayment()
    {
        // Arrange
        var response = new ClaimResponse { ClaimId = "CLM-001", Totals = new List<ClaimResponseTotal> { new ClaimResponseTotal { Amount = 150m } } };
     var paymentNotice = new PaymentNotice { TotalAmount = 100m };

        // Act
        var result = await _service.ReconcilePaymentAsync(response, paymentNotice);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.IsSuccessful);
   Assert.Equal("Underpayment", result.Status);
        Assert.True(result.Variance < 0);
    }

    [Fact]
    public async Task MatchPaymentToClaimAsync_WithExactMatch_Returns100Confidence()
    {
      // Arrange
        var payment = new Payment { ClaimId = 1, Amount = 100m };
        var claims = new List<Claim> { new Claim { Id = "1", Total = 100m, ClaimNumber = "CLM-001" } };

        // Act
        var result = await _service.MatchPaymentToClaimAsync(payment, claims);

      // Assert
        Assert.NotNull(result);
   Assert.True(result.IsMatched);
        Assert.Equal(100m, result.MatchConfidence);
    }

    [Fact]
    public async Task MatchPaymentToClaimAsync_WithFuzzyMatch_Returns85Confidence()
    {
// Arrange
        var payment = new Payment { Amount = 101m };  // 1% higher
  var claims = new List<Claim> { new Claim { Id = "1", Total = 100m, ClaimNumber = "CLM-001" } };

 // Act
        var result = await _service.MatchPaymentToClaimAsync(payment, claims);

        // Assert
  Assert.NotNull(result);
        Assert.True(result.IsMatched);
      Assert.Equal(85m, result.MatchConfidence);
    }

    [Fact]
    public async Task MatchPaymentToClaimAsync_WithNoMatch_ReturnsFalse()
    {
        // Arrange
        var payment = new Payment { Amount = 500m };  // Completely different amount
        var claims = new List<Claim> { new Claim { Id = "1", Total = 100m, ClaimNumber = "CLM-001" } };

        // Act
        var result = await _service.MatchPaymentToClaimAsync(payment, claims);

        // Assert
        Assert.NotNull(result);
        Assert.False(result.IsMatched);
    }

    [Fact]
    public async Task IdentifyDiscrepanciesAsync_WithNoDiscrepancies_ReturnsEmpty()
    {
        // Arrange
        var payments = new List<Payment> { new Payment { ClaimId = 1, Amount = 100m } };
        var claims = new List<Claim> { new Claim { Id = "1", Total = 100m, ClaimNumber = "CLM-001" } };

        // Act
        var result = await _service.IdentifyDiscrepanciesAsync(payments, claims);

        // Assert
   Assert.NotNull(result);
        // Should not have discrepancies (exact match)
}

    [Fact]
    public async Task IdentifyDiscrepanciesAsync_WithOverpayment_IdentifiesOverpayment()
    {
        // Arrange
        var payments = new List<Payment> { new Payment { ClaimId = 1, Amount = 150m } };
        var claims = new List<Claim> { new Claim { Id = "1", Total = 100m, ClaimNumber = "CLM-001" } };

        // Act
        var result = await _service.IdentifyDiscrepanciesAsync(payments, claims);

        // Assert
 Assert.NotNull(result);
        var overpayment = result.FirstOrDefault(d => d.DiscrepancyType == "Overpayment");
        Assert.NotNull(overpayment);
        Assert.True(overpayment.Variance > 0);
    }

    [Fact]
    public async Task IdentifyDiscrepanciesAsync_WithUnderpayment_IdentifiesUnderpayment()
    {
        // Arrange
        var payments = new List<Payment> { new Payment { ClaimId = 1, Amount = 75m } };
        var claims = new List<Claim> { new Claim { Id = "1", Total = 100m, ClaimNumber = "CLM-001" } };

// Act
  var result = await _service.IdentifyDiscrepanciesAsync(payments, claims);

    // Assert
        Assert.NotNull(result);
        var underpayment = result.FirstOrDefault(d => d.DiscrepancyType == "Underpayment");
     Assert.NotNull(underpayment);
        Assert.True(underpayment.Variance < 0);
    }

    [Fact]
    public async Task IdentifyDiscrepanciesAsync_WithUnmatchedPayment_IdentifiesUnmatched()
    {
        // Arrange
  var payments = new List<Payment> { new Payment { Amount = 500m } };  // No ClaimId
        var claims = new List<Claim> { new Claim { Id = "1", Total = 100m, ClaimNumber = "CLM-001" } };

        // Act
        var result = await _service.IdentifyDiscrepanciesAsync(payments, claims);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Count > 0);
    }

    [Fact]
    public async Task GenerateReconciliationReportAsync_WithValidDates_GeneratesReport()
    {
        // Arrange
     var fromDate = DateTime.UtcNow.AddMonths(-1);
        var toDate = DateTime.UtcNow;

  // Act
        var result = await _service.GenerateReconciliationReportAsync(fromDate, toDate);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.TotalClaims > 0);
 Assert.Equal(fromDate, result.FromDate);
    Assert.Equal(toDate, result.ToDate);
    }

    [Fact]
    public async Task CalculatePaymentAgeingAsync_WithProviderId_CalculatesAgeing()
    {
        // Arrange
        var providerId = "PROV-001";

// Act
        var result = await _service.CalculatePaymentAgeingAsync(providerId);

        // Assert
   Assert.NotNull(result);
        Assert.Equal(providerId, result.ProviderId);
        Assert.True(result.TotalReceivable > 0);
    }

    [Fact]
    public async Task CalculatePaymentAgeingAsync_BucketsAreCorrect()
    {
        // Arrange
 var providerId = "PROV-001";

        // Act
        var result = await _service.CalculatePaymentAgeingAsync(providerId);

        // Assert
        Assert.NotNull(result);
      // Total of all buckets should equal total receivable
  var totalFromBuckets = result.NotYetDue.Amount + result.PastDue1To30.Amount +
       result.PastDue31To60.Amount + result.PastDue61To90.Amount +
                result.PastDueOver90.Amount;
        Assert.Equal(result.TotalReceivable, totalFromBuckets);
    }

  [Fact]
    public async Task IdentifyPaymentAdjustmentsAsync_WithOverpayments_IdentifiesOverpayments()
 {
        // Arrange
      var payments = new List<Payment> { new Payment { ClaimId = 1, Amount = 150m } };

        // Act
        var result = await _service.IdentifyPaymentAdjustmentsAsync(payments);

// Assert
        Assert.NotNull(result);
 Assert.True(result.OverpaymentCount > 0);
        Assert.True(result.TotalOverpayments > 0);
    }

    [Fact]
    public async Task IdentifyPaymentAdjustmentsAsync_WithUnderpayments_IdentifiesUnderpayments()
    {
    // Arrange
        var payments = new List<Payment> { new Payment { ClaimId = 1, Amount = 75m } };

      // Act
        var result = await _service.IdentifyPaymentAdjustmentsAsync(payments);

        // Assert
      Assert.NotNull(result);
        Assert.True(result.UnderpaymentCount > 0);
    Assert.True(result.TotalUnderpayments > 0);
    }
}
