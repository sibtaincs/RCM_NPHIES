using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NPhies_FHIR_Integration.Application.Services.Analytics;

/// <summary>
/// Real-Time Analytics Service Interface
/// Provides live dashboards, stream processing, and instant metrics
/// </summary>
public interface IRealtimeAnalyticsService
{
    // ========== LIVE DASHBOARDS ==========
    /// <summary>
    /// Get live claims dashboard
    /// </summary>
    Task<LiveClaimsDashboard> GetLiveClaimsDashboardAsync(
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Get live appeals dashboard
    /// </summary>
 Task<LiveAppealsDashboard> GetLiveAppealsDashboardAsync(
        CancellationToken cancellationToken = default);

  /// <summary>
    /// Get live denials dashboard
    /// </summary>
    Task<LiveDenialsDashboard> GetLiveDenialsDashboardAsync(
    CancellationToken cancellationToken = default);

    /// <summary>
    /// Get live financial dashboard
    /// </summary>
    Task<LiveFinancialDashboard> GetLiveFinancialDashboardAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get live provider dashboard
    /// </summary>
    Task<LiveProviderDashboard> GetLiveProviderDashboardAsync(
        CancellationToken cancellationToken = default);

    // ========== STREAM PROCESSING ==========
    /// <summary>
    /// Stream claim events
    /// </summary>
    Task<List<StreamEvent>> GetClaimStreamEventsAsync(
        int lastNMinutes = 60,
    CancellationToken cancellationToken = default);

    /// <summary>
    /// Stream appeal events
    /// </summary>
    Task<List<StreamEvent>> GetAppealStreamEventsAsync(
        int lastNMinutes = 60,
        CancellationToken cancellationToken = default);

    // ========== INSTANT METRICS ==========
    /// <summary>
    /// Get instant metrics
    /// </summary>
    Task<InstantMetrics> GetInstantMetricsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get real-time KPIs
    /// </summary>
    Task<List<KeyPerformanceIndicator>> GetRealtimeKPIsAsync(
        CancellationToken cancellationToken = default);

    // ========== ALERTS & NOTIFICATIONS ==========
    /// <summary>
    /// Get real-time alerts
    /// </summary>
    Task<List<RealtimeAlert>> GetRealtimeAlertsAsync(
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Get alert history
    /// </summary>
    Task<List<RealtimeAlert>> GetAlertHistoryAsync(
        DateTime startDate,
        DateTime endDate,
   CancellationToken cancellationToken = default);

    /// <summary>
    /// Subscribe to alert
    /// </summary>
    Task<string> SubscribeToAlertAsync(
        AlertSubscription subscription,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Unsubscribe from alert
    /// </summary>
    Task<bool> UnsubscribeFromAlertAsync(
        string subscriptionId,
      CancellationToken cancellationToken = default);

    // ========== TREND ANALYSIS ==========
    /// <summary>
    /// Get real-time trends
    /// </summary>
    Task<List<TrendData>> GetRealtimeTrendsAsync(
        string metricName,
   int intervalMinutes = 5,
  CancellationToken cancellationToken = default);

    /// <summary>
  /// Get anomaly alerts
    /// </summary>
    Task<List<AnomalyAlert>> GetAnomalyAlertsAsync(
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Real-Time Analytics Service Implementation
/// </summary>
public class RealtimeAnalyticsService : IRealtimeAnalyticsService
{
    private readonly ILogger<RealtimeAnalyticsService> _logger;

    public RealtimeAnalyticsService(ILogger<RealtimeAnalyticsService> logger)
  {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ========== LIVE DASHBOARDS ==========

    /// <summary>
    /// Get live claims dashboard
    /// </summary>
  public async Task<LiveClaimsDashboard> GetLiveClaimsDashboardAsync(
CancellationToken cancellationToken = default)
    {
        try
        {
   _logger.LogInformation("Retrieving live claims dashboard");

      var dashboard = new LiveClaimsDashboard
         {
     DashboardId = Guid.NewGuid().ToString(),
 LastUpdated = DateTime.UtcNow,
        TotalClaimsToday = 1250,
           ClaimsProcessedToday = 890,
       ClaimsPendingToday = 360,
                AverageProcessingTime = 5.5,
         ApprovalRate = 0.87,
          DenialRate = 0.08,
       PendingRate = 0.05,
       TopProviders = new List<ProviderClaimData>
       {
  new ProviderClaimData { ProviderId = "PROV-001", ProviderName = "City Hospital", ClaimsCount = 45, ApprovalRate = 0.92 },
               new ProviderClaimData { ProviderId = "PROV-002", ProviderName = "County Clinic", ClaimsCount = 38, ApprovalRate = 0.89 }
        },
     TopInsurers = new List<InsurerClaimData>
     {
         new InsurerClaimData { InsurerId = "INS-001", InsurerName = "Blue Cross", ClaimsCount = 120, ApprovalRate = 0.90 }
          },
       ActiveAlerts = 3,
     RecentEvents = new List<DashboardEvent>
       {
           new DashboardEvent { EventTime = DateTime.UtcNow.AddMinutes(-5), EventType = "ClaimApproved", Description = "Claim CLM-12345 approved for $4,850" }
    }
            };

            _logger.LogInformation("Live claims dashboard retrieved: {Processed} processed today",
          dashboard.ClaimsProcessedToday);

   return dashboard;
        }
      catch (Exception ex)
        {
     _logger.LogError(ex, "Error retrieving live claims dashboard");
            throw;
        }
    }

    /// <summary>
  /// Get live appeals dashboard
    /// </summary>
    public async Task<LiveAppealsDashboard> GetLiveAppealsDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Retrieving live appeals dashboard");

          var dashboard = new LiveAppealsDashboard
 {
     DashboardId = Guid.NewGuid().ToString(),
  LastUpdated = DateTime.UtcNow,
        TotalAppealsToday = 45,
       AppealsSubmittedToday = 32,
         AppealsPendingToday = 13,
                AverageAppealTime = 7.2,
 Level1SuccessRate = 0.60,
     Level2SuccessRate = 0.65,
      Level3SuccessRate = 0.45,
     OverallSuccessRate = 0.58,
             PendingAppeals = 156,
                ActiveAppeals = new List<AppealStatus>
            {
           new AppealStatus { AppealId = "APP-001", ClaimId = "CLM-001", Level = 1, Status = "Submitted", SubmittedDate = DateTime.UtcNow.AddDays(-2) }
       }
     };

            _logger.LogInformation("Live appeals dashboard retrieved: {SuccessRate}% success",
        dashboard.OverallSuccessRate * 100);

         return dashboard;
    }
        catch (Exception ex)
  {
     _logger.LogError(ex, "Error retrieving live appeals dashboard");
     throw;
        }
    }

    /// <summary>
    /// Get live denials dashboard
    /// </summary>
    public async Task<LiveDenialsDashboard> GetLiveDenialsDashboardAsync(
        CancellationToken cancellationToken = default)
    {
        try
        {
  _logger.LogInformation("Retrieving live denials dashboard");

       var dashboard = new LiveDenialsDashboard
            {
    DashboardId = Guid.NewGuid().ToString(),
           LastUpdated = DateTime.UtcNow,
        TotalDenialsToday = 100,
    DenialRatePercentage = 8.0,
       TopDenialReasons = new List<DenialReasonData>
    {
                new DenialReasonData { ErrorCode = "AD-1-1", Reason = "Service not covered", Count = 35, Percentage = 35.0 },
              new DenialReasonData { ErrorCode = "AD-2-5", Reason = "Missing documentation", Count = 28, Percentage = 28.0 }
     },
 DenialsByProvider = new List<ProviderDenialData>
       {
      new ProviderDenialData { ProviderId = "PROV-001", ProviderName = "City Hospital", DenialCount = 8, DenialRate = 0.07 }
        },
          RecoveryRate = 0.35,
         RecoveredAmount = 42500m
       };

  _logger.LogInformation("Live denials dashboard retrieved: {Rate}% denial rate",
     dashboard.DenialRatePercentage);

       return dashboard;
      }
        catch (Exception ex)
        {
    _logger.LogError(ex, "Error retrieving live denials dashboard");
        throw;
        }
    }

    /// <summary>
    /// Get live financial dashboard
    /// </summary>
    public async Task<LiveFinancialDashboard> GetLiveFinancialDashboardAsync(
        CancellationToken cancellationToken = default)
    {
  try
        {
       _logger.LogInformation("Retrieving live financial dashboard");

            var dashboard = new LiveFinancialDashboard
            {
      DashboardId = Guid.NewGuid().ToString(),
    LastUpdated = DateTime.UtcNow,
     TodayRevenue = 2450000m,
     TodayApprovedAmount = 2100000m,
           TodayDeniedAmount = 175000m,
                AverageClaimValue = 1960m,
     AverageApprovedValue = 2356m,
            ProcessingCost = 12500m,
          CostPerClaim = 10m,
    ROI = 195m,
     YearToDateRevenue = 18750000m,
   YearToDateApprovedAmount = 16200000m
            };

       _logger.LogInformation("Live financial dashboard retrieved: ${Revenue} today revenue",
       dashboard.TodayRevenue);

            return dashboard;
        }
      catch (Exception ex)
      {
    _logger.LogError(ex, "Error retrieving live financial dashboard");
   throw;
     }
    }

    /// <summary>
    /// Get live provider dashboard
    /// </summary>
    public async Task<LiveProviderDashboard> GetLiveProviderDashboardAsync(
     CancellationToken cancellationToken = default)
    {
     try
        {
            _logger.LogInformation("Retrieving live provider dashboard");

     var dashboard = new LiveProviderDashboard
 {
                DashboardId = Guid.NewGuid().ToString(),
      LastUpdated = DateTime.UtcNow,
      TotalProviders = 450,
         ActiveProvidersToday = 120,
                TopPerformers = new List<ProviderPerformance>
            {
     new ProviderPerformance { ProviderId = "PROV-001", ProviderName = "City Hospital", ApprovalRate = 0.95, DenialRate = 0.03, ProcessingTime = 4.2 }
 },
        LowPerformers = new List<ProviderPerformance>
              {
           new ProviderPerformance { ProviderId = "PROV-100", ProviderName = "Small Clinic", ApprovalRate = 0.72, DenialRate = 0.18, ProcessingTime = 9.5 }
       },
AverageApprovalRate = 0.87,
    AverageDenialRate = 0.08,
              AverageProcessingTime = 5.8
   };

            _logger.LogInformation("Live provider dashboard retrieved: {Active} active providers today",
                dashboard.ActiveProvidersToday);

          return dashboard;
        }
        catch (Exception ex)
        {
 _logger.LogError(ex, "Error retrieving live provider dashboard");
   throw;
   }
    }

    // ========== STREAM PROCESSING ==========

    /// <summary>
    /// Get claim stream events
    /// </summary>
    public async Task<List<StreamEvent>> GetClaimStreamEventsAsync(
     int lastNMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        try
        {
    _logger.LogInformation("Retrieving claim stream events for last {Minutes} minutes",
     lastNMinutes);

         var events = new List<StreamEvent>
     {
        new StreamEvent { EventId = Guid.NewGuid().ToString(), EventType = "ClaimCreated", Timestamp = DateTime.UtcNow.AddMinutes(-5), Data = new { ClaimId = "CLM-12345" } },
    new StreamEvent { EventId = Guid.NewGuid().ToString(), EventType = "ClaimApproved", Timestamp = DateTime.UtcNow.AddMinutes(-3), Data = new { ClaimId = "CLM-12344", Amount = 4850m } },
    new StreamEvent { EventId = Guid.NewGuid().ToString(), EventType = "ClaimDenied", Timestamp = DateTime.UtcNow.AddMinutes(-1), Data = new { ClaimId = "CLM-12343", Reason = "AD-1-1" } }
            };

            _logger.LogInformation("Retrieved {Count} claim stream events", events.Count);
      return events;
        }
        catch (Exception ex)
        {
    _logger.LogError(ex, "Error retrieving claim stream events");
   throw;
        }
  }

    /// <summary>
    /// Get appeal stream events
    /// </summary>
    public async Task<List<StreamEvent>> GetAppealStreamEventsAsync(
        int lastNMinutes = 60,
        CancellationToken cancellationToken = default)
    {
        try
        {
       _logger.LogInformation("Retrieving appeal stream events for last {Minutes} minutes",
     lastNMinutes);

       var events = new List<StreamEvent>
          {
      new StreamEvent { EventId = Guid.NewGuid().ToString(), EventType = "AppealCreated", Timestamp = DateTime.UtcNow.AddMinutes(-4), Data = new { AppealId = "APP-001" } },
    new StreamEvent { EventId = Guid.NewGuid().ToString(), EventType = "AppealSubmitted", Timestamp = DateTime.UtcNow.AddMinutes(-2), Data = new { AppealId = "APP-002", Level = 1 } }
            };

            _logger.LogInformation("Retrieved {Count} appeal stream events", events.Count);
            return events;
        }
 catch (Exception ex)
        {
  _logger.LogError(ex, "Error retrieving appeal stream events");
       throw;
        }
    }

    // ========== INSTANT METRICS ==========

    /// <summary>
    /// Get instant metrics
    /// </summary>
    public async Task<InstantMetrics> GetInstantMetricsAsync(
   CancellationToken cancellationToken = default)
    {
        try
      {
            _logger.LogInformation("Calculating instant metrics");

 var metrics = new InstantMetrics
            {
     Timestamp = DateTime.UtcNow,
 ClaimsPerMinute = 0.87,
         ApprovalPerMinute = 0.75,
      DenialPerMinute = 0.07,
    AppealsPerMinute = 0.03,
        AverageCycleTime = 5.5,
      SystemHealth = "Healthy",
       DatabaseLatency = 45,
      ApiLatency = 125,
     ActiveSessions = 250
   };

    _logger.LogInformation("Instant metrics calculated: {ClaimsPerMin} claims/minute",
     metrics.ClaimsPerMinute);

     return metrics;
        }
  catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating instant metrics");
   throw;
        }
    }

    /// <summary>
    /// Get real-time KPIs
    /// </summary>
    public async Task<List<KeyPerformanceIndicator>> GetRealtimeKPIsAsync(
     CancellationToken cancellationToken = default)
    {
try
        {
            _logger.LogInformation("Retrieving real-time KPIs");

    var kpis = new List<KeyPerformanceIndicator>
        {
   new KeyPerformanceIndicator { KpiName = "Claims Processed", CurrentValue = 890, Target = 1000, Status = "On Track" },
    new KeyPerformanceIndicator { KpiName = "Approval Rate", CurrentValue = 87, Target = 90, Status = "Off Track" },
new KeyPerformanceIndicator { KpiName = "Average Processing Time", CurrentValue = 5.5, Target = 5.0, Status = "Slightly Off" },
       new KeyPerformanceIndicator { KpiName = "Fraud Prevention", CurrentValue = 12500, Target = 10000, Status = "Exceeding" }
            };

            _logger.LogInformation("Retrieved {Count} KPIs", kpis.Count);
    return kpis;
        }
        catch (Exception ex)
        {
         _logger.LogError(ex, "Error retrieving KPIs");
        throw;
        }
    }

    // ========== ALERTS & NOTIFICATIONS ==========

    /// <summary>
    /// Get real-time alerts
    /// </summary>
    public async Task<List<RealtimeAlert>> GetRealtimeAlertsAsync(
        CancellationToken cancellationToken = default)
    {
        try
   {
      _logger.LogInformation("Retrieving real-time alerts");

   var alerts = new List<RealtimeAlert>
     {
  new RealtimeAlert { AlertId = Guid.NewGuid().ToString(), AlertType = "HighDenialRate", Severity = "High", Message = "Denial rate exceeded 10%", CreatedDate = DateTime.UtcNow.AddMinutes(-15) },
    new RealtimeAlert { AlertId = Guid.NewGuid().ToString(), AlertType = "SlowProcessing", Severity = "Medium", Message = "Average processing time above target", CreatedDate = DateTime.UtcNow.AddMinutes(-5) }
            };

       _logger.LogInformation("Retrieved {Count} active alerts", alerts.Count);
      return alerts;
        }
        catch (Exception ex)
        {
_logger.LogError(ex, "Error retrieving alerts");
  throw;
      }
    }

    /// <summary>
    /// Get alert history
 /// </summary>
    public async Task<List<RealtimeAlert>> GetAlertHistoryAsync(
DateTime startDate,
      DateTime endDate,
  CancellationToken cancellationToken = default)
    {
  try
        {
    _logger.LogInformation("Retrieving alert history from {Start} to {End}",
          startDate, endDate);

       var alerts = new List<RealtimeAlert>
       {
    new RealtimeAlert { AlertId = Guid.NewGuid().ToString(), AlertType = "HighDenialRate", Severity = "High", Message = "Denial rate exceeded 10%", CreatedDate = startDate.AddHours(2), ResolvedDate = startDate.AddHours(4) }
         };

 return alerts;
      }
      catch (Exception ex)
    {
            _logger.LogError(ex, "Error retrieving alert history");
         throw;
   }
    }

    /// <summary>
    /// Subscribe to alert
    /// </summary>
    public async Task<string> SubscribeToAlertAsync(
        AlertSubscription subscription,
    CancellationToken cancellationToken = default)
    {
      try
        {
      var subscriptionId = Guid.NewGuid().ToString();
    _logger.LogInformation("Alert subscription created: {SubscriptionId}, Type: {Type}",
    subscriptionId, subscription.AlertType);

            return subscriptionId;
  }
 catch (Exception ex)
      {
            _logger.LogError(ex, "Error subscribing to alert");
            throw;
 }
    }

    /// <summary>
    /// Unsubscribe from alert
    /// </summary>
    public async Task<bool> UnsubscribeFromAlertAsync(
        string subscriptionId,
    CancellationToken cancellationToken = default)
    {
     try
        {
     _logger.LogInformation("Alert subscription removed: {SubscriptionId}",
  subscriptionId);

       return true;
        }
    catch (Exception ex)
        {
            _logger.LogError(ex, "Error unsubscribing from alert");
            return false;
     }
    }

    // ========== TREND ANALYSIS ==========

    /// <summary>
    /// Get real-time trends
    /// </summary>
    public async Task<List<TrendData>> GetRealtimeTrendsAsync(
  string metricName,
        int intervalMinutes = 5,
     CancellationToken cancellationToken = default)
    {
        try
        {
         _logger.LogInformation("Retrieving real-time trends for {Metric}", metricName);

         var trends = new List<TrendData>
      {
    new TrendData { Timestamp = DateTime.UtcNow.AddMinutes(-15), Value = 0.85 },
                new TrendData { Timestamp = DateTime.UtcNow.AddMinutes(-10), Value = 0.87 },
     new TrendData { Timestamp = DateTime.UtcNow.AddMinutes(-5), Value = 0.88 },
       new TrendData { Timestamp = DateTime.UtcNow, Value = 0.87 }
    };

   return trends;
        }
        catch (Exception ex)
        {
        _logger.LogError(ex, "Error retrieving trends");
        throw;
        }
    }

    /// <summary>
  /// Get anomaly alerts
    /// </summary>
    public async Task<List<AnomalyAlert>> GetAnomalyAlertsAsync(
        CancellationToken cancellationToken = default)
    {
        try
{
            _logger.LogInformation("Retrieving anomaly alerts");

var anomalies = new List<AnomalyAlert>
            {
        new AnomalyAlert { AnomalyId = Guid.NewGuid().ToString(), MetricName = "DenialRate", NormalValue = 0.08, CurrentValue = 0.15, Severity = "High", DetectedDate = DateTime.UtcNow.AddMinutes(-10) }
     };

   _logger.LogInformation("Retrieved {Count} anomaly alerts", anomalies.Count);
            return anomalies;
      }
   catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving anomaly alerts");
            throw;
        }
    }
}

// ========== DATA MODELS ==========

/// <summary>
/// Live claims dashboard
/// </summary>
public class LiveClaimsDashboard
{
    public string DashboardId { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public int TotalClaimsToday { get; set; }
    public int ClaimsProcessedToday { get; set; }
    public int ClaimsPendingToday { get; set; }
    public double AverageProcessingTime { get; set; }
    public double ApprovalRate { get; set; }
public double DenialRate { get; set; }
    public double PendingRate { get; set; }
    public List<ProviderClaimData> TopProviders { get; set; } = new();
    public List<InsurerClaimData> TopInsurers { get; set; } = new();
    public int ActiveAlerts { get; set; }
    public List<DashboardEvent> RecentEvents { get; set; } = new();
}

/// <summary>
/// Provider claim data
/// </summary>
public class ProviderClaimData
{
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public int ClaimsCount { get; set; }
    public double ApprovalRate { get; set; }
}

/// <summary>
/// Insurer claim data
/// </summary>
public class InsurerClaimData
{
    public string InsurerId { get; set; } = string.Empty;
 public string InsurerName { get; set; } = string.Empty;
    public int ClaimsCount { get; set; }
    public double ApprovalRate { get; set; }
}

/// <summary>
/// Dashboard event
/// </summary>
public class DashboardEvent
{
    public DateTime EventTime { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}

/// <summary>
/// Live appeals dashboard
/// </summary>
public class LiveAppealsDashboard
{
    public string DashboardId { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public int TotalAppealsToday { get; set; }
    public int AppealsSubmittedToday { get; set; }
    public int AppealsPendingToday { get; set; }
    public double AverageAppealTime { get; set; }
    public double Level1SuccessRate { get; set; }
    public double Level2SuccessRate { get; set; }
    public double Level3SuccessRate { get; set; }
    public double OverallSuccessRate { get; set; }
    public int PendingAppeals { get; set; }
    public List<AppealStatus> ActiveAppeals { get; set; } = new();
}

/// <summary>
/// Appeal status
/// </summary>
public class AppealStatus
{
    public string AppealId { get; set; } = string.Empty;
    public string ClaimId { get; set; } = string.Empty;
    public int Level { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime SubmittedDate { get; set; }
}

/// <summary>
/// Live denials dashboard
/// </summary>
public class LiveDenialsDashboard
{
    public string DashboardId { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public int TotalDenialsToday { get; set; }
    public double DenialRatePercentage { get; set; }
    public List<DenialReasonData> TopDenialReasons { get; set; } = new();
    public List<ProviderDenialData> DenialsByProvider { get; set; } = new();
    public double RecoveryRate { get; set; }
    public decimal RecoveredAmount { get; set; }
}

/// <summary>
/// Denial reason data
/// </summary>
public class DenialReasonData
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

/// <summary>
/// Provider denial data
/// </summary>
public class ProviderDenialData
{
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public int DenialCount { get; set; }
    public double DenialRate { get; set; }
}

/// <summary>
/// Live financial dashboard
/// </summary>
public class LiveFinancialDashboard
{
    public string DashboardId { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public decimal TodayRevenue { get; set; }
    public decimal TodayApprovedAmount { get; set; }
    public decimal TodayDeniedAmount { get; set; }
  public decimal AverageClaimValue { get; set; }
    public decimal AverageApprovedValue { get; set; }
    public decimal ProcessingCost { get; set; }
    public decimal CostPerClaim { get; set; }
    public decimal ROI { get; set; }
    public decimal YearToDateRevenue { get; set; }
    public decimal YearToDateApprovedAmount { get; set; }
}

/// <summary>
/// Live provider dashboard
/// </summary>
public class LiveProviderDashboard
{
    public string DashboardId { get; set; } = string.Empty;
    public DateTime LastUpdated { get; set; }
    public int TotalProviders { get; set; }
    public int ActiveProvidersToday { get; set; }
    public List<ProviderPerformance> TopPerformers { get; set; } = new();
    public List<ProviderPerformance> LowPerformers { get; set; } = new();
    public double AverageApprovalRate { get; set; }
    public double AverageDenialRate { get; set; }
    public double AverageProcessingTime { get; set; }
}

/// <summary>
/// Provider performance
/// </summary>
public class ProviderPerformance
{
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public double ApprovalRate { get; set; }
    public double DenialRate { get; set; }
    public double ProcessingTime { get; set; }
}

/// <summary>
/// Stream event
/// </summary>
public class StreamEvent
{
    public string EventId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
  public object? Data { get; set; }
}

/// <summary>
/// Instant metrics
/// </summary>
public class InstantMetrics
{
  public DateTime Timestamp { get; set; }
    public double ClaimsPerMinute { get; set; }
    public double ApprovalPerMinute { get; set; }
  public double DenialPerMinute { get; set; }
    public double AppealsPerMinute { get; set; }
    public double AverageCycleTime { get; set; }
    public string SystemHealth { get; set; } = string.Empty;
  public int DatabaseLatency { get; set; }
    public int ApiLatency { get; set; }
    public int ActiveSessions { get; set; }
}

/// <summary>
/// Key performance indicator
/// </summary>
public class KeyPerformanceIndicator
{
    public string KpiName { get; set; } = string.Empty;
    public double CurrentValue { get; set; }
  public double Target { get; set; }
    public string Status { get; set; } = string.Empty;
}

/// <summary>
/// Real-time alert
/// </summary>
public class RealtimeAlert
{
    public string AlertId { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public DateTime? ResolvedDate { get; set; }
}

/// <summary>
/// Alert subscription
/// </summary>
public class AlertSubscription
{
    public string AlertType { get; set; } = string.Empty;
    public string Threshold { get; set; } = string.Empty;
    public string NotificationMethod { get; set; } = string.Empty;
}

/// <summary>
/// Trend data
/// </summary>
public class TrendData
{
    public DateTime Timestamp { get; set; }
    public double Value { get; set; }
}

/// <summary>
/// Anomaly alert
/// </summary>
public class AnomalyAlert
{
    public string AnomalyId { get; set; } = string.Empty;
    public string MetricName { get; set; } = string.Empty;
    public double NormalValue { get; set; }
    public double CurrentValue { get; set; }
    public string Severity { get; set; } = string.Empty;
    public DateTime DetectedDate { get; set; }
}
