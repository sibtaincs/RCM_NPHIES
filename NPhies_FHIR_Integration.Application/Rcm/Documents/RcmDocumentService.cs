using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Rcm.Documents;

/// <summary>
/// Claim document store: PDFs and images uploaded by users or pushed by the HIS, held outside the claim rows and
/// embedded into supportingInfo valueAttachment only when the bundle is built. Documents are selected per claim by
/// patient, encounter, invoice and service date (long inpatient stays are split across interim claims) and every
/// claim is checked against the NPHIES 10 MB message limit.
/// </summary>
public interface IRcmDocumentService
{
    Task<ClaimDocumentDto> UploadAsync(DocumentMetadataDto metadata, string fileName, string? contentType, byte[] content,
        string? actor, string? providerScope, CancellationToken ct = default);

    /// <summary>HIS push; returns the existing document (Duplicate = true) when the ExternalId is already stored.</summary>
    Task<ClaimDocumentDto> IngestAsync(DocumentIngestDto request, string? actor, string? providerScope, CancellationToken ct = default);

    Task<RcmPagedResult<ClaimDocumentDto>> ListAsync(DocumentQuery query, CancellationToken ct = default);
    Task<ClaimDocumentDto?> GetAsync(string id, string? providerScope, CancellationToken ct = default);
    Task<DocumentContent?> DownloadAsync(string id, bool original, string? providerScope, CancellationToken ct = default);
    Task DeleteAsync(string id, string? providerScope, CancellationToken ct = default);

    Task<ClaimSizeBudgetDto> AttachToClaimAsync(string claimId, AttachDocumentsDto request, string? providerScope, CancellationToken ct = default);
    Task<ClaimSizeBudgetDto> DetachFromClaimAsync(string claimId, string documentId, string? providerScope, CancellationToken ct = default);

    /// <summary>Documents of the claim's patient that belong to its encounter or fall in its billable period.</summary>
    Task<List<ClaimDocumentDto>> GetCandidatesAsync(string claimId, string? providerScope, CancellationToken ct = default);

    Task<ClaimSizeBudgetDto> GetSizeBudgetAsync(string claimId, string? providerScope, CancellationToken ct = default);

    /// <summary>Moves inline base64 attachments (ClaimSupportingInfo.AttachmentData) into the document store.</summary>
    Task<DocumentMigrationResultDto> MigrateInlineAttachmentsAsync(int max, string? providerScope, CancellationToken ct = default);
}

public sealed record DocumentContent(string FileName, string ContentType, byte[] Data);

public sealed class RcmDocumentService : IRcmDocumentService
{
    public static readonly string[] DocumentTypes =
    {
        "lab-report", "radiology-report", "discharge-summary", "operative-report", "medical-report", "prescription", "invoice", "consent", "other",
    };

    private readonly ApplicationDbContext _db;
    private readonly IClaimDocumentStorage _storage;
    private readonly IPdfCompressor _compressor;
    private readonly RcmDocumentOptions _options;

    public RcmDocumentService(ApplicationDbContext db, IClaimDocumentStorage storage, IPdfCompressor compressor, IOptions<RcmDocumentOptions> options)
    {
        _db = db;
        _storage = storage;
        _compressor = compressor;
        _options = options.Value;
    }

    // ===================== Store =====================

    public async Task<ClaimDocumentDto> UploadAsync(DocumentMetadataDto metadata, string fileName, string? contentType, byte[] content,
        string? actor, string? providerScope, CancellationToken ct = default)
    {
        var doc = await StoreAsync(metadata, fileName, contentType, content, "Upload", null, actor, providerScope, ct);
        return (await GetAsync(doc.Id, providerScope, ct))!;
    }

