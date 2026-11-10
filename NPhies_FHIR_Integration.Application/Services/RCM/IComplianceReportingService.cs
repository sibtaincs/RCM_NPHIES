using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM;

/// <summary>
/// Compliance Reporting Service Interface
/// Generates NPHIES compliance reports, audits, and quality metrics
/// </summary>
public interface IComplianceReportingService
{
  /// <summary>
    /// Generate NPHIES compliance audit report
    /// </summary>
  /// <param name="fromDate">From date</param>
    /// <param name="toDate">To date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Compliance audit report</returns>
    Task<NphiesComplianceAuditReport> GenerateComplianceAuditAsync(
     DateTime fromDate,
 DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
  /// Generate quality metrics report
 /// </summary>
    /// <param name="fromDate">From date</param>
    /// <param name="toDate">To date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Quality metrics</returns>
    Task<QualityMetricsReport> GenerateQualityMetricsAsync(
 DateTime fromDate,
    DateTime toDate,
        CancellationToken cancellationToken = default);

  /// <summary>
    /// Generate data quality report
    /// </summary>
    /// <param name="fromDate">From date</param>
    /// <param name="toDate">To date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Data quality report</returns>
    Task<DataQualityReport> GenerateDataQualityReportAsync(
        DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Generate performance benchmarking report
    /// </summary>
    /// <param name="providerId">Provider ID</param>
 /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Benchmarking report</returns>
    Task<PerformanceBenchmarkReport> GeneratePerformanceBenchmarkAsync(
  string providerId,
  CancellationToken cancellationToken = default);

    /// <summary>
    /// Export data for external audit
    /// </summary>
    /// <param name="format">Export format (CSV, JSON, XML)</param>
    /// <param name="fromDate">From date</param>
 /// <param name="toDate">To date</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Export data</returns>
    Task<ExportResult> ExportDataForAuditAsync(
 string format,
      DateTime fromDate,
        DateTime toDate,
        CancellationToken cancellationToken = default);

  /// <summary>
    /// Generate regulatory compliance report
 /// </summary>
    /// <param name="regulatoryBody">Regulatory body name</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Regulatory compliance report</returns>
    Task<RegulatoryComplianceReport> GenerateRegulatoryComplianceAsync(
   string regulatoryBody,
   CancellationToken cancellationToken = default);
}

/// <summary>
/// NPHIES Compliance Audit Report
/// </summary>
public class NphiesComplianceAuditReport
{
    /// <summary>
    /// Report ID
    /// </summary>
 public string ReportId { get; set; } = string.Empty;

    /// <summary>
    /// Audit date
    /// </summary>
    public DateTime AuditDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Overall compliance percentage
    /// </summary>
    public decimal OverallCompliance { get; set; }

  /// <summary>
    /// Message type compliance
    /// </summary>
 public Dictionary<string, decimal> MessageTypeCompliance { get; set; } = new();

    /// <summary>
    /// Data element compliance
    /// </summary>
  public Dictionary<string, decimal> DataElementCompliance { get; set; } = new();

    /// <summary>
    /// Validation rule compliance
    /// </summary>
    public Dictionary<string, decimal> ValidationRuleCompliance { get; set; } = new();

    /// <summary>
 /// Critical issues
    /// </summary>
    public List<ComplianceIssue> CriticalIssues { get; set; } = new();

    /// <summary>
    /// Warnings
    /// </summary>
    public List<ComplianceIssue> Warnings { get; set; } = new();

    /// <summary>
  /// Recommendations
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Audit period
    /// </summary>
    public string AuditPeriod { get; set; } = string.Empty;
}

/// <summary>
/// Compliance Issue
 /// </summary>
public class ComplianceIssue
{
   /// <summary>
    /// Issue ID
    /// </summary>
    public string IssueId { get; set; } = string.Empty;

    /// <summary>
    /// Severity (Critical, High, Medium, Low)
  /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

  /// <summary>
    /// Affected records count
    /// </summary>
    public int AffectedRecordsCount { get; set; }

    /// <summary>
   /// Remediation steps
    /// </summary>
   public List<string> RemediationSteps { get; set; } = new();

  /// <summary>
    /// Due date for remediation
 /// </summary>
    public DateTime RemediationDueDate { get; set; }
}

/// <summary>
/// Quality Metrics Report
/// </summary>
public class QualityMetricsReport
{
    /// <summary>
    /// Report ID
    /// </summary>
    public string ReportId { get; set; } = string.Empty;

    /// <summary>
    /// Report date
    /// </summary>
    public DateTime ReportDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Data completeness score
  /// </summary>
    public decimal DataCompletenessScore { get; set; }

    /// <summary>
    /// Data accuracy score
    /// </summary>
    public decimal DataAccuracyScore { get; set; }

    /// <summary>
    /// Timeliness score
    /// </summary>
    public decimal TimelinessScore { get; set; }

    /// <summary>
    /// Consistency score
    /// </summary>
  public decimal ConsistencyScore { get; set; }

    /// <summary>
    /// Overall quality score
    /// </summary>
    public decimal OverallQualityScore { get; set; }

    /// <summary>
    /// Quality issues by category
    /// </summary>
 public Dictionary<string, int> IssuesByCategory { get; set; } = new();

    /// <summary>
    /// Improvement trend
    /// </summary>
   public string ImprovementTrend { get; set; } = string.Empty;

    /// <summary>
    /// Quality level (Excellent, Good, Fair, Poor)
 /// </summary>
    public string QualityLevel { get; set; } = string.Empty;
}

/// <summary>
/// Data Quality Report
/// </summary>
public class DataQualityReport
{
    /// <summary>
    /// Report ID
    /// </summary>
    public string ReportId { get; set; } = string.Empty;

    /// <summary>
    /// Generated date
    /// </summary>
    public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total records checked
    /// </summary>
    public int TotalRecordsChecked { get; set; }

  /// <summary>
    /// Records with issues
    /// </summary>
    public int RecordsWithIssues { get; set; }

    /// <summary>
    /// Data quality percentage
    /// </summary>
    public decimal DataQualityPercentage { get; set; }

    /// <summary>
    /// Missing data issues
    /// </summary>
    public int MissingDataIssues { get; set; }

    /// <summary>
    /// Invalid format issues
    /// </summary>
    public int InvalidFormatIssues { get; set; }

    /// <summary>
  /// Duplicate records
    /// </summary>
    public int DuplicateRecords { get; set; }

  /// <summary>
    /// Validation errors
    /// </summary>
   public int ValidationErrors { get; set; }

    /// <summary>
    /// Detailed issues
    /// </summary>
    public List<DataQualityIssue> DetailedIssues { get; set; } = new();
}

/// <summary>
/// Data Quality Issue
/// </summary>
public class DataQualityIssue
{
    /// <summary>
    /// Record ID
    /// </summary>
    public string RecordId { get; set; } = string.Empty;

    /// <summary>
    /// Field name
    /// </summary>
    public string FieldName { get; set; } = string.Empty;

    /// <summary>
    /// Issue type
    /// </summary>
public string IssueType { get; set; } = string.Empty;

    /// <summary>
    /// Issue description
    /// </summary>
    public string IssueDescription { get; set; } = string.Empty;

    /// <summary>
    /// Correction action
    /// </summary>
    public string CorrectionAction { get; set; } = string.Empty;
}

/// <summary>
/// Performance Benchmark Report
/// </summary>
public class PerformanceBenchmarkReport
{
    /// <summary>
    /// Provider ID
    /// </summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>
    /// Report date
    /// </summary>
    public DateTime ReportDate { get; set; } = DateTime.UtcNow;

 /// <summary>
    /// Provider performance score
    /// </summary>
    public decimal ProviderScore { get; set; }

    /// <summary>
    /// Network average score
    /// </summary>
    public decimal NetworkAverageScore { get; set; }

    /// <summary>
    /// Top performer score
    /// </summary>
    public decimal TopPerformerScore { get; set; }

    /// <summary>
    /// Percentile rank
    /// </summary>
    public int PercentileRank { get; set; }

    /// <summary>
    /// Gap to average
  /// </summary>
    public decimal GapToAverage { get; set; }

    /// <summary>
    /// Gap to top performer
    /// </summary>
    public decimal GapToTopPerformer { get; set; }

    /// <summary>
    /// Performance strengths
    /// </summary>
    public List<string> Strengths { get; set; } = new();

    /// <summary>
 /// Performance improvement areas
    /// </summary>
    public List<string> ImprovementAreas { get; set; } = new();

    /// <summary>
    /// Recommended actions
 /// </summary>
    public List<string> RecommendedActions { get; set; } = new();
}

/// <summary>
/// Export Result
/// </summary>
public class ExportResult
{
    /// <summary>
    /// Export ID
    /// </summary>
    public string ExportId { get; set; } = string.Empty;

    /// <summary>
    /// Export format
  /// </summary>
    public string ExportFormat { get; set; } = string.Empty;

    /// <summary>
    /// File location/URL
    /// </summary>
    public string FileLocation { get; set; } = string.Empty;

    /// <summary>
 /// Record count
    /// </summary>
    public int RecordCount { get; set; }

    /// <summary>
    /// File size (bytes)
    /// </summary>
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Export status
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Expiration date
    /// </summary>
    public DateTime ExpirationDate { get; set; }

    /// <summary>
    /// Checksum for verification
    /// </summary>
    public string Checksum { get; set; } = string.Empty;
}

/// <summary>
/// Regulatory Compliance Report
/// </summary>
public class RegulatoryComplianceReport
{
    /// <summary>
    /// Report ID
    /// </summary>
    public string ReportId { get; set; } = string.Empty;

    /// <summary>
    /// Regulatory body
    /// </summary>
    public string RegulatoryBody { get; set; } = string.Empty;

    /// <summary>
    /// Report date
    /// </summary>
    public DateTime ReportDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Compliance status (Compliant, Non-Compliant, Conditional)
    /// </summary>
    public string ComplianceStatus { get; set; } = string.Empty;

 /// <summary>
    /// Required controls
    /// </summary>
    public List<RegulatoryControl> RequiredControls { get; set; } = new();

    /// <summary>
    /// Control assessment results
    /// </summary>
    public Dictionary<string, string> ControlAssessmentResults { get; set; } = new();

    /// <summary>
    /// Findings
    /// </summary>
    public List<RegulatoryFinding> Findings { get; set; } = new();

  /// <summary>
    /// Corrective actions
    /// </summary>
    public List<CorrectiveAction> CorrectiveActions { get; set; } = new();
}

/// <summary>
/// Regulatory Control
/// </summary>
public class RegulatoryControl
{
    /// <summary>
    /// Control ID
/// </summary>
    public string ControlId { get; set; } = string.Empty;

    /// <summary>
    /// Control description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
 /// Control category
    /// </summary>
    public string Category { get; set; } = string.Empty;

    /// <summary>
    /// Assessment method
    /// </summary>
    public string AssessmentMethod { get; set; } = string.Empty;
}

/// <summary>
/// Regulatory Finding
/// </summary>
public class RegulatoryFinding
{
    /// <summary>
    /// Finding ID
    /// </summary>
    public string FindingId { get; set; } = string.Empty;

    /// <summary>
    /// Finding type
    /// </summary>
    public string FindingType { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
  public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Risk level
    /// </summary>
    public string RiskLevel { get; set; } = string.Empty;

    /// <summary>
    /// Root cause
    /// </summary>
    public string RootCause { get; set; } = string.Empty;
}

/// <summary>
/// Corrective Action
/// </summary>
public class CorrectiveAction
{
    /// <summary>
  /// Action ID
    /// </summary>
    public string ActionId { get; set; } = string.Empty;

    /// <summary>
    /// Description
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Responsible party
    /// </summary>
    public string ResponsibleParty { get; set; } = string.Empty;

    /// <summary>
    /// Due date
    /// </summary>
    public DateTime DueDate { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = string.Empty;
}
