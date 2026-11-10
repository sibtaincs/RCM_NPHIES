using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.Reporting;

/// <summary>
/// Reporting Service Interface
/// Provides comprehensive reporting and analytics capabilities
/// </summary>
public interface IReportingService
{
    // ========== CLAIM REPORTS ==========
    /// <summary>
  /// Generate claims summary report
    /// </summary>
    Task<ClaimsSummaryReport> GenerateClaimsSummaryReportAsync(
        DateTime startDate,
        DateTime endDate,
        ReportFilterOptions? filters = null,
        CancellationToken cancellationToken = default);

  /// <summary>
    /// Generate financial performance report
    /// </summary>
    Task<FinancialPerformanceReport> GenerateFinancialPerformanceReportAsync(
        DateTime startDate,
        DateTime endDate,
     ReportFilterOptions? filters = null,
CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate denial analysis report
    /// </summary>
    Task<DenialAnalysisReport> GenerateDenialAnalysisReportAsync(
        DateTime startDate,
      DateTime endDate,
        ReportFilterOptions? filters = null,
     CancellationToken cancellationToken = default);

    // ========== APPEAL REPORTS ==========
    /// <summary>
 /// Generate appeal status report
    /// </summary>
    Task<AppealStatusReport> GenerateAppealStatusReportAsync(
        DateTime startDate,
        DateTime endDate,
        ReportFilterOptions? filters = null,
      CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate approval rate report
    /// </summary>
    Task<ApprovalRateReport> GenerateApprovalRateReportAsync(
        DateTime startDate,
        DateTime endDate,
        ReportFilterOptions? filters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate error code analysis report
    /// </summary>
    Task<ErrorCodeAnalysisReport> GenerateErrorCodeAnalysisReportAsync(
        DateTime startDate,
        DateTime endDate,
        ReportFilterOptions? filters = null,
        CancellationToken cancellationToken = default);

    // ========== COMPLIANCE REPORTS ==========
    /// <summary>
    /// Generate NPHIES compliance report
    /// </summary>
    Task<NphiesComplianceReport> GenerateNphiesComplianceReportAsync(
      DateTime startDate,
        DateTime endDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate SLA compliance report
    /// </summary>
    Task<SlaComplianceReport> GenerateSlaComplianceReportAsync(
        DateTime startDate,
    DateTime endDate,
        ReportFilterOptions? filters = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate timeline compliance report
    /// </summary>
    Task<TimelineComplianceReport> GenerateTimelineComplianceReportAsync(
        DateTime startDate,
        DateTime endDate,
     CancellationToken cancellationToken = default);

    // ========== PERFORMANCE REPORTS ==========
    /// <summary>
    /// Generate provider performance report
    /// </summary>
    Task<ProviderPerformanceReport> GenerateProviderPerformanceReportAsync(
        DateTime startDate,
   DateTime endDate,
     string? providerId = null,
   CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate insurer performance report
    /// </summary>
    Task<InsurerPerformanceReport> GenerateInsurerPerformanceReportAsync(
  DateTime startDate,
        DateTime endDate,
        string? insurerId = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate adjudication statistics report
    /// </summary>
    Task<AdjudicationStatisticsReport> GenerateAdjudicationStatisticsReportAsync(
        DateTime startDate,
        DateTime endDate,
  ReportFilterOptions? filters = null,
        CancellationToken cancellationToken = default);

    // ========== CUSTOM REPORTS ==========
    /// <summary>
    /// Get available report types
 /// </summary>
    Task<List<ReportTypeInfo>> GetAvailableReportTypesAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Save report for later retrieval
    /// </summary>
    Task<string> SaveReportAsync(
        string reportName,
        ReportData reportData,
  CancellationToken cancellationToken = default);

    /// <summary>
    /// Get saved report by ID
    /// </summary>
    Task<ReportData?> GetSavedReportAsync(
   string reportId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List saved reports
    /// </summary>
    Task<List<SavedReportInfo>> ListSavedReportsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete saved report
    /// </summary>
    Task<bool> DeleteSavedReportAsync(
     string reportId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Export report to CSV
    /// </summary>
    Task<byte[]> ExportReportToCsvAsync(
        ReportData report,
 CancellationToken cancellationToken = default);

    /// <summary>
    /// Export report to Excel
    /// </summary>
    Task<byte[]> ExportReportToExcelAsync(
      ReportData report,
   CancellationToken cancellationToken = default);
}

// ========== FILTER OPTIONS ==========

/// <summary>
/// Report filter options
/// </summary>
public class ReportFilterOptions
{
    public string? ProviderId { get; set; }
    public string? InsurerId { get; set; }
    public string? PatientId { get; set; }
    public string? ClaimStatus { get; set; }
public string? AppealStatus { get; set; }
    public string? ErrorCode { get; set; }
    public decimal? MinAmount { get; set; }
    public decimal? MaxAmount { get; set; }
    public List<string>? ServiceTypes { get; set; }
    public List<string>? DiagnosisCodes { get; set; }
}

// ========== REPORT BASE CLASSES ==========

/// <summary>
/// Base report class
/// </summary>
public abstract class ReportBase
{
    public string ReportId { get; set; } = Guid.NewGuid().ToString();
    public string ReportType { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public ReportFilterOptions? Filters { get; set; }
    public int TotalRecords { get; set; }
    public Dictionary<string, object?> Metadata { get; set; } = new();
}

// ========== REPORT TYPES ==========

/// <summary>
/// Claims summary report
/// </summary>
public class ClaimsSummaryReport : ReportBase
{
    public int TotalClaims { get; set; }
    public int ApprovedClaims { get; set; }
    public int DeniedClaims { get; set; }
  public int PartialClaims { get; set; }
    public decimal TotalSubmittedAmount { get; set; }
    public decimal TotalApprovedAmount { get; set; }
    public decimal TotalDeniedAmount { get; set; }
    public double AverageProcessingTimeMinutes { get; set; }
    public List<ClaimSummaryItem> ClaimItems { get; set; } = new();
}

public class ClaimSummaryItem
{
    public string ClaimId { get; set; } = string.Empty;
    public string ClaimNumber { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal SubmittedAmount { get; set; }
    public decimal ApprovedAmount { get; set; }
public DateTime ProcessedDate { get; set; }
}

/// <summary>
/// Financial performance report
/// </summary>
public class FinancialPerformanceReport : ReportBase
{
    public decimal TotalRevenue { get; set; }
    public decimal ApprovedRevenue { get; set; }
public decimal DeniedRevenue { get; set; }
    public decimal PartialRevenue { get; set; }
    public decimal ApprovalPercentage { get; set; }
    public decimal AverageClaimValue { get; set; }
    public decimal AverageApprovedValue { get; set; }
    public List<FinancialTrendItem> TrendData { get; set; } = new();
}

public class FinancialTrendItem
{
    public DateTime Date { get; set; }
    public decimal Revenue { get; set; }
    public decimal ApprovedAmount { get; set; }
    public decimal DeniedAmount { get; set; }
}

/// <summary>
/// Denial analysis report
/// </summary>
public class DenialAnalysisReport : ReportBase
{
    public int TotalDenials { get; set; }
    public decimal TotalDeniedAmount { get; set; }
    public double ApprealablePercentage { get; set; }
    public List<DenialByReasonItem> DenialsByReason { get; set; } = new();
    public List<DenialByProviderItem> DenialsByProvider { get; set; } = new();
    public List<DenialByInsurerItem> DenialsByInsurer { get; set; } = new();
}

public class DenialByReasonItem
{
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorDescription { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
    public double Percentage { get; set; }
}

public class DenialByProviderItem
{
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public int DenialCount { get; set; }
    public decimal DeniedAmount { get; set; }
}

public class DenialByInsurerItem
{
    public string InsurerId { get; set; } = string.Empty;
    public string InsurerName { get; set; } = string.Empty;
    public int DenialCount { get; set; }
    public decimal DeniedAmount { get; set; }
}

/// <summary>
/// Appeal status report
/// </summary>
public class AppealStatusReport : ReportBase
{
    public int TotalAppeals { get; set; }
    public int ActiveAppeals { get; set; }
    public int ApprovedAppeals { get; set; }
    public int DeniedAppeals { get; set; }
    public int PartialAppeals { get; set; }
    public int WithdrawnAppeals { get; set; }
    public double ApprovalRate { get; set; }
    public double AverageResolutionDays { get; set; }
    public List<AppealStatusItem> AppealItems { get; set; } = new();
}

public class AppealStatusItem
{
    public string AppealId { get; set; } = string.Empty;
    public string AppealNumber { get; set; } = string.Empty;
    public int AppealLevel { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Outcome { get; set; } = string.Empty;
    public decimal? ApprovedAmount { get; set; }
    public int AgeInDays { get; set; }
}

/// <summary>
/// Approval rate report
/// </summary>
public class ApprovalRateReport : ReportBase
{
    public double OverallApprovalRate { get; set; }
    public List<ApprovalRateByProviderItem> ByProvider { get; set; } = new();
    public List<ApprovalRateByInsurerItem> ByInsurer { get; set; } = new();
    public List<ApprovalRateByServiceItem> ByService { get; set; } = new();
    public List<ApprovalRateTrendItem> TrendData { get; set; } = new();
}

public class ApprovalRateByProviderItem
{
 public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public int TotalClaims { get; set; }
    public int ApprovedClaims { get; set; }
    public double ApprovalRate { get; set; }
}

public class ApprovalRateByInsurerItem
{
    public string InsurerId { get; set; } = string.Empty;
    public string InsurerName { get; set; } = string.Empty;
    public int TotalClaims { get; set; }
    public int ApprovedClaims { get; set; }
    public double ApprovalRate { get; set; }
}

public class ApprovalRateByServiceItem
{
    public string ServiceType { get; set; } = string.Empty;
    public int TotalClaims { get; set; }
public int ApprovedClaims { get; set; }
public double ApprovalRate { get; set; }
}

public class ApprovalRateTrendItem
{
    public DateTime Date { get; set; }
    public double ApprovalRate { get; set; }
}

/// <summary>
/// Error code analysis report
/// </summary>
public class ErrorCodeAnalysisReport : ReportBase
{
    public int UniqueErrorCodes { get; set; }
    public List<ErrorCodeFrequencyItem> ErrorCodeFrequency { get; set; } = new();
    public List<ErrorCodeByTypeItem> ErrorCodesByType { get; set; } = new();
    public List<ErrorCodeSeverityItem> ErrorCodeBySeverity { get; set; } = new();
}

public class ErrorCodeFrequencyItem
{
    public string ErrorCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Occurrences { get; set; }
    public decimal ImpactAmount { get; set; }
    public bool AllowsAppeal { get; set; }
}

public class ErrorCodeByTypeItem
{
    public string ErrorType { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

public class ErrorCodeSeverityItem
{
    public string Severity { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>
/// NPHIES compliance report
/// </summary>
public class NphiesComplianceReport : ReportBase
{
    public double CompliancePercentage { get; set; }
    public bool IsCompliant { get; set; }
    public List<ComplianceCheckItem> ComplianceChecks { get; set; } = new();
    public List<ComplianceIssueItem> Issues { get; set; } = new();
}

public class ComplianceCheckItem
{
    public string CheckName { get; set; } = string.Empty;
    public bool IsPassed { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class ComplianceIssueItem
{
    public string IssueType { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Recommendation { get; set; } = string.Empty;
}

/// <summary>
/// SLA compliance report
/// </summary>
public class SlaComplianceReport : ReportBase
{
    public double CompliancePercentage { get; set; }
    public int OnTimeClaims { get; set; }
    public int LateCllaims { get; set; }
    public double AverageProcessingDays { get; set; }
    public double SlaThresholdDays { get; set; }
}

/// <summary>
/// Timeline compliance report
/// </summary>
public class TimelineComplianceReport : ReportBase
{
    public int AppealsWithinDeadline { get; set; }
    public int AppealsPastDeadline { get; set; }
    public double CompliancePercentage { get; set; }
public List<TimelineComplianceItem> Details { get; set; } = new();
}

public class TimelineComplianceItem
{
    public string AppealId { get; set; } = string.Empty;
    public DateTime DeadlineDate { get; set; }
    public DateTime? SubmittedDate { get; set; }
    public bool IsWithinDeadline { get; set; }
    public int DaysOverdue { get; set; }
}

/// <summary>
/// Provider performance report
/// </summary>
public class ProviderPerformanceReport : ReportBase
{
    public string ProviderId { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public int TotalClaims { get; set; }
    public decimal TotalSubmittedAmount { get; set; }
    public double ApprovalRate { get; set; }
    public double AverageProcessingDays { get; set; }
    public int AppealCount { get; set; }
    public double AppealApprovalRate { get; set; }
}

/// <summary>
/// Insurer performance report
/// </summary>
public class InsurerPerformanceReport : ReportBase
{
  public string InsurerId { get; set; } = string.Empty;
    public string InsurerName { get; set; } = string.Empty;
    public int TotalClaims { get; set; }
    public decimal TotalAmountReceived { get; set; }
    public double ApprovalRate { get; set; }
    public double AverageProcessingDays { get; set; }
    public int AppealCount { get; set; }
}

/// <summary>
/// Adjudication statistics report
/// </summary>
public class AdjudicationStatisticsReport : ReportBase
{
  public int TotalClaimsProcessed { get; set; }
    public int RulesApplied { get; set; }
    public List<RuleApplicationItem> RuleApplications { get; set; } = new();
 public List<AdjudicationOutcomeItem> Outcomes { get; set; } = new();
}

public class RuleApplicationItem
{
    public string RuleName { get; set; } = string.Empty;
    public int TimesApplied { get; set; }
    public decimal TotalImpactAmount { get; set; }
}

public class AdjudicationOutcomeItem
{
    public string Outcome { get; set; } = string.Empty;
    public int Count { get; set; }
    public decimal Amount { get; set; }
}

/// <summary>
/// Report type information
/// </summary>
public class ReportTypeInfo
{
    public string ReportType { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<string> AvailableFilters { get; set; } = new();
}

/// <summary>
/// Saved report information
/// </summary>
public class SavedReportInfo
{
    public string ReportId { get; set; } = string.Empty;
    public string ReportName { get; set; } = string.Empty;
    public string ReportType { get; set; } = string.Empty;
    public DateTime GeneratedDate { get; set; }
 public string GeneratedBy { get; set; } = string.Empty;
}

/// <summary>
/// Report data wrapper
/// </summary>
public class ReportData
{
    public string ReportType { get; set; } = string.Empty;
    public object ReportContent { get; set; } = null!;
 public Dictionary<string, object?> Metadata { get; set; } = new();
}
