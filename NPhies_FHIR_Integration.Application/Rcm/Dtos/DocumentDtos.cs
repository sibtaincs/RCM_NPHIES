namespace NPhies_FHIR_Integration.Application.Rcm.Dtos;

/// <summary>
/// Document list filters. <see cref="RcmQuery.From"/>/<see cref="RcmQuery.To"/> filter on the service date;
/// invoice dates use <see cref="InvoiceFrom"/>/<see cref="InvoiceTo"/>.
/// </summary>
public sealed class DocumentQuery : RcmQuery
{
    public string? ClaimId { get; set; }
    public string? EncounterId { get; set; }
    public string? EncounterNumber { get; set; }
    public string? EncounterType { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceFrom { get; set; }
    public DateTime? InvoiceTo { get; set; }
    public string? DocumentType { get; set; }
    /// <summary>Only documents not attached to any claim.</summary>
    public bool? Unattached { get; set; }
    /// <summary>Free text over title, file name, invoice and encounter number.</summary>
    public string? Search { get; set; }
}

/// <summary>Metadata supplied with an upload (multipart form fields) or HIS push.</summary>
public class DocumentMetadataDto
{
    public string? ProviderId { get; set; }
    public string PatientId { get; set; } = string.Empty;
    public string? EncounterId { get; set; }
    public string? EncounterNumber { get; set; }
    public string? EncounterType { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? ServiceDate { get; set; }
    public string? DocumentType { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }
    /// <summary>Attach to this claim right after storing (claim must be editable).</summary>
    public string? ClaimId { get; set; }
}

/// <summary>HIS push: metadata plus base64 content. Idempotent on (provider, <see cref="ExternalId"/>).</summary>
public sealed class DocumentIngestDto : DocumentMetadataDto
{
    public string? ExternalId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";
    public string ContentBase64 { get; set; } = string.Empty;
}

public sealed class ClaimDocumentDto
{
    public string Id { get; set; } = string.Empty;
    public string ProviderId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public string? PatientName { get; set; }
    public string? EncounterId { get; set; }
    public string? EncounterNumber { get; set; }
    public string? EncounterType { get; set; }
    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }
    public DateTime? ServiceDate { get; set; }
    public string DocumentType { get; set; } = string.Empty;
    public string? Title { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public long OriginalSizeBytes { get; set; }
    /// <summary>Approximate bytes this document adds to an NPHIES message once base64 encoded.</summary>
    public long EncodedSizeBytes { get; set; }
    public bool IsCompressed { get; set; }
    public string Sha256 { get; set; } = string.Empty;
    public string StorageProvider { get; set; } = string.Empty;
    public string Source { get; set; } = string.Empty;
    public string? ExternalId { get; set; }
    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<DocumentClaimLinkDto> Claims { get; set; } = new();
    /// <summary>Candidates only: the service date falls inside the claim's billable period.</summary>
    public bool? InClaimPeriod { get; set; }
    /// <summary>Set on ingest when an existing document was returned (same ExternalId).</summary>
    public bool Duplicate { get; set; }
}

public sealed class DocumentClaimLinkDto
{
    public string ClaimId { get; set; } = string.Empty;
    public string? ClaimNumber { get; set; }
    public string? NphiesStatus { get; set; }
    public int Sequence { get; set; }
    public bool Sent { get; set; }
}

public sealed class AttachDocumentsDto
{
    public List<string> DocumentIds { get; set; } = new();
    /// <summary>supportingInfo category (default <c>attachment</c>).</summary>
    public string? Category { get; set; }
}

public sealed class ClaimSizeBudgetDto
{
    public string ClaimId { get; set; } = string.Empty;
    public int AttachmentCount { get; set; }
    public long RawAttachmentBytes { get; set; }
    public long EncodedAttachmentBytes { get; set; }
    /// <summary>Encoded attachments plus an allowance for the rest of the bundle.</summary>
    public long EstimatedMessageBytes { get; set; }
    public long WarnBytes { get; set; }
    public long LimitBytes { get; set; }
    public long RemainingBytes { get; set; }
    /// <summary>ok, warning or over-limit.</summary>
    public string Status { get; set; } = "ok";
    public List<ClaimSizeBudgetItemDto> Attachments { get; set; } = new();
}

public sealed class ClaimSizeBudgetItemDto
{
    public int Sequence { get; set; }
    public string? DocumentId { get; set; }
    public string? Title { get; set; }
    public long RawBytes { get; set; }
    public long EncodedBytes { get; set; }
    /// <summary>document or inline.</summary>
    public string Source { get; set; } = "document";
}

public sealed class DocumentMigrationResultDto
{
    public int Scanned { get; set; }
    public int Migrated { get; set; }
    public int Skipped { get; set; }
    public long BytesMoved { get; set; }
    public List<string> Errors { get; set; } = new();
}