    public async Task<ClaimDocumentDto> IngestAsync(DocumentIngestDto request, string? actor, string? providerScope, CancellationToken ct = default)
    {
        byte[] content;
        try { content = Convert.FromBase64String(request.ContentBase64 ?? string.Empty); }
        catch (FormatException) { throw RcmWorkflowException.Invalid("contentBase64 is not valid base64."); }

        if (!string.IsNullOrWhiteSpace(request.ExternalId))
        {
            var provider = await ResolveProviderAsync(request, providerScope, ct);
            var existing = await _db.ClaimDocuments.AsNoTracking()
                .FirstOrDefaultAsync(d => d.ProviderId == provider && d.ExternalId == request.ExternalId, ct);
            if (existing is not null)
            {
                var dto = (await GetAsync(existing.Id, providerScope, ct))!;
                dto.Duplicate = true;
                return dto;
            }
        }

        var doc = await StoreAsync(request, request.FileName, request.ContentType, content, "His", Blank(request.ExternalId), actor, providerScope, ct);
        return (await GetAsync(doc.Id, providerScope, ct))!;
    }

    private async Task<ClaimDocument> StoreAsync(DocumentMetadataDto m, string fileName, string? contentType, byte[] content, string source,
        string? externalId, string? actor, string? providerScope, CancellationToken ct)
    {
        if (content.Length == 0) throw RcmWorkflowException.Invalid("The document is empty.");
        if (content.LongLength > _options.MaxUploadBytes)
            throw RcmWorkflowException.Invalid($"The document is {Mb(content.LongLength)}; the maximum is {Mb(_options.MaxUploadBytes)}.");
        contentType = NormalizeContentType(contentType, fileName, content);
        if (!_options.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
            throw RcmWorkflowException.Invalid($"Content type '{contentType}' is not allowed ({string.Join(", ", _options.AllowedContentTypes)}).");
        var docType = string.IsNullOrWhiteSpace(m.DocumentType) ? "other" : m.DocumentType!.Trim().ToLowerInvariant();
        if (!DocumentTypes.Contains(docType)) throw RcmWorkflowException.Invalid($"Unknown document type '{m.DocumentType}' ({string.Join(", ", DocumentTypes)}).");

        var providerId = await ResolveProviderAsync(m, providerScope, ct);
        if (string.IsNullOrWhiteSpace(m.PatientId) && !string.IsNullOrWhiteSpace(m.ClaimId))
            m.PatientId = await _db.Claims.Where(c => c.Id == m.ClaimId).Select(c => c.PatientId).FirstOrDefaultAsync(ct) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(m.PatientId) && !string.IsNullOrWhiteSpace(m.EncounterId))
            m.PatientId = await _db.Encounters.Where(e => e.Id == m.EncounterId).Select(e => e.PatientId).FirstOrDefaultAsync(ct) ?? string.Empty;
        if (string.IsNullOrWhiteSpace(m.PatientId)) throw RcmWorkflowException.Invalid("patientId is required (or pass claimId / encounterId).");
        if (!await _db.Patients.AnyAsync(p => p.Id == m.PatientId, ct)) throw RcmWorkflowException.NotFound("Patient", m.PatientId);

        var doc = new ClaimDocument
        {
            ProviderId = providerId,
            PatientId = m.PatientId,
            EncounterNumber = Blank(m.EncounterNumber),
            EncounterType = Blank(m.EncounterType)?.ToUpperInvariant(),
            InvoiceNumber = Blank(m.InvoiceNumber),
            InvoiceDate = m.InvoiceDate,
            ServiceDate = m.ServiceDate,
            DocumentType = docType,
            Title = Blank(m.Title) ?? Path.GetFileNameWithoutExtension(fileName),
            FileName = SafeFileName(fileName, contentType),
            ContentType = contentType,
            Source = source,
            ExternalId = externalId,
            Notes = Blank(m.Notes),
            CreatedBy = actor,
        };
        await ResolveEncounterAsync(doc, m.EncounterId, ct);

        var stored = content;
        if (contentType == "application/pdf" && await _compressor.CompressAsync(content, ct) is { } smaller)
        {
            stored = smaller;
            doc.IsCompressed = true;
        }

        var keyBase = $"{DateTime.UtcNow:yyyy}/{DateTime.UtcNow:MM}/{doc.Id}";
        doc.StorageKey = keyBase;
        doc.StorageProvider = await _storage.WriteAsync(keyBase, stored, ct);
        if (doc.IsCompressed)
        {
            doc.OriginalStorageKey = keyBase + ".orig";
            await _storage.WriteAsync(doc.OriginalStorageKey, content, ct);
        }
        doc.SizeBytes = stored.LongLength;
        doc.OriginalSizeBytes = content.LongLength;
        doc.Sha256 = ClaimDocumentStorage.Sha256Hex(stored);

        _db.ClaimDocuments.Add(doc);
        await _db.SaveChangesAsync(ct);

        if (!string.IsNullOrWhiteSpace(m.ClaimId))
            await AttachToClaimAsync(m.ClaimId!, new AttachDocumentsDto { DocumentIds = { doc.Id } }, providerScope, ct);
        return doc;
    }

