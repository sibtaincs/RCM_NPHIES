using System;
using System.Collections.Generic;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Domain.Entities.Reporting;

/// <summary>
/// Report entity for storing generated reports
/// </summary>
public class Report : BaseEntity
{
  /// <summary>
    /// Unique report identifier
    /// </summary>
    public string ReportNumber { get; set; } = string.Empty;

 /// <summary>
    /// Report name/title
    /// </summary>
    public string ReportName { get; set; } = string.Empty;

    /// <summary>
/// Report type (ClaimsSummary, FinancialPerformance, etc.)
/// </summary>
    public string ReportType { get; set; } = string.Empty;

    /// <summary>
    /// Report description
/// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Report start date range
  /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Report end date range
 /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Report generation date
/// </summary>
    public DateTime GeneratedDate { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Report status (Generated, Processing, Scheduled, etc.)
    /// </summary>
    public string ReportStatus { get; set; } = "Generated";

    /// <summary>
    /// User who generated the report
  /// </summary>
    public string GeneratedBy { get; set; } = "system";

    /// <summary>
    /// Filters applied to report
/// </summary>
    public string? FilterJson { get; set; }

    /// <summary>
    /// Report summary/metadata
    /// </summary>
    public string? MetadataJson { get; set; }

  /// <summary>
    /// Report content (serialized)
    /// </summary>
    public string? ReportContent { get; set; }

    /// <summary>
    /// Total records in report
    /// </summary>
    public int TotalRecords { get; set; }

    /// <summary>
    /// File path if exported
    /// </summary>
    public string? ExportFilePath { get; set; }

    /// <summary>
    /// Export format (CSV, Excel, PDF, etc.)
    /// </summary>
    public string? ExportFormat { get; set; }

    /// <summary>
    /// Is report scheduled for delivery
    /// </summary>
    public bool IsScheduled { get; set; }

    /// <summary>
    /// Schedule frequency (Daily, Weekly, Monthly, etc.)
    /// </summary>
    public string? ScheduleFrequency { get; set; }

    /// <summary>
    /// Next scheduled generation date
    /// </summary>
    public DateTime? NextScheduleDate { get; set; }

    /// <summary>
/// Email recipients for scheduled reports
    /// </summary>
    public string? EmailRecipients { get; set; }

    /// <summary>
    /// Report execution time in milliseconds
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Notes about the report
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Audit and compliance history
    /// </summary>
    public ICollection<ReportAudit> AuditTrail { get; set; } = new List<ReportAudit>();
}

/// <summary>
/// Report audit trail entity
/// </summary>
public class ReportAudit : BaseEntity
{
    /// <summary>
    /// Report ID this audit entry belongs to
    /// </summary>
    public string ReportId { get; set; } = string.Empty;

    /// <summary>
    /// Report entity
    /// </summary>
    public Report? Report { get; set; }

    /// <summary>
    /// Action performed (Generated, Exported, Viewed, Deleted, etc.)
    /// </summary>
  public string Action { get; set; } = string.Empty;

    /// <summary>
    /// User who performed the action
    /// </summary>
    public string PerformedBy { get; set; } = "system";

    /// <summary>
    /// Description of action
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Timestamp of action
    /// </summary>
    public DateTime ActionDate { get; set; } = DateTime.UtcNow;

  /// <summary>
    /// Additional details
    /// </summary>
    public string? Details { get; set; }
}

/// <summary>
/// Dashboard entity for storing dashboard configurations
/// </summary>
public class Dashboard : BaseEntity
{
    /// <summary>
    /// Dashboard name
    /// </summary>
    public string DashboardName { get; set; } = string.Empty;

    /// <summary>
    /// Dashboard type (Executive, Provider, Insurer, Compliance, etc.)
    /// </summary>
    public string DashboardType { get; set; } = string.Empty;

    /// <summary>
    /// Dashboard description
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// User who owns this dashboard
    /// </summary>
public string OwnerId { get; set; } = string.Empty;

    /// <summary>
    /// Is this a system dashboard (read-only)
    /// </summary>
    public bool IsSystemDashboard { get; set; }

    /// <summary>
    /// Dashboard configuration (serialized JSON)
    /// </summary>
    public string? ConfigurationJson { get; set; }

    /// <summary>
    /// Dashboard widgets
    /// </summary>
    public ICollection<DashboardWidget> Widgets { get; set; } = new List<DashboardWidget>();

