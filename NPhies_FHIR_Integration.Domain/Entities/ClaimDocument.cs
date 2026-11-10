namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// A clinical / billing document (normally a PDF) that can be attached to claims as a supportingInfo valueAttachment.
/// Only metadata lives here; the binary is held by the configured document content store (SQL table or file system)
/// under <see cref="StorageKey"/>. Indexed for selection by patient, encounter, invoice and service date so long
/// inpatient stays can be split across interim claims without exceeding the NPHIES 10 MB message limit.
/// </summary>
public class ClaimDocument : BaseEntity
{
    /// <summary>Provider organization (facility) that owns the document; used for tenant scoping.</summary>
    public string ProviderId { get; set; } = string.Empty;

    public string PatientId { get; set; } = string.Empty;
    public Patient? Patient { get; set; }

    /// <summary>Optional link to the encounter entity.</summary>
    public string? EncounterId { get; set; }

    /// <summary>HIS encounter / visit / admission number.</summary>
    public string? EncounterNumber { get; set; }

    /// <summary>Encounter class: IMP (inpatient), AMB (outpatient), EMER, SS (day case), HH, VR ...</summary>
    public string? EncounterType { get; set; }

    public string? InvoiceNumber { get; set; }
    public DateTime? InvoiceDate { get; set; }

    /// <summary>Clinical date of the document (report / result / discharge date).</summary>
    public DateTime? ServiceDate { get; set; }

    /// <summary>lab-report, radiology-report, discharge-summary, operative-report, medical-report, prescription, invoice, consent, other.</summary>
    public string DocumentType { get; set; } = "other";

    public string? Title { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = "application/pdf";

    /// <summary>Size of the stored (possibly compressed) content that is sent to NPHIES.</summary>
    public long SizeBytes { get; set; }

    /// <summary>Size of the file as received, before compression.</summary>
    public long OriginalSizeBytes { get; set; }

    public bool IsCompressed { get; set; }

    /// <summary>Hex SHA-256 of the stored content (dedupe and audit-log references).</summary>
    public string Sha256 { get; set; } = string.Empty;

    /// <summary>Content store that holds the binary: Sql or FileSystem.</summary>
    public string StorageProvider { get; set; } = string.Empty;
    public string StorageKey { get; set; } = string.Empty;

    /// <summary>Key of the uncompressed original, when compression produced a smaller copy.</summary>
    public string? OriginalStorageKey { get; set; }

    /// <summary>Upload, His or Migrated.</summary>
    public string Source { get; set; } = "Upload";

    /// <summary>Document id in the source system (HIS); makes HIS pushes idempotent.</summary>
    public string? ExternalId { get; set; }

    public string? Notes { get; set; }
    public string? CreatedBy { get; set; }
}

/// <summary>Binary content of a <see cref="ClaimDocument"/> when the SQL content store is used (kept out of the metadata table).</summary>
public class ClaimDocumentContent
{
    public string Key { get; set; } = string.Empty;
    public byte[] Data { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
