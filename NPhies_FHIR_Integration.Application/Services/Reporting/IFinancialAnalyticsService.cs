using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.Reporting;

/// <summary>
/// Financial Analytics Service Interface
/// Provides financial metrics and performance analysis
/// </summary>
public interface IFinancialAnalyticsService
{
    // ========== CLAIM FINANCIAL ANALYTICS ==========
    /// <summary>
    /// Calculate total revenue by date range
    /// </summary>
    Task<TotalRevenueAnalysis> GetTotalRevenueAsync(
        DateTime startDate,
  DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate revenue breakdown by status
    /// </summary>
    Task<RevenueBreakdownByStatus> GetRevenueBreakdownByStatusAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate revenue trend over time
    /// </summary>
    Task<List<RevenueTrendPoint>> GetRevenueTrendAsync(
        DateTime startDate,
   DateTime endDate,
        int groupByDays = 1,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate average claim value
    /// </summary>
    Task<AverageClaimValueAnalysis> GetAverageClaimValueAsync(
        DateTime startDate,
        DateTime endDate,
     CancellationToken cancellationToken = default);

    // ========== PROVIDER FINANCIAL ANALYTICS ==========
    /// <summary>
    /// Calculate provider revenue and metrics
  /// </summary>
    Task<ProviderFinancialMetrics> GetProviderFinancialMetricsAsync(
 string providerId,
   DateTime startDate,
      DateTime endDate,
        CancellationToken cancellationToken = default);

 /// <summary>
    /// Get top providers by revenue
    /// </summary>
Task<List<ProviderRevenueRanking>> GetTopProvidersByRevenueAsync(
  DateTime startDate,
    DateTime endDate,
        int topCount = 10,
 CancellationToken cancellationToken = default);

/// <summary>
    /// Calculate provider efficiency metrics
    /// </summary>
    Task<List<ProviderEfficiencyMetrics>> GetProviderEfficiencyAsync(
        DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    // ========== INSURER FINANCIAL ANALYTICS ==========
    /// <summary>
    /// Calculate insurer financial metrics
    /// </summary>
    Task<InsurerFinancialMetrics> GetInsurerFinancialMetricsAsync(
        string insurerId,
        DateTime startDate,
        DateTime endDate,
    CancellationToken cancellationToken = default);

  /// <summary>
    /// Get top insurers by processed amount
    /// </summary>
    Task<List<InsurerProcessingRanking>> GetTopInsurersByProcessingAsync(
        DateTime startDate,
  DateTime endDate,
        int topCount = 10,
        CancellationToken cancellationToken = default);

    // ========== CLAIM PROCESSING COST ANALYTICS ==========
    /// <summary>
    /// Calculate cost per claim processed
    /// </summary>
    Task<CostPerClaimAnalysis> GetCostPerClaimAsync(
        DateTime startDate,
        DateTime endDate,
    CancellationToken cancellationToken = default);

    /// <summary>
  /// Calculate denial cost impact
    /// </summary>
    Task<DenialCostImpactAnalysis> GetDenialCostImpactAsync(
DateTime startDate,
        DateTime endDate,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate appeal cost impact
    /// </summary>
    Task<AppealCostImpactAnalysis> GetAppealCostImpactAsync(
        DateTime startDate,
     DateTime endDate,
        CancellationToken cancellationToken = default);

    // ========== FINANCIAL FORECASTING ==========
  /// <summary>
    /// Forecast revenue for next period
    /// </summary>
    Task<RevenueForecast> ForecastRevenueAsync(
        int forecastDays = 30,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculate revenue projections
    /// </summary>
    Task<List<RevenueProjection>> GetRevenueProjectionsAsync(
        DateTime baselineStart,
    DateTime baselineEnd,
   int projectionDays = 90,
 CancellationToken cancellationToken = default);
}

// ========== DATA MODELS ==========

/// <summary>
/// Total revenue analysis
/// </summary>
public class TotalRevenueAnalysis
{
    public decimal TotalSubmittedAmount { get; set; }
 public decimal TotalApprovedAmount { get; set; }
    public decimal TotalDeniedAmount { get; set; }
    public decimal TotalPartialAmount { get; set; }
    public double ApprovalPercentage { get; set; }
    public int TotalClaimsSubmitted { get; set; }
    public int TotalClaimsApproved { get; set; }
    public decimal AverageClaimAmount { get; set; }
}

/// <summary>
/// Revenue breakdown by status
/// </summary>
public class RevenueBreakdownByStatus
{
    public List<RevenueByStatusItem> Items { get; set; } = new();
    public decimal TotalAmount { get; set; }
}

public class RevenueByStatusItem
{
    public string Status { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public int ClaimCount { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Revenue trend point
/// </summary>
public class RevenueTrendPoint
{
    public DateTime Date { get; set; }
    public decimal DailyRevenue { get; set; }
    public decimal CumulativeRevenue { get; set; }
    public int ClaimCount { get; set; }
    public double RunningAveragePercentage { get; set; }
}

/// <summary>
/// Average claim value analysis
/// </summary>
public class AverageClaimValueAnalysis
{
    public decimal OverallAverage { get; set; }
    public decimal ApprovedAverage { get; set; }
    public decimal DeniedAverage { get; set; }
    public decimal PartialAverage { get; set; }
    public decimal MedianClaimValue { get; set; }
    public decimal MinClaimValue { get; set; }
    public decimal MaxClaimValue { get; set; }
    public decimal StandardDeviation { get; set; }
}

/// <summary>
/// Provider financial metrics
/// </summary>
public class ProviderFinancialMetrics
{
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public decimal TotalSubmittedAmount { get; set; }
    public decimal TotalApprovedAmount { get; set; }
    public int TotalClaimsSubmitted { get; set; }
    public int TotalClaimsApproved { get; set; }
 public double ApprovalRate { get; set; }
 public decimal AverageClaimValue { get; set; }
    public double AverageProcessingDays { get; set; }
    public int DenialCount { get; set; }
    public int AppealCount { get; set; }
    public double AppealSuccessRate { get; set; }
}

/// <summary>
/// Provider revenue ranking
/// </summary>
public class ProviderRevenueRanking
{
    public int Rank { get; set; }
    public string ProviderId { get; set; } = string.Empty;
  public string ProviderName { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
 public int ClaimCount { get; set; }
    public double PercentageOfTotal { get; set; }
}

/// <summary>
/// Provider efficiency metrics
/// </summary>
public class ProviderEfficiencyMetrics
{
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public double ApprovalRate { get; set; }
    public double DenialRate { get; set; }
    public double AverageProcessingDays { get; set; }
    public double AppealSuccessRate { get; set; }
    public int EfficiencyScore { get; set; } // 0-100
}

/// <summary>
/// Insurer financial metrics
/// </summary>
public class InsurerFinancialMetrics
{
    public string InsurerId { get; set; } = string.Empty;
    public string InsurerName { get; set; } = string.Empty;
  public decimal TotalAmountProcessed { get; set; }
    public int TotalClaimsProcessed { get; set; }
    public double ApprovalRate { get; set; }
    public double AverageProcessingDays { get; set; }
    public int AppealCount { get; set; }
    public double AppealApprovalRate { get; set; }
}

/// <summary>
/// Insurer processing ranking
/// </summary>
public class InsurerProcessingRanking
{
    public int Rank { get; set; }
public string InsurerId { get; set; } = string.Empty;
    public string InsurerName { get; set; } = string.Empty;
    public decimal TotalAmountProcessed { get; set; }
    public int ClaimCount { get; set; }
    public double PercentageOfTotal { get; set; }
}

/// <summary>
/// Cost per claim analysis
/// </summary>
public class CostPerClaimAnalysis
{
    public decimal EstimatedProcessingCostPerClaim { get; set; }
    public decimal CostPerApprovedClaim { get; set; }
    public decimal CostPerDeniedClaim { get; set; }
    public int TotalClaimsProcessed { get; set; }
    public decimal TotalProcessingCost { get; set; }
}

/// <summary>
/// Denial cost impact analysis
/// </summary>
public class DenialCostImpactAnalysis
{
    public int TotalDenials { get; set; }
    public decimal TotalDeniedAmount { get; set; }
    public decimal AverageAppleasAmountIfReverted { get; set; }
    public int AppealableCount { get; set; }
    public decimal EstimatedRecoverableAmount { get; set; }
    public decimal PotentialRevenueLoss { get; set; }
}

/// <summary>
/// Appeal cost impact analysis
/// </summary>
public class AppealCostImpactAnalysis
{
    public int TotalAppeals { get; set; }
    public int SuccessfulAppeals { get; set; }
    public int FailedAppeals { get; set; }
    public decimal RecoveredAmount { get; set; }
  public decimal EstimatedAppealProcessingCost { get; set; }
    public decimal NetGainFromAppeals { get; set; }
    public double ROIPercentage { get; set; }
}

/// <summary>
/// Revenue forecast
/// </summary>
public class RevenueForecast
{
    public DateTime ForecastStartDate { get; set; }
    public DateTime ForecastEndDate { get; set; }
    public decimal ForecastedTotalRevenue { get; set; }
    public decimal ForecastedDailyAverage { get; set; }
    public List<RevenueForecastPoint> DailyForecasts { get; set; } = new();
    public double ConfidenceLevel { get; set; }
}

public class RevenueForecastPoint
{
    public DateTime Date { get; set; }
    public decimal ForecastedAmount { get; set; }
    public decimal LowEstimate { get; set; }
    public decimal HighEstimate { get; set; }
}

/// <summary>
/// Revenue projection
/// </summary>
public class RevenueProjection
{
    public DateTime ProjectionDate { get; set; }
    public decimal ProjectedAmount { get; set; }
    public double GrowthRate { get; set; }
}
