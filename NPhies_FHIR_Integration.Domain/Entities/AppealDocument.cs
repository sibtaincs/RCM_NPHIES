namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Supporting documents for appeals
/// </summary>
public class AppealDocument
{
    public Guid Id { get; set; }
    public Guid AppealId { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string DocumentName { get; set; } = string.Empty;
    public string DocumentTitle { get; set; } = string.Empty;
    public string? DocumentDescription { get; set; }
    public string DocumentUrl { get; set; } = string.Empty;
    public string? FilePath { get; set; }
    public long DocumentSize { get; set; }
    public long FileSizeBytes { get; set; }
    public string? MimeType { get; set; }
    public DateTime UploadedDate { get; set; }
    public DateTime AttachedDate { get; set; }
    public string? UploadedBy { get; set; }
    public bool IsVerified { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    // Navigation property
    public virtual Appeal Appeal { get; set; } = null!;
}