    private async Task<string> ResolveProviderAsync(DocumentMetadataDto m, string? providerScope, CancellationToken ct)
    {
        var provider = Blank(m.ProviderId);
        if (provider is null && !string.IsNullOrWhiteSpace(m.ClaimId))
            provider = await _db.Claims.Where(c => c.Id == m.ClaimId).Select(c => c.ProviderId).FirstOrDefaultAsync(ct);
        if (provider is null && !string.IsNullOrWhiteSpace(m.EncounterId))
            provider = await _db.Encounters.Where(e => e.Id == m.EncounterId).Select(e => e.ServiceProviderId).FirstOrDefaultAsync(ct);
        provider ??= providerScope;
        if (provider is null) throw RcmWorkflowException.Invalid("providerId is required (or pass claimId / encounterId).");
        if (providerScope != null && provider != providerScope) throw RcmWorkflowException.Invalid("You can only store documents for your own facility.");
        return provider;
    }

    private async Task ResolveEncounterAsync(ClaimDocument doc, string? encounterId, CancellationToken ct)
    {
        Encounter? enc = null;
        if (!string.IsNullOrWhiteSpace(encounterId))
        {
            enc = await _db.Encounters.AsNoTracking().FirstOrDefaultAsync(e => e.Id == encounterId, ct)
                  ?? throw RcmWorkflowException.NotFound("Encounter", encounterId!);
            if (enc.PatientId != doc.PatientId) throw RcmWorkflowException.Invalid("The encounter belongs to a different patient.");
        }
        else if (doc.EncounterNumber is { } number)
        {
            enc = await _db.Encounters.AsNoTracking()
                .Where(e => e.PatientId == doc.PatientId && (e.IdentifierValue == number || e.EncounterId == number))
                .OrderByDescending(e => e.PeriodStart).FirstOrDefaultAsync(ct);
        }
        if (enc is null) return;
        doc.EncounterId = enc.Id;
        doc.EncounterNumber ??= Blank(enc.IdentifierValue) ?? Blank(enc.EncounterId);
        doc.EncounterType ??= Blank(enc.Class)?.ToUpperInvariant();
        doc.ServiceDate ??= enc.PeriodStart;
    }

    // ===================== Query =====================