    /// <summary>
    /// Last modified by
    /// </summary>
    public string? LastModifiedBy { get; set; }
}

/// <summary>
/// Dashboard widget entity
/// </summary>
public class DashboardWidget : BaseEntity
{
  /// <summary>
    /// Dashboard ID this widget belongs to
    /// </summary>
    public string DashboardId { get; set; } = string.Empty;

    /// <summary>
    /// Dashboard entity
    /// </summary>
    public Dashboard? Dashboard { get; set; }

    /// <summary>
    /// Widget type (Chart, Table, KPI, Gauge, etc.)
    /// </summary>
    public string WidgetType { get; set; } = string.Empty;

    /// <summary>
  /// Widget title
    /// </summary>
    public string WidgetTitle { get; set; } = string.Empty;

 /// <summary>
    /// Report type this widget displays
    /// </summary>
    public string ReportType { get; set; } = string.Empty;

    /// <summary>
    /// Widget position/order
    /// </summary>
    public int Position { get; set; }

/// <summary>
    /// Widget size (Small, Medium, Large)
    /// </summary>
    public string WidgetSize { get; set; } = "Medium";

    /// <summary>
    /// Widget configuration (serialized)
    /// </summary>
    public string? ConfigurationJson { get; set; }

    /// <summary>
    /// Refresh interval in seconds
    /// </summary>
    public int RefreshIntervalSeconds { get; set; } = 300;

    /// <summary>
    /// Is widget enabled
    /// </summary>
public bool IsEnabled { get; set; } = true;
}

/// <summary>
/// Scheduled report entity
/// </summary>
public class ScheduledReport : BaseEntity
{
    /// <summary>
    /// Scheduled report name
    /// </summary>
    public string ScheduleName { get; set; } = string.Empty;

    /// <summary>
    /// Report type to schedule
    /// </summary>
    public string ReportType { get; set; } = string.Empty;

    /// <summary>
    /// Frequency (Daily, Weekly, Monthly)
    /// </summary>
 public string Frequency { get; set; } = "Weekly";

    /// <summary>
    /// Day of week for weekly schedules
 /// </summary>
    public int? DayOfWeek { get; set; }

    /// <summary>
    /// Day of month for monthly schedules
    /// </summary>
    public int? DayOfMonth { get; set; }

    /// <summary>
    /// Hour of day to run (0-23)
    /// </summary>
    public int HourOfDay { get; set; }

    /// <summary>
    /// Minute of hour to run (0-59)
    /// </summary>
    public int MinuteOfHour { get; set; }

    /// <summary>
    /// Email recipients (comma-separated)
    /// </summary>
    public string EmailRecipients { get; set; } = string.Empty;

    /// <summary>
    /// Export format (CSV, Excel, PDF)
    /// </summary>
    public string ExportFormat { get; set; } = "Excel";

    /// <summary>
    /// Filters to apply (serialized JSON)
    /// </summary>
    public string? FilterJson { get; set; }

    /// <summary>
    /// Owner of this scheduled report
    /// </summary>
    public string CreatedBy { get; set; } = "system";

    /// <summary>
    /// Last execution date
    /// </summary>
    public DateTime? LastExecutionDate { get; set; }

    /// <summary>
    /// Next scheduled execution date
    /// </summary>
    public DateTime? NextExecutionDate { get; set; }

 /// <summary>
    /// Is schedule enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Number of times executed
    /// </summary>
    public int ExecutionCount { get; set; }

    /// <summary>
    /// Last execution status
    /// </summary>
    public string? LastExecutionStatus { get; set; }
}

/// <summary>
/// Report template entity for creating reusable report configurations
/// </summary>
public class ReportTemplate : BaseEntity
{
    /// <summary>
    /// Template name
    /// </summary>
    public string TemplateName { get; set; } = string.Empty;

    /// <summary>
    /// Template description
    /// </summary>
public string? Description { get; set; }

    /// <summary>
    /// Report type this template creates
    /// </summary>
    public string ReportType { get; set; } = string.Empty;

    /// <summary>
    /// Default filters (serialized JSON)
    /// </summary>
    public string? DefaultFiltersJson { get; set; }

    /// <summary>
    /// Template configuration (serialized)
    /// </summary>
    public string? ConfigurationJson { get; set; }

    /// <summary>
    /// Usage count
    /// </summary>
    public int UsageCount { get; set; }

    /// <summary>
    /// Is this a system template (read-only)
    /// </summary>
    public bool IsSystemTemplate { get; set; }

    /// <summary>
    /// Created by
    /// </summary>
    public string CreatedBy { get; set; } = "system";
}
