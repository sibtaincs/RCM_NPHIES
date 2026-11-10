namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Compliance audit record
/// </summary>
public class ComplianceAudit
{
    public Guid Id { get; set; }
    public string AuditNumber { get; set; } = string.Empty;
 public string AuditType { get; set; } = string.Empty;
    public DateTime FromDate { get; set; }
    public DateTime ToDate { get; set; }
    public decimal OverallCompliancePercentage { get; set; }
    public int TotalRecordsAudited { get; set; }
    public int CompliantRecords { get; set; }
    public int NonCompliantRecords { get; set; }
    public int CriticalIssuesCount { get; set; }
    public string? AuditResults { get; set; } // JSON
    public DateTime GeneratedDate { get; set; }
    public string? GeneratedBy { get; set; }
}