    public async Task<RcmPagedResult<ClaimDocumentDto>> ListAsync(DocumentQuery query, CancellationToken ct = default)
    {
        var q = _db.ClaimDocuments.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.ProviderId)) q = q.Where(d => d.ProviderId == query.ProviderId);
        if (!string.IsNullOrWhiteSpace(query.PatientId)) q = q.Where(d => d.PatientId == query.PatientId);
        if (!string.IsNullOrWhiteSpace(query.EncounterId)) q = q.Where(d => d.EncounterId == query.EncounterId);
        if (!string.IsNullOrWhiteSpace(query.EncounterNumber)) q = q.Where(d => d.EncounterNumber == query.EncounterNumber);
        if (!string.IsNullOrWhiteSpace(query.EncounterType)) q = q.Where(d => d.EncounterType == query.EncounterType!.ToUpper());
        if (!string.IsNullOrWhiteSpace(query.InvoiceNumber)) q = q.Where(d => d.InvoiceNumber == query.InvoiceNumber);
        if (query.InvoiceFrom.HasValue) q = q.Where(d => d.InvoiceDate >= query.InvoiceFrom);
        if (query.InvoiceTo.HasValue) q = q.Where(d => d.InvoiceDate <= query.InvoiceTo);
        // Undated documents (e.g. uploaded straight onto a claim) fall back to invoice date, then upload date.
        if (query.From.HasValue) q = q.Where(d => (d.ServiceDate ?? d.InvoiceDate ?? d.CreatedAt) >= query.From.Value.Date);
        if (query.To.HasValue) q = q.Where(d => (d.ServiceDate ?? d.InvoiceDate ?? d.CreatedAt) < query.To.Value.Date.AddDays(1));
        if (!string.IsNullOrWhiteSpace(query.DocumentType)) q = q.Where(d => d.DocumentType == query.DocumentType);
        if (!string.IsNullOrWhiteSpace(query.ClaimId))
            q = q.Where(d => _db.ClaimSupportingInfos.Any(s => s.ClaimId == query.ClaimId && s.DocumentId == d.Id));
        if (query.Unattached == true) q = q.Where(d => !_db.ClaimSupportingInfos.Any(s => s.DocumentId == d.Id));
        if (query.Unattached == false) q = q.Where(d => _db.ClaimSupportingInfos.Any(s => s.DocumentId == d.Id));
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search!.Trim();
            q = q.Where(d => (d.Title != null && d.Title.Contains(s)) || d.FileName.Contains(s)
                             || (d.InvoiceNumber != null && d.InvoiceNumber.Contains(s)) || (d.EncounterNumber != null && d.EncounterNumber.Contains(s)));
        }

        var total = await q.CountAsync(ct);
        var page = await q.OrderByDescending(d => d.ServiceDate ?? d.CreatedAt).ThenByDescending(d => d.CreatedAt)
            .Skip(query.Skip).Take(query.Take).ToListAsync(ct);
        return new RcmPagedResult<ClaimDocumentDto>
        {
            Items = await ToDtosAsync(page, ct),
            Total = total,
            Page = Math.Max(1, query.Page),
            PageSize = query.Take,
        };
    }

    public async Task<ClaimDocumentDto?> GetAsync(string id, string? providerScope, CancellationToken ct = default)
    {
        var doc = await _db.ClaimDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc is null || !InScope(doc, providerScope)) return null;
        return (await ToDtosAsync(new List<ClaimDocument> { doc }, ct))[0];
    }

    public async Task<DocumentContent?> DownloadAsync(string id, bool original, string? providerScope, CancellationToken ct = default)
    {
        var doc = await _db.ClaimDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc is null || !InScope(doc, providerScope)) return null;
        var key = original && doc.OriginalStorageKey != null ? doc.OriginalStorageKey : doc.StorageKey;
        var data = await _storage.ReadAsync(doc.StorageProvider, key, ct)
                   ?? throw RcmWorkflowException.Conflict($"The content of document '{id}' is missing from the {doc.StorageProvider} store.");
        return new DocumentContent(doc.FileName, doc.ContentType, data);
    }

    public async Task DeleteAsync(string id, string? providerScope, CancellationToken ct = default)
    {
        var doc = await _db.ClaimDocuments.FirstOrDefaultAsync(d => d.Id == id, ct);
        if (doc is null || !InScope(doc, providerScope)) throw RcmWorkflowException.NotFound("Document", id);
        var claims = await _db.ClaimSupportingInfos.Where(s => s.DocumentId == id).Select(s => s.Claim!.ClaimNumber).Distinct().ToListAsync(ct);
        if (claims.Count > 0)
            throw RcmWorkflowException.Conflict($"The document is attached to claim(s) {string.Join(", ", claims)}; detach it first.");
        await _storage.DeleteAsync(doc.StorageProvider, doc.StorageKey, ct);
        if (doc.OriginalStorageKey != null) await _storage.DeleteAsync(doc.StorageProvider, doc.OriginalStorageKey, ct);
        _db.ClaimDocuments.Remove(doc);
        await _db.SaveChangesAsync(ct);
    }

    // ===================== Claim linkage =====================

    public async Task<ClaimSizeBudgetDto> AttachToClaimAsync(string claimId, AttachDocumentsDto request, string? providerScope, CancellationToken ct = default)
    {
        var ids = request.DocumentIds.Where(i => !string.IsNullOrWhiteSpace(i)).Distinct().ToList();
        if (ids.Count == 0) throw RcmWorkflowException.Invalid("documentIds is required.");
        var claim = await LoadEditableClaimAsync(claimId, providerScope, ct);
        var docs = await _db.ClaimDocuments.AsNoTracking().Where(d => ids.Contains(d.Id)).ToListAsync(ct);
        var errors = new List<string>();
        foreach (var id in ids)
        {
            var d = docs.FirstOrDefault(x => x.Id == id);
            if (d is null || !InScope(d, providerScope)) errors.Add($"Document '{id}' was not found.");
            else if (d.PatientId != claim.PatientId) errors.Add($"Document '{d.Title ?? d.FileName}' belongs to a different patient.");
            else if (d.ProviderId != claim.ProviderId) errors.Add($"Document '{d.Title ?? d.FileName}' belongs to a different facility.");
        }
        if (errors.Count > 0) throw RcmWorkflowException.Invalid("Some documents cannot be attached.", errors);

        var category = string.IsNullOrWhiteSpace(request.Category) ? "attachment" : request.Category!.Trim();
        var next = claim.SupportingInfo.Select(s => s.Sequence).DefaultIfEmpty(0).Max();
        foreach (var d in ids.Select(id => docs.First(x => x.Id == id)))
        {
            if (claim.SupportingInfo.Any(s => s.DocumentId == d.Id)) continue;
            var info = new ClaimSupportingInfo
            {
                ClaimId = claim.Id,
                Sequence = ++next,
                Category = category,
                AttachmentTitle = Truncate(d.Title ?? d.FileName, 255),
                AttachmentContentType = d.ContentType,
                DocumentId = d.Id,
            };
            claim.SupportingInfo.Add(info);
            _db.ClaimSupportingInfos.Add(info);
        }
        claim.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await BudgetAsync(claim.Id, ct);
    }

    public async Task<ClaimSizeBudgetDto> DetachFromClaimAsync(string claimId, string documentId, string? providerScope, CancellationToken ct = default)
    {
        var claim = await LoadEditableClaimAsync(claimId, providerScope, ct);
        var infos = claim.SupportingInfo.Where(s => s.DocumentId == documentId).ToList();
        if (infos.Count == 0) throw RcmWorkflowException.NotFound("Attached document", documentId);
        foreach (var info in infos)
        {
            claim.SupportingInfo.Remove(info);
            _db.ClaimSupportingInfos.Remove(info);
        }
        claim.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return await BudgetAsync(claim.Id, ct);
    }

    public async Task<List<ClaimDocumentDto>> GetCandidatesAsync(string claimId, string? providerScope, CancellationToken ct = default)
    {
        var claim = await _db.Claims.AsNoTracking().FirstOrDefaultAsync(c => c.Id == claimId, ct);
        if (claim is null || (providerScope != null && claim.ProviderId != providerScope)) throw RcmWorkflowException.NotFound("Claim", claimId);
        var enc = claim.EncounterId is null ? null : await _db.Encounters.AsNoTracking().FirstOrDefaultAsync(e => e.Id == claim.EncounterId, ct);
        var numbers = new[] { enc?.IdentifierValue, enc?.EncounterId }.Where(n => !string.IsNullOrWhiteSpace(n)).Select(n => n!).ToList();
        var start = (claim.BillablePeriodStart ?? claim.ServicedPeriodStart ?? enc?.PeriodStart)?.Date;
        var end = (claim.BillablePeriodEnd ?? claim.ServicedPeriodEnd ?? enc?.PeriodEnd)?.Date.AddDays(1);

        var q = _db.ClaimDocuments.AsNoTracking().Where(d => d.PatientId == claim.PatientId && d.ProviderId == claim.ProviderId);
        var encId = enc?.Id;
        q = q.Where(d => (encId != null && d.EncounterId == encId)
                         || (d.EncounterNumber != null && numbers.Contains(d.EncounterNumber))
                         || (d.EncounterId == null && d.EncounterNumber == null && d.ServiceDate == null)
                         || (d.EncounterId == null && d.EncounterNumber == null && d.ServiceDate != null
                             && (start == null || d.ServiceDate >= start) && (end == null || d.ServiceDate < end)));
        var docs = await q.OrderBy(d => d.ServiceDate ?? d.CreatedAt).Take(500).ToListAsync(ct);
        var dtos = await ToDtosAsync(docs, ct);
        // Undated documents without an encounter cannot be placed in the period: offered, but not pre-selected.
        foreach (var dto in dtos)
            dto.InClaimPeriod = dto.ServiceDate is null
                ? (encId != null && dto.EncounterId == encId) || (dto.EncounterNumber != null && numbers.Contains(dto.EncounterNumber))
                : (start == null || dto.ServiceDate >= start) && (end == null || dto.ServiceDate < end);
        return dtos;
    }

    public async Task<ClaimSizeBudgetDto> GetSizeBudgetAsync(string claimId, string? providerScope, CancellationToken ct = default)
    {
        var claim = await _db.Claims.AsNoTracking().Select(c => new { c.Id, c.ProviderId }).FirstOrDefaultAsync(c => c.Id == claimId, ct);
        if (claim is null || (providerScope != null && claim.ProviderId != providerScope)) throw RcmWorkflowException.NotFound("Claim", claimId);
        return await BudgetAsync(claimId, ct);
    }

    private async Task<ClaimSizeBudgetDto> BudgetAsync(string claimId, CancellationToken ct)
    {
        // Only the length of inline base64 matters: avoid pulling it into memory.
        var rows = await _db.ClaimSupportingInfos.AsNoTracking().Where(s => s.ClaimId == claimId)
            .Select(s => new { s.Sequence, s.AttachmentTitle, s.DocumentId, InlineLength = s.AttachmentData == null ? 0 : s.AttachmentData.Length })
            .ToListAsync(ct);
        var infos = rows.Select(r => new ClaimSupportingInfo
        {
            ClaimId = claimId,
            Sequence = r.Sequence,
            AttachmentTitle = r.AttachmentTitle,
            DocumentId = r.DocumentId,
            AttachmentData = r.InlineLength == 0 ? null : new string('A', r.InlineLength),
        }).ToList();
        var sizes = await LoadDocumentSizesAsync(_db, infos, ct);
        return ComputeBudget(claimId, infos, sizes, _options);
    }

    /// <summary>Stored size of every document referenced by <paramref name="infos"/>.</summary>
    public static async Task<Dictionary<string, long>> LoadDocumentSizesAsync(ApplicationDbContext db, IEnumerable<ClaimSupportingInfo> infos, CancellationToken ct)
    {
        var ids = infos.Where(s => s.DocumentId != null).Select(s => s.DocumentId!).Distinct().ToList();
        if (ids.Count == 0) return new Dictionary<string, long>();
        return await db.ClaimDocuments.AsNoTracking().Where(d => ids.Contains(d.Id)).ToDictionaryAsync(d => d.Id, d => d.SizeBytes, ct);
    }

    /// <summary>Estimates the NPHIES message size contributed by a claim's attachments (base64 grows content by 4/3).</summary>
    public static ClaimSizeBudgetDto ComputeBudget(string claimId, IEnumerable<ClaimSupportingInfo> infos, IReadOnlyDictionary<string, long> documentSizes, RcmDocumentOptions options)
    {
        var budget = new ClaimSizeBudgetDto { ClaimId = claimId, LimitBytes = options.MaxMessageBytes, WarnBytes = options.WarnMessageBytes };
        foreach (var s in infos.OrderBy(s => s.Sequence))
        {
            if (s.DocumentId != null && documentSizes.TryGetValue(s.DocumentId, out var size))
                budget.Attachments.Add(new ClaimSizeBudgetItemDto
                {
                    Sequence = s.Sequence, DocumentId = s.DocumentId, Title = s.AttachmentTitle, RawBytes = size,
                    EncodedBytes = ClaimDocumentStorage.EncodedSize(size), Source = "document",
                });
            else if (!string.IsNullOrEmpty(s.AttachmentData))
                budget.Attachments.Add(new ClaimSizeBudgetItemDto
                {
                    Sequence = s.Sequence, Title = s.AttachmentTitle, RawBytes = s.AttachmentData.Length / 4L * 3,
                    EncodedBytes = s.AttachmentData.Length, Source = "inline",
                });
        }
        budget.AttachmentCount = budget.Attachments.Count;
        budget.RawAttachmentBytes = budget.Attachments.Sum(a => a.RawBytes);
        budget.EncodedAttachmentBytes = budget.Attachments.Sum(a => a.EncodedBytes);
        budget.EstimatedMessageBytes = budget.EncodedAttachmentBytes + options.BundleOverheadBytes;
        budget.RemainingBytes = Math.Max(0, options.MaxMessageBytes - budget.EstimatedMessageBytes);
        budget.Status = budget.EstimatedMessageBytes > options.MaxMessageBytes ? "over-limit"
            : budget.EstimatedMessageBytes > options.WarnMessageBytes ? "warning" : "ok";
        return budget;
    }

    // ===================== Migration =====================

    public async Task<DocumentMigrationResultDto> MigrateInlineAttachmentsAsync(int max, string? providerScope, CancellationToken ct = default)
    {
        var result = new DocumentMigrationResultDto();
        var q = _db.ClaimSupportingInfos.Where(s => s.AttachmentData != null && s.DocumentId == null);
        if (providerScope != null) q = q.Where(s => s.Claim!.ProviderId == providerScope);
        var ids = await q.OrderBy(s => s.CreatedAt).Select(s => s.Id).Take(Math.Clamp(max, 1, 1000)).ToListAsync(ct);
        foreach (var id in ids)
        {
            result.Scanned++;
            var info = await _db.ClaimSupportingInfos.Include(s => s.Claim).FirstAsync(s => s.Id == id, ct);
            byte[] data;
            try { data = Convert.FromBase64String(info.AttachmentData!); }
            catch (FormatException)
            {
                result.Skipped++;
                result.Errors.Add($"Claim {info.Claim?.ClaimNumber} supportingInfo #{info.Sequence}: invalid base64.");
                continue;
            }
            var claim = info.Claim!;
            var contentType = string.IsNullOrWhiteSpace(info.AttachmentContentType) ? "application/pdf" : info.AttachmentContentType!;
            var doc = new ClaimDocument
            {
                ProviderId = claim.ProviderId,
                PatientId = claim.PatientId,
                ServiceDate = info.DateValue ?? claim.ServicedPeriodStart ?? claim.BillablePeriodStart ?? claim.CreatedDate,
                DocumentType = "other",
                Title = info.AttachmentTitle,
                FileName = SafeFileName(info.AttachmentTitle ?? $"claim-{claim.ClaimNumber}-{info.Sequence}", contentType),
                ContentType = contentType,
                Source = "Migrated",
            };
            await ResolveEncounterAsync(doc, claim.EncounterId, ct);
            doc.StorageKey = $"{doc.CreatedAt:yyyy}/{doc.CreatedAt:MM}/{doc.Id}";
            doc.StorageProvider = await _storage.WriteAsync(doc.StorageKey, data, ct);
            doc.SizeBytes = doc.OriginalSizeBytes = data.LongLength;
            doc.Sha256 = ClaimDocumentStorage.Sha256Hex(data);
            _db.ClaimDocuments.Add(doc);
            info.DocumentId = doc.Id;
            info.AttachmentData = null;
            info.AttachmentContentType = contentType;
            await _db.SaveChangesAsync(ct);
            _db.ChangeTracker.Clear();
            result.Migrated++;
            result.BytesMoved += data.LongLength;
        }
        return result;
    }

    // ===================== Helpers =====================

    private async Task<Claim> LoadEditableClaimAsync(string claimId, string? providerScope, CancellationToken ct)
    {
        var claim = await _db.Claims.Include(c => c.SupportingInfo).FirstOrDefaultAsync(c => c.Id == claimId, ct);
        if (claim is null || (providerScope != null && claim.ProviderId != providerScope)) throw RcmWorkflowException.NotFound("Claim", claimId);
        if (!RcmClaimEditorService.IsEditable(claim))
            throw RcmWorkflowException.Conflict($"Claim {claim.ClaimNumber} is {claim.NphiesStatus}; documents can only change on draft or rejected-before-send claims. Use a resubmission or a communication instead.");
        return claim;
    }

    private async Task<List<ClaimDocumentDto>> ToDtosAsync(List<ClaimDocument> docs, CancellationToken ct)
    {
        if (docs.Count == 0) return new List<ClaimDocumentDto>();
        var ids = docs.Select(d => d.Id).ToList();
        var patientIds = docs.Select(d => d.PatientId).Distinct().ToList();
        var names = await _db.Patients.AsNoTracking().Where(p => patientIds.Contains(p.Id))
            .Select(p => new { p.Id, p.FullName, p.FirstName, p.LastName }).ToListAsync(ct);
        var links = await _db.ClaimSupportingInfos.AsNoTracking().Where(s => s.DocumentId != null && ids.Contains(s.DocumentId))
            .Select(s => new { s.DocumentId, s.ClaimId, s.Sequence, s.Claim!.ClaimNumber, s.Claim.NphiesStatus }).ToListAsync(ct);
        return docs.Select(d =>
        {
            var p = names.FirstOrDefault(n => n.Id == d.PatientId);
            return new ClaimDocumentDto
            {
                Id = d.Id,
                ProviderId = d.ProviderId,
                PatientId = d.PatientId,
                PatientName = p is null ? null : (string.IsNullOrWhiteSpace(p.FullName) ? $"{p.FirstName} {p.LastName}".Trim() : p.FullName),
                EncounterId = d.EncounterId,
                EncounterNumber = d.EncounterNumber,
                EncounterType = d.EncounterType,
                InvoiceNumber = d.InvoiceNumber,
                InvoiceDate = d.InvoiceDate,
                ServiceDate = d.ServiceDate,
                DocumentType = d.DocumentType,
                Title = d.Title,
                FileName = d.FileName,
                ContentType = d.ContentType,
                SizeBytes = d.SizeBytes,
                OriginalSizeBytes = d.OriginalSizeBytes,
                EncodedSizeBytes = ClaimDocumentStorage.EncodedSize(d.SizeBytes),
                IsCompressed = d.IsCompressed,
                Sha256 = d.Sha256,
                StorageProvider = d.StorageProvider,
                Source = d.Source,
                ExternalId = d.ExternalId,
                Notes = d.Notes,
                CreatedBy = d.CreatedBy,
                CreatedAt = d.CreatedAt,
                Claims = links.Where(l => l.DocumentId == d.Id).Select(l => new DocumentClaimLinkDto
                {
                    ClaimId = l.ClaimId,
                    ClaimNumber = l.ClaimNumber,
                    NphiesStatus = l.NphiesStatus,
                    Sequence = l.Sequence,
                    Sent = !RcmClaimEditorService.EditableStatuses.Contains(l.NphiesStatus ?? RcmClaimStatus.Draft),
                }).ToList(),
            };
        }).ToList();
    }

    private static bool InScope(ClaimDocument d, string? providerScope) => providerScope is null || d.ProviderId == providerScope;

    private static string NormalizeContentType(string? contentType, string fileName, byte[] content)
    {
        var ct = (contentType ?? string.Empty).Split(';')[0].Trim().ToLowerInvariant();
        if (ct is "" or "application/octet-stream")
        {
            if (content.Length >= 4 && content[0] == 0x25 && content[1] == 0x50 && content[2] == 0x44 && content[3] == 0x46) return "application/pdf";
            ct = Path.GetExtension(fileName).ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                _ => ct,
            };
        }
        return ct is "image/jpg" ? "image/jpeg" : ct;
    }

    private static string SafeFileName(string? name, string contentType)
    {
        var n = Path.GetFileName(name ?? string.Empty);
        n = Regex.Replace(n, @"[^\w\-. ]", "_").Trim();
        if (string.IsNullOrWhiteSpace(n)) n = "document";
        if (string.IsNullOrEmpty(Path.GetExtension(n)))
            n += contentType switch { "image/jpeg" => ".jpg", "image/png" => ".png", _ => ".pdf" };
        return Truncate(n, 255);
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
    private static string? Blank(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();
    private static string Mb(long bytes) => $"{bytes / 1024d / 1024d:0.0} MB";
}
