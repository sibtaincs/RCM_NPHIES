using System.Globalization;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Rcm.Documents;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Rcm.Services;

/// <summary>
/// Claim / prior-authorization editor: creates and edits drafts (header, items, diagnoses, care team, supporting info,
/// vision lenses) with server-side recalculation, enforces the NPHIES lifecycle lock (only unsent / failed versions are
/// editable), runs the claim health rules, lists the valid correction paths per state and keeps a field-level audit trail.
/// </summary>
public interface IRcmClaimEditorService
{
    Task<ClaimEditDto> GetAsync(string claimId, string? providerScope, CancellationToken ct = default);
    Task<ClaimEditDto> CreateAsync(ClaimEditDto request, string? actor, string? providerScope, CancellationToken ct = default);
    Task<ClaimEditDto> UpdateAsync(string claimId, ClaimEditDto request, string? actor, string? providerScope, CancellationToken ct = default);
    Task<ClaimHealthDto> CheckHealthAsync(string claimId, string? providerScope, CancellationToken ct = default);
    Task<List<ClaimHealthDto>> CheckHealthBulkAsync(RcmClaimQuery query, bool problemsOnly, CancellationToken ct = default);
    Task<ClaimCorrectionOptionsDto> GetCorrectionOptionsAsync(string claimId, string? providerScope, CancellationToken ct = default);
    Task<List<ClaimAuditEntryDto>> GetAuditAsync(string claimId, string? providerScope, CancellationToken ct = default);
    Task<ClaimVersionCompareDto> CompareWithPreviousAsync(string claimId, string? providerScope, CancellationToken ct = default);
}

public sealed class RcmClaimEditorService : IRcmClaimEditorService
{
    public static readonly string[] EditableStatuses = { RcmClaimStatus.Draft, RcmClaimStatus.Error, RcmClaimStatus.ValidationFailed };
    private static readonly string[] ClaimTypes = { "institutional", "oral", "pharmacy", "professional", "vision" };
    private static readonly string[] SubTypes = { "op", "ip", "emr" };
    private const string AuditEntityType = "Claim";

    private readonly ApplicationDbContext _db;
    private readonly RcmDocumentOptions _documentOptions;

    public RcmClaimEditorService(ApplicationDbContext db, IOptions<RcmDocumentOptions>? documentOptions = null)
    {
        _db = db;
        _documentOptions = documentOptions?.Value ?? new RcmDocumentOptions();
    }

    public static bool IsEditable(Claim claim) => EditableStatuses.Contains(claim.NphiesStatus ?? RcmClaimStatus.Draft);

    // ===================== Read =====================

    public async Task<ClaimEditDto> GetAsync(string claimId, string? providerScope, CancellationToken ct = default)
    {
        var claim = await LoadScopedAsync(claimId, providerScope, ct);
        return await ToEditDtoAsync(claim, ct);
    }

    private async Task<ClaimEditDto> ToEditDtoAsync(Claim c, CancellationToken ct)
    {
        var itemIds = c.Items.Select(i => i.Id).ToList();
        var lenses = itemIds.Count == 0 ? new List<VisionPrescription>()
            : await _db.VisionPrescriptions.AsNoTracking().Where(v => itemIds.Contains(v.ClaimItemId)).ToListAsync(ct);
        var editable = IsEditable(c);
        var dto = new ClaimEditDto
        {
            Id = c.Id,
            ClaimNumber = c.ClaimNumber,
            Use = RcmInputMapper.IsPreAuth(c) ? "preauthorization" : "claim",
            ClaimType = c.ClaimType,
            ClaimSubType = c.ClaimSubType,
            Priority = c.Priority,
            PayeeType = c.PayeeType,
            PatientId = c.PatientId,
            CoverageId = c.CoverageId,
            ProviderId = c.ProviderId,
            InsurerId = c.InsurerId,
            PractitionerId = c.PractitionerId,
            LocationId = c.LocationId,
            EncounterId = c.EncounterId,
            BillablePeriodStart = c.BillablePeriodStart,
            BillablePeriodEnd = c.BillablePeriodEnd,
            ServicedPeriodStart = c.ServicedPeriodStart,
            ServicedPeriodEnd = c.ServicedPeriodEnd,
            PreAuthorizationRef = c.PreAuthorizationRef,
            EpisodeIdentifierValue = c.EpisodeIdentifierValue,
            ReferralIdentifier = c.ReferralIdentifier,
            PrescriptionIdentifier = c.PrescriptionIdentifier,
            AccidentDate = c.AccidentDate,
            AccidentType = c.AccidentType,
            EligibilityOfflineReference = c.EligibilityOfflineReference,
            EligibilityOfflineDate = c.EligibilityOfflineDate,
            AuthorizationOfflineDate = c.AuthorizationOfflineDate,
            IsShadowBilling = c.IsShadowBilling,
            IsTransfer = c.IsTransfer,
            IsNewborn = c.IsNewborn,
            CoverageHolderPatientId = c.Coverage?.PatientId,
            NphiesStatus = c.NphiesStatus ?? RcmClaimStatus.Draft,
            Editable = editable,
            LockedReason = editable ? null : LockReason(c),
            Total = c.Total,
            TotalTax = c.Items.Sum(i => i.Tax ?? 0m),
            TotalPatientShare = c.Items.Sum(i => i.PatientShare ?? 0m),
            TechnicalReviewStatus = c.TechnicalReviewStatus,
            MedicalReviewStatus = c.MedicalReviewStatus,
            Items = c.Items.OrderBy(i => i.Sequence).Select(i => new ClaimEditItemDto
            {
                Sequence = i.Sequence,
                ProductOrServiceCode = i.ProductOrServiceCode,
                ProductOrServiceSystem = i.ProductOrServiceSystem,
                ProductOrServiceDisplay = i.ProductOrServiceDisplay,
                AltProductOrServiceCode = i.AltProductOrServiceCode,
                ServicedDate = i.ServicedDate ?? i.ServicedPeriodStart,
                Quantity = i.Quantity ?? 1m,
                UnitPrice = i.UnitPrice ?? 0m,
                Factor = i.Factor,
                TaxRate = i.TaxRate,
                PatientShare = i.PatientShare,
                CareTeamSequence = i.CareTeamSequence,
                DiagnosisSequences = ParseSeq(i.DiagnosisSequence),
                InformationSequences = ParseSeq(i.InformationSequence),
                BodySiteCode = i.BodySiteCode,
                SubSiteCode = i.SubSiteCode,
                IsPackage = i.IsPackage,
                IsMaternity = i.IsMaternity,
                PatientInvoiceValue = i.PatientInvoiceValue,
                Notes = i.Notes,
                Tax = i.Tax ?? 0m,
                Net = i.Net ?? 0m,
                DetailCount = i.Details.Count,
                Lenses = lenses.Where(l => l.ClaimItemId == i.Id).Select(l => new ClaimEditLensDto
                {
                    Product = l.Product, Eye = l.Eye, Sphere = l.Sphere, Cylinder = l.Cylinder, Axis = l.Axis,
                    Prism = l.Prism, PrismBase = l.PrismBase, Add = l.Add, Power = l.Power,
                }).ToList(),
            }).ToList(),
            Diagnoses = c.Diagnoses.OrderBy(d => d.Sequence).Select(d => new ClaimEditDiagnosisDto
            {
                Sequence = d.Sequence,
                DiagnosisCode = d.DiagnosisCode,
                DiagnosisDisplay = d.DiagnosisDisplay,
                DiagnosisType = d.DiagnosisType,
                OnAdmissionCode = d.OnAdmissionCode,
                ConditionOnsetCode = d.ConditionOnsetCode,
            }).ToList(),
            CareTeam = c.CareTeam.OrderBy(m => m.Sequence).Select(m => new ClaimEditCareTeamDto
            {
                Sequence = m.Sequence,
                PractitionerId = m.PractitionerId,
                PractitionerName = m.Practitioner is null ? null : $"{m.Practitioner.FirstName} {m.Practitioner.LastName}".Trim(),
                Role = m.Role,
                Qualification = m.Qualification,
            }).ToList(),
            SupportingInfo = c.SupportingInfo.OrderBy(s => s.Sequence).Select(s => new ClaimEditSupportingInfoDto
            {
                Sequence = s.Sequence,
                Category = s.Category,
                CodeValue = s.CodeValue,
                CodeSystem = s.CodeSystem,
                StringValue = s.StringValue,
                QuantityValue = s.QuantityValue,
                QuantityUnit = s.QuantityUnit,
                DateValue = s.DateValue,
                BooleanValue = s.BooleanValue,
                AttachmentTitle = s.AttachmentTitle,
                AttachmentContentType = s.AttachmentContentType,
                AttachmentUrl = s.AttachmentUrl,
                DocumentId = s.DocumentId,
                HasAttachment = HasFile(s),
            }).ToList(),
            Related = c.RelatedClaims.Select(r => new RelatedClaimDto { ClaimId = r.ReferencedClaimId, Identifier = r.RelatedClaimIdentifierValue, Relationship = r.Relationship }).ToList(),
        };
        return dto;
    }

    private static string LockReason(Claim c)
    {
        var status = c.NphiesStatus ?? RcmClaimStatus.Draft;
        return status switch
        {
            RcmClaimStatus.Submitted or RcmClaimStatus.Queued or RcmClaimStatus.Pended
                => $"The {(RcmInputMapper.IsPreAuth(c) ? "prior authorization" : "claim")} is with the payer ({status}). Run a status-check, or cancel it and create a corrected version.",
            RcmClaimStatus.Timeout => "The last submission timed out and may have been delivered. Run a status-check before doing anything else.",
            RcmClaimStatus.CancelRequested => "A cancellation is in progress. Run a status-check.",
            RcmClaimStatus.Superseded => "A newer version replaced this one. Edit the newer version instead.",
            RcmClaimStatus.Cancelled or RcmClaimStatus.Nullified => $"The record is {status}. Create a new version (resubmit) if the service still needs to be billed.",
            RcmClaimStatus.Paid or RcmClaimStatus.PartiallyPaid => "The payer has paid this claim. Nullify it, or resubmit / appeal to dispute the amount.",
            RcmClaimStatus.Recouped => "The payer recouped the payment for this claim. Correct & resubmit or appeal to dispute it.",
            _ => RcmInputMapper.IsPreAuth(c)
                ? "The payer has decided this prior authorization. Use Update or Extend to send a new version."
                : "The payer has adjudicated this claim. Use 'Correct & resubmit' to send a new version linked to it.",
        };
    }

    // ===================== Create / update =====================

    public async Task<ClaimEditDto> CreateAsync(ClaimEditDto request, string? actor, string? providerScope, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(providerScope)) request.ProviderId = providerScope!;
        await ValidateReferencesAsync(request, ct);

        var preAuth = IsPreAuthUse(request.Use);
        var number = await NextClaimNumberAsync(preAuth, ct);
        var claim = new Claim
        {
            ClaimNumber = number,
            Status = "active",
            CreatedDate = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            NphiesStatus = RcmClaimStatus.Draft,
            TotalCurrency = "SAR",
        };
        ApplyHeader(claim, request, isNew: true);
        _db.Claims.Add(claim);
        await ApplyChildrenAsync(claim, request, ct);
        Recalculate(claim);

        _db.AuditLogs.Add(BuildAudit(claim, "ClaimCreated", actor, request.ChangeReason,
            new Dictionary<string, string?>(), Snapshot(claim)));
        await _db.SaveChangesAsync(ct);
        return await GetAsync(claim.Id, null, ct);
    }

    public async Task<ClaimEditDto> UpdateAsync(string claimId, ClaimEditDto request, string? actor, string? providerScope, CancellationToken ct = default)
    {
        var claim = await LoadScopedAsync(claimId, providerScope, ct, tracking: true);
        if (!IsEditable(claim))
            throw RcmWorkflowException.Conflict($"{claim.ClaimNumber} cannot be edited: {LockReason(claim)}");
        if (claim.SubmittedAt != null && IsPreAuthUse(request.Use) != RcmInputMapper.IsPreAuth(claim))
            throw RcmWorkflowException.Invalid("Use (claim / preauthorization) cannot change after the record was sent to NPHIES.");
        if (claim.SubmittedAt != null && (request.PatientId != claim.PatientId || request.InsurerId != claim.InsurerId || request.ProviderId != claim.ProviderId))
            throw RcmWorkflowException.Invalid("Patient, insurer and provider cannot change after the record was sent to NPHIES. Cancel it and create a new claim.");
        if (!string.IsNullOrWhiteSpace(providerScope)) request.ProviderId = providerScope!;
        await ValidateReferencesAsync(request, ct);

        var before = Snapshot(claim);
        ApplyHeader(claim, request, isNew: false);
        await ApplyChildrenAsync(claim, request, ct);
        Recalculate(claim);
        var after = Snapshot(claim);
        var changes = Diff(before, after);
        if (changes.Count == 0) return await ToEditDtoAsync(claim, ct);

        // The stored bundle no longer reflects the claim; reviews were given on the old content.
        claim.FhirClaimBundle = null;
        claim.UpdatedAt = DateTime.UtcNow;
        var reviewReset = false;
        if (!RcmInputMapper.IsPreAuth(claim))
        {
            if (claim.TechnicalReviewStatus != "Pending" || claim.MedicalReviewStatus != "Pending") reviewReset = true;
            claim.TechnicalReviewStatus = "Pending";
            claim.MedicalReviewStatus = "Pending";
            claim.TechnicalReviewedAt = null;
            claim.MedicalReviewedAt = null;
        }
        if (claim.NphiesStatus is RcmClaimStatus.ValidationFailed or RcmClaimStatus.Error && claim.SubmittedAt is null)
            claim.NphiesStatus = RcmClaimStatus.Draft;

        var audit = BuildAudit(claim, "ClaimEdited", actor, request.ChangeReason, before, after);
        if (reviewReset) audit.ChangeDetails = (audit.ChangeDetails + " [technical/medical review reset to Pending]").Trim();
        _db.AuditLogs.Add(audit);
        await _db.SaveChangesAsync(ct);
        return await ToEditDtoAsync(claim, ct);
    }

    private static bool IsPreAuthUse(string? use) => !string.IsNullOrWhiteSpace(use) && use.Contains("preauth", StringComparison.OrdinalIgnoreCase);

    private async Task<string> NextClaimNumberAsync(bool preAuth, CancellationToken ct)
    {
        var prefix = (preAuth ? "PA-" : "CL-") + DateTime.UtcNow.ToString("yyyyMMdd", CultureInfo.InvariantCulture) + "-";
        var count = await _db.Claims.CountAsync(c => c.ClaimNumber.StartsWith(prefix), ct);
        string number;
        do number = $"{prefix}{++count:D4}";
        while (await _db.Claims.AnyAsync(c => c.ClaimNumber == number, ct));
        return number;
    }

    private async Task ValidateReferencesAsync(ClaimEditDto r, CancellationToken ct)
    {
        var errors = new List<string>();
        r.ClaimType = (r.ClaimType ?? string.Empty).Trim().ToLowerInvariant();
        r.ClaimSubType = string.IsNullOrWhiteSpace(r.ClaimSubType) ? "op" : r.ClaimSubType!.Trim().ToLowerInvariant();
        if (!ClaimTypes.Contains(r.ClaimType)) errors.Add($"claimType must be one of {string.Join(", ", ClaimTypes)}.");
        if (!SubTypes.Contains(r.ClaimSubType)) errors.Add($"claimSubType must be one of {string.Join(", ", SubTypes)}.");
        foreach (var (value, name) in new[] { (r.PatientId, "patientId"), (r.CoverageId, "coverageId"), (r.ProviderId, "providerId"), (r.InsurerId, "insurerId") })
            if (string.IsNullOrWhiteSpace(value)) errors.Add($"{name} is required.");
        if (errors.Count > 0) throw RcmWorkflowException.Invalid("The claim is incomplete.", errors);

        if (!await _db.Patients.AnyAsync(p => p.Id == r.PatientId, ct)) errors.Add("Patient not found.");
        if (!await _db.Organizations.AnyAsync(o => o.Id == r.ProviderId, ct)) errors.Add("Provider not found.");
        if (!await _db.Organizations.AnyAsync(o => o.Id == r.InsurerId, ct)) errors.Add("Insurer not found.");
        var coverage = await _db.Coverages.AsNoTracking().FirstOrDefaultAsync(c => c.Id == r.CoverageId, ct);
        if (coverage is null) errors.Add("Coverage not found.");
        else
        {
            if (coverage.PatientId != r.PatientId) errors.Add("The coverage belongs to a different patient.");
            if (coverage.InsurerId != r.InsurerId) errors.Add("The coverage is with a different insurer.");
        }
        if (!string.IsNullOrWhiteSpace(r.PractitionerId) && !await _db.Practitioners.AnyAsync(p => p.Id == r.PractitionerId, ct))
            errors.Add("Attending practitioner not found.");
        if (!string.IsNullOrWhiteSpace(r.EncounterId) && !await _db.Encounters.AnyAsync(e => e.Id == r.EncounterId, ct))
            errors.Add("Encounter not found.");
        if (!string.IsNullOrWhiteSpace(r.LocationId) && !await _db.Locations.AnyAsync(l => l.Id == r.LocationId, ct))
            errors.Add("Location not found.");
        var practitionerIds = r.CareTeam.Select(m => m.PractitionerId).Where(p => !string.IsNullOrWhiteSpace(p)).Distinct().ToList();
        var known = await _db.Practitioners.Where(p => practitionerIds.Contains(p.Id)).Select(p => p.Id).ToListAsync(ct);
        foreach (var missing in practitionerIds.Except(known)) errors.Add($"Care team practitioner '{missing}' not found.");
        if (r.CareTeam.Any(m => string.IsNullOrWhiteSpace(m.PractitionerId))) errors.Add("Every care team member needs a practitioner.");

        foreach (var (item, idx) in r.Items.Select((x, i) => (x, i + 1)))
        {
            if (string.IsNullOrWhiteSpace(item.ProductOrServiceCode)) errors.Add($"Item {idx}: service / product code is required.");
            if (item.Quantity <= 0) errors.Add($"Item {idx}: quantity must be greater than 0.");
            if (item.UnitPrice < 0) errors.Add($"Item {idx}: unit price cannot be negative.");
            if (item.Factor is <= 0 or > 1) errors.Add($"Item {idx}: factor must be between 0 and 1.");
            if (item.TaxRate is < 0 or > 100) errors.Add($"Item {idx}: VAT rate must be a percentage between 0 and 100.");
            if (item.PatientShare is < 0) errors.Add($"Item {idx}: patient share cannot be negative.");
        }
        if (r.Diagnoses.Any(d => string.IsNullOrWhiteSpace(d.DiagnosisCode))) errors.Add("Every diagnosis needs an ICD-10-AM code.");
        if (r.SupportingInfo.Any(s => string.IsNullOrWhiteSpace(s.Category))) errors.Add("Every supporting info entry needs a category.");
        foreach (var s in r.SupportingInfo.Where(s => !string.IsNullOrWhiteSpace(s.AttachmentData)))
        {
            try { Convert.FromBase64String(s.AttachmentData!); }
            catch (FormatException) { errors.Add($"Supporting info {s.Sequence}: attachment is not valid base64."); }
        }
        if (r.BillablePeriodStart.HasValue && r.BillablePeriodEnd.HasValue && r.BillablePeriodEnd < r.BillablePeriodStart)
            errors.Add("Billable period end is before its start.");
        if (errors.Count > 0) throw RcmWorkflowException.Invalid("The claim has invalid data.", errors);
    }

    private static void ApplyHeader(Claim c, ClaimEditDto r, bool isNew)
    {
        var preAuth = IsPreAuthUse(r.Use);
        if (isNew || c.SubmittedAt is null)
        {
            if (c.Use != (preAuth ? "preauthorization" : "claim")) c.ClaimIdentifierSystem = null;
            c.Use = preAuth ? "preauthorization" : "claim";
        }
        c.ClaimType = r.ClaimType;
        c.ClaimSubType = r.ClaimSubType;
        c.Priority = string.IsNullOrWhiteSpace(r.Priority) ? "normal" : r.Priority.Trim().ToLowerInvariant();
        c.PayeeType = string.IsNullOrWhiteSpace(r.PayeeType) ? "provider" : r.PayeeType;
        c.PatientId = r.PatientId;
        c.CoverageId = r.CoverageId;
        c.ProviderId = r.ProviderId;
        c.InsurerId = r.InsurerId;
        c.PractitionerId = Blank(r.PractitionerId);
        c.LocationId = Blank(r.LocationId);
        c.EncounterId = Blank(r.EncounterId);
        c.BillablePeriodStart = r.BillablePeriodStart;
        c.BillablePeriodEnd = r.BillablePeriodEnd;
        c.ServicedPeriodStart = r.ServicedPeriodStart ?? r.BillablePeriodStart;
        c.ServicedPeriodEnd = r.ServicedPeriodEnd ?? r.BillablePeriodEnd;
        c.PreAuthorizationRef = preAuth ? null : NormalizeRefs(r.PreAuthorizationRef);
        c.EpisodeIdentifierValue = Blank(r.EpisodeIdentifierValue);
        c.ReferralIdentifier = Blank(r.ReferralIdentifier);
        c.PrescriptionIdentifier = Blank(r.PrescriptionIdentifier);
        c.AccidentDate = r.AccidentDate;
        c.AccidentType = Blank(r.AccidentType);
        c.EligibilityOfflineReference = Blank(r.EligibilityOfflineReference);
        c.EligibilityOfflineDate = r.EligibilityOfflineDate;
        c.AuthorizationOfflineDate = r.AuthorizationOfflineDate;
        c.IsShadowBilling = r.IsShadowBilling;
        c.IsTransfer = r.IsTransfer;
        c.IsNewborn = r.IsNewborn;
    }

    private static string? Blank(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim();

    private static string? NormalizeRefs(string? refs)
    {
        if (string.IsNullOrWhiteSpace(refs)) return null;
        var list = refs.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToList();
        return list.Count == 0 ? null : string.Join(",", list);
    }

    /// <summary>Syncs child rows with the request. Items are matched by sequence so their package details / oral rows survive.</summary>
    private async Task ApplyChildrenAsync(Claim claim, ClaimEditDto r, CancellationToken ct)
    {
        Resequence(r.Items, i => i.Sequence, (i, s) => i.Sequence = s);
        Resequence(r.Diagnoses, d => d.Sequence, (d, s) => d.Sequence = s);
        Resequence(r.CareTeam, m => m.Sequence, (m, s) => m.Sequence = s);
        Resequence(r.SupportingInfo, s => s.Sequence, (s, v) => s.Sequence = v);
        var documentIds = r.SupportingInfo.Where(s => !s.RemoveAttachment && !string.IsNullOrWhiteSpace(s.DocumentId)).Select(s => s.DocumentId!).Distinct().ToList();
        var documents = documentIds.Count == 0 ? new List<ClaimDocument>()
            : await _db.ClaimDocuments.AsNoTracking().Where(d => documentIds.Contains(d.Id)).ToListAsync(ct);
        foreach (var id in documentIds)
        {
            var d = documents.FirstOrDefault(x => x.Id == id) ?? throw RcmWorkflowException.Invalid($"Document '{id}' was not found.");
            if (d.PatientId != claim.PatientId || d.ProviderId != claim.ProviderId)
                throw RcmWorkflowException.Invalid($"Document '{d.Title ?? d.FileName}' belongs to a different patient or facility.");
        }

        // Items
        var existing = claim.Items.ToDictionary(i => i.Sequence);
        var keep = r.Items.Select(i => i.Sequence).ToHashSet();
        var removed = claim.Items.Where(i => !keep.Contains(i.Sequence)).ToList();
        if (removed.Count > 0)
        {
            var removedIds = removed.Select(i => i.Id).ToList();
            _db.VisionPrescriptions.RemoveRange(await _db.VisionPrescriptions.Where(v => removedIds.Contains(v.ClaimItemId)).ToListAsync(ct));
            _db.OralDetails.RemoveRange(await _db.OralDetails.Where(o => removedIds.Contains(o.ClaimItemId)).ToListAsync(ct));
            foreach (var i in removed)
            {
                _db.RemoveRange(i.Details);
                claim.Items.Remove(i);
                _db.ClaimItems.Remove(i);
            }
        }
        var vision = claim.ClaimType == "vision";
        foreach (var src in r.Items)
        {
            if (!existing.TryGetValue(src.Sequence, out var item))
            {
                item = new ClaimItem { ClaimId = claim.Id, Sequence = src.Sequence, CreatedAt = DateTime.UtcNow };
                claim.Items.Add(item);
                if (_db.Entry(claim).State != EntityState.Added) _db.ClaimItems.Add(item);
            }
            item.ProductOrServiceCode = src.ProductOrServiceCode.Trim();
            item.ProductOrServiceSystem = Blank(src.ProductOrServiceSystem);
            item.ProductOrServiceDisplay = Blank(src.ProductOrServiceDisplay);
            item.AltProductOrServiceCode = Blank(src.AltProductOrServiceCode);
            item.ServicedDate = src.ServicedDate;
            if (src.ServicedDate.HasValue) { item.ServicedPeriodStart = null; item.ServicedPeriodEnd = null; }
            item.Quantity = src.Quantity;
            item.UnitPrice = src.UnitPrice;
            item.Factor = src.Factor;
            item.TaxRate = src.TaxRate;
            item.PatientShare = src.PatientShare;
            item.CareTeamSequence = src.CareTeamSequence;
            item.DiagnosisSequence = src.DiagnosisSequences.Count == 0 ? null : string.Join(",", src.DiagnosisSequences.Distinct());
            item.InformationSequence = src.InformationSequences.Count == 0 ? null : string.Join(",", src.InformationSequences.Distinct());
            item.BodySiteCode = Blank(src.BodySiteCode);
            item.SubSiteCode = Blank(src.SubSiteCode);
            item.IsPackage = src.IsPackage;
            item.IsMaternity = src.IsMaternity;
            item.PatientInvoiceValue = Blank(src.PatientInvoiceValue);
            item.Notes = Blank(src.Notes);
            item.UpdatedAt = DateTime.UtcNow;

            if (vision)
            {
                var current = await _db.VisionPrescriptions.Where(v => v.ClaimItemId == item.Id).ToListAsync(ct);
                _db.VisionPrescriptions.RemoveRange(current);
                foreach (var lens in src.Lenses)
                    _db.VisionPrescriptions.Add(new VisionPrescription
                    {
                        ClaimItemId = item.Id,
                        LensSpecification = string.IsNullOrWhiteSpace(lens.Product) ? "lens" : lens.Product!,
                        Product = string.IsNullOrWhiteSpace(lens.Product) ? "lens" : lens.Product,
                        Eye = string.IsNullOrWhiteSpace(lens.Eye) ? "right" : lens.Eye,
                        Sphere = lens.Sphere, Cylinder = lens.Cylinder, Axis = lens.Axis, Prism = lens.Prism,
                        PrismBase = Blank(lens.PrismBase), Add = lens.Add, Power = lens.Power,
                    });
            }
        }

        // Diagnoses / care team: small, fully replaced.
        foreach (var d in claim.Diagnoses.ToList()) { claim.Diagnoses.Remove(d); _db.Remove(d); }
        foreach (var d in r.Diagnoses)
            AddChild(claim, claim.Diagnoses, new ClaimDiagnosis
            {
                ClaimId = claim.Id,
                Sequence = d.Sequence,
                DiagnosisCode = d.DiagnosisCode.Trim().ToUpperInvariant(),
                DiagnosisSystem = Nphies.Constants.NphiesCodeSystems.Icd10Am,
                DiagnosisDisplay = Blank(d.DiagnosisDisplay),
                DiagnosisType = string.IsNullOrWhiteSpace(d.DiagnosisType) ? "secondary" : d.DiagnosisType!.Trim().ToLowerInvariant(),
                OnAdmissionCode = Blank(d.OnAdmissionCode),
                ConditionOnsetCode = Blank(d.ConditionOnsetCode)?.ToUpperInvariant(),
            });

        foreach (var m in claim.CareTeam.ToList()) { claim.CareTeam.Remove(m); _db.Remove(m); }
        foreach (var m in r.CareTeam)
            AddChild(claim, claim.CareTeam, new ClaimCareTeam
            {
                ClaimId = claim.Id,
                Sequence = m.Sequence,
                PractitionerId = m.PractitionerId,
                Role = string.IsNullOrWhiteSpace(m.Role) ? "primary" : m.Role,
                Qualification = Blank(m.Qualification),
            });

        // Supporting info: matched by sequence so stored attachments are kept unless replaced / removed.
        var infoBySeq = claim.SupportingInfo.ToDictionary(s => s.Sequence);
        var infoKeep = r.SupportingInfo.Select(s => s.Sequence).ToHashSet();
        foreach (var s in claim.SupportingInfo.Where(s => !infoKeep.Contains(s.Sequence)).ToList()) { claim.SupportingInfo.Remove(s); _db.Remove(s); }
        foreach (var src in r.SupportingInfo)
        {
            if (!infoBySeq.TryGetValue(src.Sequence, out var info))
            {
                info = new ClaimSupportingInfo { ClaimId = claim.Id, Sequence = src.Sequence };
                AddChild(claim, claim.SupportingInfo, info);
            }
            info.Category = src.Category.Trim();
            info.CodeValue = Blank(src.CodeValue);
            info.CodeSystem = Blank(src.CodeSystem);
            info.StringValue = Blank(src.StringValue);
            info.QuantityValue = src.QuantityValue;
            info.QuantityUnit = Blank(src.QuantityUnit);
            info.DateValue = src.DateValue;
            info.BooleanValue = src.BooleanValue;
            info.AttachmentTitle = Blank(src.AttachmentTitle);
            info.AttachmentUrl = Blank(src.AttachmentUrl);
            if (src.RemoveAttachment)
            {
                info.AttachmentData = null;
                info.AttachmentContentType = null;
                info.DocumentId = null;
            }
            else if (!string.IsNullOrWhiteSpace(src.DocumentId))
            {
                var doc = documents.First(d => d.Id == src.DocumentId);
                info.DocumentId = doc.Id;
                info.AttachmentData = null;
                info.AttachmentContentType = doc.ContentType;
                info.AttachmentTitle ??= doc.Title ?? doc.FileName;
            }
            else if (!string.IsNullOrWhiteSpace(src.AttachmentData))
            {
                info.DocumentId = null;
                info.AttachmentData = src.AttachmentData;
                info.AttachmentContentType = string.IsNullOrWhiteSpace(src.AttachmentContentType) ? "application/pdf" : src.AttachmentContentType;
            }
            else if (!string.IsNullOrWhiteSpace(src.AttachmentContentType))
                info.AttachmentContentType = src.AttachmentContentType;
        }
    }

    private void AddChild<T>(Claim claim, ICollection<T> collection, T child) where T : class
    {
        collection.Add(child);
        if (_db.Entry(claim).State != EntityState.Added) _db.Add(child);
    }

    private static void Resequence<T>(List<T> list, Func<T, int> get, Action<T, int> set)
    {
        // Keep caller sequences when they are positive and unique; otherwise number 1..n in list order.
        var seqs = list.Select(get).ToList();
        if (seqs.All(s => s > 0) && seqs.Distinct().Count() == seqs.Count) return;
        for (var i = 0; i < list.Count; i++) set(list[i], i + 1);
    }

    /// <summary>Net = quantity × unit price × factor + VAT; header total = Σ net. Users never type these.</summary>
    public static void Recalculate(Claim claim)
    {
        foreach (var item in claim.Items)
        {
            var gross = Math.Round((item.Quantity is > 0 ? item.Quantity.Value : 1m) * (item.UnitPrice ?? 0m) * (item.Factor ?? 1m), 2);
            item.Tax = item.TaxRate is > 0 ? Math.Round(gross * item.TaxRate.Value / 100m, 2) : 0m;
            item.Net = gross + item.Tax;
        }
        claim.Total = claim.Items.Sum(i => i.Net ?? 0m);
    }

    // ===================== Health =====================

    public async Task<ClaimHealthDto> CheckHealthAsync(string claimId, string? providerScope, CancellationToken ct = default)
    {
        var claim = await LoadScopedAsync(claimId, providerScope, ct);
        var preAuths = await LoadPreAuthInfoAsync(new[] { claim }, ct);
        var sizes = await RcmDocumentService.LoadDocumentSizesAsync(_db, claim.SupportingInfo, ct);
        return Evaluate(claim, preAuths, sizes, _documentOptions);
    }

    public async Task<List<ClaimHealthDto>> CheckHealthBulkAsync(RcmClaimQuery query, bool problemsOnly, CancellationToken ct = default)
    {
        var q = GraphQuery().AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.ProviderId)) q = q.Where(c => c.ProviderId == query.ProviderId);
        if (!string.IsNullOrWhiteSpace(query.InsurerId)) q = q.Where(c => c.InsurerId == query.InsurerId);
        if (!string.IsNullOrWhiteSpace(query.ClaimType)) q = q.Where(c => c.ClaimType == query.ClaimType);
        if (!string.IsNullOrWhiteSpace(query.Use))
            q = query.Use!.StartsWith("pre", StringComparison.OrdinalIgnoreCase) ? q.Where(c => c.Use.Contains("preauth")) : q.Where(c => c.Use == query.Use);
        var status = query.NphiesStatus ?? query.Status;
        if (!string.IsNullOrWhiteSpace(status))
            q = status == RcmClaimStatus.Draft ? q.Where(c => c.NphiesStatus == null || c.NphiesStatus == status) : q.Where(c => c.NphiesStatus == status);
        if (query.From.HasValue) q = q.Where(c => c.CreatedDate >= query.From);
        if (query.To.HasValue) q = q.Where(c => c.CreatedDate <= query.To);

        var claims = await q.OrderByDescending(c => c.CreatedDate).Take(Math.Clamp(query.Take, 1, 500)).ToListAsync(ct);
        var preAuths = await LoadPreAuthInfoAsync(claims, ct);
        var sizes = await RcmDocumentService.LoadDocumentSizesAsync(_db, claims.SelectMany(c => c.SupportingInfo), ct);
        var results = claims.Select(c => Evaluate(c, preAuths, sizes, _documentOptions)).ToList();
        return problemsOnly ? results.Where(r => r.Issues.Count > 0).ToList() : results;
    }

    private sealed record PreAuthInfo(string ClaimId, DateTime? PeriodEnd, decimal? Eligible, string? PatientId);

    private async Task<Dictionary<string, PreAuthInfo>> LoadPreAuthInfoAsync(IEnumerable<Claim> claims, CancellationToken ct)
    {
        var refs = claims.SelectMany(c => SplitRefs(c.PreAuthorizationRef)).Distinct().ToList();
        if (refs.Count == 0) return new Dictionary<string, PreAuthInfo>();
        var rows = await _db.ClaimResponses.AsNoTracking().Where(r => r.PreAuthRef != null && refs.Contains(r.PreAuthRef))
            .OrderByDescending(r => r.ReceivedAt)
            .Select(r => new { r.PreAuthRef, r.ClaimId, r.PreAuthPeriodEnd, r.TotalEligible, r.TotalBenefit, r.TotalPatientShare, PatientId = r.Claim!.PatientId })
            .ToListAsync(ct);
        return rows.GroupBy(r => r.PreAuthRef!).ToDictionary(g => g.Key, g =>
        {
            var r = g.First();
            return new PreAuthInfo(r.ClaimId, r.PreAuthPeriodEnd, r.TotalEligible ?? (r.TotalBenefit + (r.TotalPatientShare ?? 0m)), r.PatientId);
        });
    }

    private static IEnumerable<string> SplitRefs(string? refs) =>
        string.IsNullOrWhiteSpace(refs) ? Enumerable.Empty<string>()
            : refs.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    private static readonly string[] VitalSignCategories =
    {
        "vital-sign-systolic", "vital-sign-diastolic", "vital-sign-height", "vital-sign-weight",
        "pulse", "temperature", "respiratory-rate", "oxygen-saturation",
    };

    private static readonly string[] ClinicalNarrativeCategories =
    {
        "chief-complaint", "history-of-present-illness", "patient-history", "physical-examination", "treatment-plan",
    };

    /// <summary>Supporting-info slices with min ≥ 1 in the nphies-fs 1.0.0 claim/priorauth profiles.</summary>
    private static readonly Dictionary<string, string[]> RequiredClinicalInfo = new(StringComparer.OrdinalIgnoreCase)
    {
        ["institutional"] = ClinicalNarrativeCategories.Append("investigation-result").Concat(VitalSignCategories).ToArray(),
        ["professional"] = ClinicalNarrativeCategories.Append("investigation-result").Concat(VitalSignCategories).ToArray(),
        ["oral"] = new[] { "chief-complaint" },
    };

    /// <summary>Categories whose slices require timing[x]:timingPeriod (sent as start = end = dateValue).</summary>
    private static readonly HashSet<string> TimedCategories = new(VitalSignCategories.Concat(new[]
    {
        "birth-weight", "employmentImpacted", "icu-hours", "lab-test", "ventilation-hours", "onset", "last-menstrual-period", "missingtooth",
    }), StringComparer.OrdinalIgnoreCase);

    private static bool HasFile(ClaimSupportingInfo s) =>
        !string.IsNullOrWhiteSpace(s.DocumentId) || !string.IsNullOrWhiteSpace(s.AttachmentData) || !string.IsNullOrWhiteSpace(s.AttachmentUrl);

    private static ClaimHealthDto Evaluate(Claim c, Dictionary<string, PreAuthInfo> preAuths, IReadOnlyDictionary<string, long> documentSizes, RcmDocumentOptions documentOptions)
    {
        var h = new ClaimHealthDto { ClaimId = c.Id, ClaimNumber = c.ClaimNumber };
        void Add(string severity, string code, string? field, string message) =>
            h.Issues.Add(new ClaimHealthIssueDto { Severity = severity, Code = code, Field = field, Message = message });

        var preAuth = RcmInputMapper.IsPreAuth(c);
        var type = (c.ClaimType ?? string.Empty).ToLowerInvariant();
        var careTeamRequired = type is "professional" or "institutional" or "oral" or "vision";

        // Header
        if (c.Patient is null) Add("Error", "PATIENT_MISSING", "header.patientId", "Patient is missing.");
        if (c.Coverage is null) Add("Error", "COVERAGE_MISSING", "header.coverageId", "Coverage is missing.");
        else
        {
            // A newborn uses the mother's coverage (IG 6.5); the mother is then sent as the coverage subscriber.
            if (c.Coverage.PatientId != c.PatientId && !c.IsNewborn)
                Add("Error", "COVERAGE_PATIENT", "header.coverageId", "Coverage belongs to a different patient. For a newborn on the mother's coverage, mark the claim as newborn.");
            if (c.Coverage.InsurerId != c.InsurerId) Add("Error", "COVERAGE_INSURER", "header.coverageId", "Coverage is with a different insurer than the claim.");
            var serviceDate = c.ServicedPeriodStart ?? c.BillablePeriodStart ?? c.Items.Select(i => i.ServicedDate).Min() ?? c.CreatedDate;
            var active = string.Equals(c.Coverage.Status, "active", StringComparison.OrdinalIgnoreCase)
                && serviceDate.Date >= c.Coverage.CoverageStartDate.Date
                && (c.Coverage.CoverageEndDate == default || serviceDate.Date <= c.Coverage.CoverageEndDate.Date);
            if (!active)
                Add("Error", "COVERAGE_INACTIVE", "header.coverageId",
                    $"Coverage is not active on the service date {serviceDate:yyyy-MM-dd} (policy {c.Coverage.CoverageStartDate:yyyy-MM-dd} – {c.Coverage.CoverageEndDate:yyyy-MM-dd}, status {c.Coverage.Status}).");
        }
        if (c.Provider is null) Add("Error", "PROVIDER_MISSING", "header.providerId", "Provider is missing.");
        if (c.Insurer is null) Add("Error", "INSURER_MISSING", "header.insurerId", "Insurer is missing.");
        var start = c.BillablePeriodStart ?? c.ServicedPeriodStart;
        var end = c.BillablePeriodEnd ?? c.ServicedPeriodEnd;
        if (start.HasValue && end.HasValue && end < start) Add("Error", "PERIOD_INVALID", "header.billablePeriodEnd", "Billable period end is before its start.");
        if (!preAuth && start.HasValue && start.Value.Date > DateTime.UtcNow.Date) Add("Error", "SERVICE_FUTURE", "header.billablePeriodStart", "A claim cannot be for a future service date.");
        if (type == "institutional" && c.EncounterId is null) Add("Warning", "ENCOUNTER_MISSING", "header.encounterId", "No encounter recorded; one will be derived from the sub-type and service period. Record the actual encounter for in-patient stays.");
        if (c.ClaimSubType == "ip" && c.EncounterId is null && type != "institutional") Add("Warning", "ENCOUNTER_MISSING", "header.encounterId", "In-patient records should reference an encounter.");

        // IG 6.5: newborn prior authorizations and claims carry birth-weight on the birth encounter.
        if (c.IsNewborn && !c.SupportingInfo.Any(s => string.Equals(s.Category, "birth-weight", StringComparison.OrdinalIgnoreCase)))
            Add("Warning", "NEWBORN_BIRTH_WEIGHT", "supportingInfo",
                "Newborn claim without birth-weight supporting info. NPHIES requires it for the birth encounter.");

        // NPHIES requires an episode (Claim) and a patient invoice (every item); defaults are sent when empty.
        if (string.IsNullOrWhiteSpace(c.EpisodeIdentifierValue))
            Add("Info", "EPISODE_DEFAULT", "header.episode",
                $"No episode of care is recorded; '{RcmInputMapper.DefaultEpisode(c)}' will be sent. Use the same episode for every claim of one admission or course of treatment.");
        var itemsWithoutInvoice = c.Items.Where(i => string.IsNullOrWhiteSpace(i.PatientInvoiceValue)).Select(i => i.Sequence).OrderBy(s => s).ToList();
        if (itemsWithoutInvoice.Count > 0)
            Add("Info", "INVOICE_DEFAULT", "items",
                $"Item(s) {string.Join(", ", itemsWithoutInvoice)} have no patient invoice number; '{RcmInputMapper.DefaultPatientInvoice(c)}' will be sent.");

        // Current IG (nphies-fs 1.0.0) conformance
        if (c.Provider != null && string.IsNullOrWhiteSpace(c.Provider.ProviderTypeCode))
            Add("Error", "PROVIDER_TYPE", "header.providerId", $"Provider '{c.Provider.OrganizationName}' has no provider type; NPHIES requires the provider-type extension (Masters → Organizations).");
        if (c.Patient != null)
        {
            var kind = RcmInputMapper.ParseIdentifierKind(c.Patient.NationalIdType, c.Patient.NationalId);
            var idValue = c.Patient.NationalId ?? string.Empty;
            if (kind == Nphies.Models.PatientIdentifierKind.Iqama && !idValue.StartsWith('2'))
                Add("Error", "PATIENT_IQAMA", "header.patientId", $"Iqama number '{idValue}' must start with 2 (BV-00798).");
            if (kind == Nphies.Models.PatientIdentifierKind.NationalId && !idValue.StartsWith('1'))
                Add("Error", "PATIENT_NATIONAL_ID", "header.patientId", $"National ID '{idValue}' must start with 1.");
            if (kind == Nphies.Models.PatientIdentifierKind.Passport && string.IsNullOrWhiteSpace(c.Patient.NationalIdCountry))
                Add("Error", "PATIENT_PASSPORT_COUNTRY", "header.patientId", "Passport holders need the issuing country (ISO 3166 alpha-3, e.g. IND) on the patient (BV-00481).");
            if (string.IsNullOrWhiteSpace(c.Patient.MaritalStatus))
                Add("Info", "PATIENT_MARITAL", "header.patientId", "Patient marital status is not recorded; 'UNK' (unknown) will be sent.");
            if (string.IsNullOrWhiteSpace(c.Patient.Occupation))
                Add("Info", "PATIENT_OCCUPATION", "header.patientId", "Patient occupation is not recorded; 'unknown' will be sent.");
            else if (!Nphies.Constants.NphiesCodeSystems.OccupationCodes.Contains(c.Patient.Occupation))
                Add("Error", "PATIENT_OCCUPATION", "header.patientId",
                    $"Occupation '{c.Patient.Occupation}' is not an NPHIES occupation code ({string.Join(", ", Nphies.Constants.NphiesCodeSystems.OccupationCodes)}).");
        }
        if (type == "institutional" && !preAuth)
            foreach (var d in c.Diagnoses.Where(d => string.IsNullOrWhiteSpace(d.ConditionOnsetCode)))
                Add("Info", "DIAGNOSIS_ONSET", $"diagnoses[{d.Sequence}].conditionOnsetCode", $"Diagnosis #{d.Sequence} has no condition onset; 'NR' (not reported) will be sent.");
        if (RequiredClinicalInfo.TryGetValue(type, out var requiredCategories))
        {
            var present = c.SupportingInfo.Select(s => s.Category).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = requiredCategories.Where(r => !present.Contains(r)).ToList();
            if (missing.Count > 0)
                Add("Error", "CLINICAL_INFO_REQUIRED", "supportingInfo",
                    $"The {type} profile requires these supporting-info categories: {string.Join(", ", missing)}.");
        }
        foreach (var s in c.SupportingInfo.Where(s => TimedCategories.Contains(s.Category) && s.DateValue is null))
            Add("Error", "INFO_TIMING", $"supportingInfo[{s.Sequence}].dateValue", $"Supporting info #{s.Sequence} ({s.Category}) requires a measurement date/time.");
        foreach (var s in c.SupportingInfo.Where(s => s.Category == "onset" && string.IsNullOrWhiteSpace(s.CodeValue)))
            Add("Error", "INFO_ONSET_CODE", $"supportingInfo[{s.Sequence}].code", $"Supporting info #{s.Sequence} (onset) requires the condition code (ICD-10).");
        foreach (var s in c.SupportingInfo.Where(s => s.Category == "investigation-result" && s.CodeValue == "IRA"
                     && !HasFile(s)))
            Add("Error", "INFO_IRA_ATTACHMENT", $"supportingInfo[{s.Sequence}]", $"Supporting info #{s.Sequence}: 'Investigation results attached' (IRA) requires the result attachment (BV-00846).");
        foreach (var i in c.Items.Where(i => (i.SubSiteCode ?? string.Empty).Contains(',')))
            Add("Error", "ITEM_SUBSITE", $"items[{i.Sequence}].subSiteCode", $"Item {i.Sequence} has several sub-sites ({i.SubSiteCode}); NPHIES allows one per item.");
        foreach (var i in c.Items.Where(i => i.IsPackage && i.Details.Count == 0))
            Add("Error", "ITEM_PACKAGE_DETAIL", $"items[{i.Sequence}].details", $"Item {i.Sequence} is a package but has no details (BV-00036).");

        // Diagnoses
        if (c.Diagnoses.Count == 0) Add("Error", "DIAGNOSIS_MISSING", "diagnoses", "At least one diagnosis is required.");
        else
        {
            var principal = c.Diagnoses.Count(d => string.Equals(d.DiagnosisType, "principal", StringComparison.OrdinalIgnoreCase));
            if (principal == 0) Add("Error", "PRINCIPAL_MISSING", "diagnoses", "No principal diagnosis.");
            if (principal > 1) Add("Warning", "PRINCIPAL_MULTIPLE", "diagnoses", $"{principal} principal diagnoses; NPHIES expects one.");
        }
        DuplicateSequences(c.Diagnoses.Select(d => d.Sequence), "diagnoses", Add);

        // Care team
        if (c.CareTeam.Count == 0 && careTeamRequired)
            Add(c.PractitionerId is null ? "Error" : "Warning", "CARETEAM_MISSING", "careTeam",
                c.PractitionerId is null ? "No care team member." : "No care team; the attending practitioner will be sent as care team #1.");
        DuplicateSequences(c.CareTeam.Select(m => m.Sequence), "careTeam", Add);
        foreach (var m in c.CareTeam.Where(m => string.IsNullOrWhiteSpace(m.Qualification) && string.IsNullOrWhiteSpace(m.Practitioner?.Specialization)))
            Add("Warning", "CARETEAM_QUALIFICATION", $"careTeam[{m.Sequence}].qualification", $"Care team #{m.Sequence} has no qualification (practice specialty).");

        // Items
        if (c.Items.Count == 0) Add("Error", "ITEMS_MISSING", "items", "At least one item is required.");
        DuplicateSequences(c.Items.Select(i => i.Sequence), "items", Add);
        var diagSeqs = c.Diagnoses.Select(d => d.Sequence).ToHashSet();
        var careSeqs = c.CareTeam.Select(m => m.Sequence).ToHashSet();
        var infoSeqs = c.SupportingInfo.Select(s => s.Sequence).ToHashSet();
        foreach (var i in c.Items.OrderBy(i => i.Sequence))
        {
            var f = $"items[{i.Sequence}]";
            if (string.IsNullOrWhiteSpace(i.ProductOrServiceCode)) Add("Error", "ITEM_CODE", $"{f}.productOrServiceCode", $"Item {i.Sequence} has no service code.");
            if (i.Quantity is null or <= 0) Add("Error", "ITEM_QUANTITY", $"{f}.quantity", $"Item {i.Sequence} quantity must be greater than 0.");
            if (i.UnitPrice is null or < 0) Add("Error", "ITEM_PRICE", $"{f}.unitPrice", $"Item {i.Sequence} has no valid unit price.");
            var gross = Math.Round((i.Quantity ?? 1m) * (i.UnitPrice ?? 0m) * (i.Factor ?? 1m), 2);
            var expectedNet = gross + (i.Tax ?? 0m);
            if (i.Net is decimal net && Math.Abs(net - expectedNet) > 0.01m)
                Add("Error", "ITEM_NET", $"{f}.net", $"Item {i.Sequence} net {net:0.00} ≠ quantity × price × factor + VAT ({expectedNet:0.00}).");
            if (i.PatientShare is decimal ps && ps > (i.Net ?? expectedNet))
                Add("Error", "ITEM_PATIENT_SHARE", $"{f}.patientShare", $"Item {i.Sequence} patient share {ps:0.00} exceeds its net amount.");
            if (i.CareTeamSequence is int cs)
            {
                if (!careSeqs.Contains(cs) && !(careSeqs.Count == 0 && cs == 1 && c.PractitionerId != null))
                    Add("Error", "ITEM_CARETEAM_REF", $"{f}.careTeamSequence", $"Item {i.Sequence} points to care team #{cs}, which does not exist.");
            }
            else if (careTeamRequired)
                Add("Warning", "ITEM_CARETEAM", $"{f}.careTeamSequence", $"Item {i.Sequence} is not linked to a care team member (care team #1 will be assumed).");
            var dx = ParseSeq(i.DiagnosisSequence);
            foreach (var d in dx.Where(d => !diagSeqs.Contains(d)))
                Add("Error", "ITEM_DIAGNOSIS_REF", $"{f}.diagnosisSequence", $"Item {i.Sequence} points to diagnosis #{d}, which does not exist.");
            if (dx.Count == 0 && diagSeqs.Count > 1)
                Add("Warning", "ITEM_DIAGNOSIS", $"{f}.diagnosisSequence", $"Item {i.Sequence} is not linked to a diagnosis (the first diagnosis will be assumed).");
            foreach (var s in ParseSeq(i.InformationSequence).Where(s => !infoSeqs.Contains(s)))
                Add("Error", "ITEM_INFO_REF", $"{f}.informationSequence", $"Item {i.Sequence} points to supporting info #{s}, which does not exist.");
            var served = i.ServicedDate ?? i.ServicedPeriodStart;
            if (served.HasValue && start.HasValue && end.HasValue && (served.Value.Date < start.Value.Date || served.Value.Date > end.Value.Date))
                Add("Warning", "ITEM_DATE_PERIOD", $"{f}.servicedDate", $"Item {i.Sequence} service date {served:yyyy-MM-dd} is outside the billable period.");
            if (!preAuth && served.HasValue && served.Value.Date > DateTime.UtcNow.Date)
                Add("Error", "ITEM_DATE_FUTURE", $"{f}.servicedDate", $"Item {i.Sequence} service date is in the future.");
            if (type == "oral" && string.IsNullOrWhiteSpace(i.BodySiteCode))
                Add("Info", "ITEM_TOOTH", $"{f}.bodySite", $"Item {i.Sequence} has no tooth (body site); most dental procedures need one.");
        }
        var itemSum = c.Items.Sum(i => i.Net ?? 0m);
        if (c.Items.Count > 0 && Math.Abs(c.Total - itemSum) > 0.01m)
            Add("Error", "TOTAL_MISMATCH", "header.total", $"Header total {c.Total:0.00} ≠ sum of item net {itemSum:0.00}.");

        // Supporting info
        DuplicateSequences(c.SupportingInfo.Select(s => s.Sequence), "supportingInfo", Add);
        foreach (var s in c.SupportingInfo.Where(s => s.Category == "attachment" && !HasFile(s)))
            Add("Error", "INFO_ATTACHMENT", $"supportingInfo[{s.Sequence}]", $"Supporting info #{s.Sequence} is an attachment without a file.");
        var budget = RcmDocumentService.ComputeBudget(c.Id, c.SupportingInfo, documentSizes, documentOptions);
        static string Mb(long b) => $"{b / 1024d / 1024d:0.0} MB";
        if (budget.Status == "over-limit")
            Add("Error", "ATTACHMENTS_TOO_LARGE", "supportingInfo",
                $"Attachments make the NPHIES message about {Mb(budget.EstimatedMessageBytes)}; the limit is {Mb(budget.LimitBytes)}. Remove or compress documents, split the stay into interim claims, or send extra documents as a communication.");
        else if (budget.Status == "warning")
            Add("Warning", "ATTACHMENT_SIZE_WARN", "supportingInfo",
                $"Attachments make the NPHIES message about {Mb(budget.EstimatedMessageBytes)}, close to the {Mb(budget.LimitBytes)} limit.");

        // Pre-authorization reference
        foreach (var r in SplitRefs(c.PreAuthorizationRef))
        {
            if (!preAuths.TryGetValue(r, out var pa))
            {
                Add("Warning", "PREAUTH_UNKNOWN", "header.preAuthorizationRef", $"Pre-authorization '{r}' is not in the system; the payer cannot be matched against an approval we hold.");
                continue;
            }
            if (pa.PatientId != null && pa.PatientId != c.PatientId)
                Add("Error", "PREAUTH_PATIENT", "header.preAuthorizationRef", $"Pre-authorization '{r}' belongs to a different patient.");
            var served = c.ServicedPeriodStart ?? c.BillablePeriodStart ?? c.CreatedDate;
            if (pa.PeriodEnd.HasValue && served.Date > pa.PeriodEnd.Value.Date)
                Add("Warning", "PREAUTH_EXPIRED", "header.preAuthorizationRef", $"Pre-authorization '{r}' expired on {pa.PeriodEnd:yyyy-MM-dd}, before the service date.");
            var gross = c.Items.Sum(i => (i.Net ?? 0m) - (i.Tax ?? 0m));
            if (pa.Eligible is decimal eligible && eligible > 0 && gross > eligible + 0.01m)
                Add("Warning", "PREAUTH_EXCEEDED", "header.preAuthorizationRef", $"Claimed amount {gross:0.00} (before VAT) exceeds the pre-authorized amount {eligible:0.00}.");
        }

        if (!preAuth && c.IsShadowBilling && c.Items.Any(i => string.IsNullOrWhiteSpace(i.AltProductOrServiceCode)))
            Add("Info", "SHADOW_CODE", "items", "Shadow billing claim has items without the provider's internal code.");

        h.Issues = h.Issues.OrderBy(i => i.Severity switch { "Error" => 0, "Warning" => 1, _ => 2 }).ThenBy(i => i.Field).ToList();
        return h;
    }

    private static void DuplicateSequences(IEnumerable<int> seqs, string field, Action<string, string, string?, string> add)
    {
        foreach (var dup in seqs.GroupBy(s => s).Where(g => g.Count() > 1))
            add("Error", "SEQUENCE_DUPLICATE", field, $"Sequence {dup.Key} is used more than once in {field}.");
    }

    private static List<int> ParseSeq(string? value) =>
        string.IsNullOrWhiteSpace(value) ? new List<int>()
            : value.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(v => int.TryParse(v, out var n) ? n : 0).Where(n => n > 0).ToList();

    // ===================== Correction options =====================

    public async Task<ClaimCorrectionOptionsDto> GetCorrectionOptionsAsync(string claimId, string? providerScope, CancellationToken ct = default)
    {
        var c = await LoadScopedAsync(claimId, providerScope, ct);
        var status = c.NphiesStatus ?? RcmClaimStatus.Draft;
        var preAuth = RcmInputMapper.IsPreAuth(c);
        var openRequests = await _db.CommunicationRequests.AnyAsync(r => r.ClaimId == c.Id && r.Status != "completed" && r.Status != "Responded", ct);
        var openDenials = await _db.ClaimDenials.AnyAsync(d => d.ClaimId == c.Id && d.Status == "Open", ct);
        var newer = await _db.Set<ClaimRelated>().Where(r => r.ReferencedClaimId == c.Id).Select(r => r.ClaimId).FirstOrDefaultAsync(ct);

        var dto = new ClaimCorrectionOptionsDto { ClaimId = c.Id, ClaimNumber = c.ClaimNumber, NphiesStatus = status, Editable = IsEditable(c) };
        void Opt(string action, string label, string description, bool recommended = false) =>
            dto.Options.Add(new ClaimCorrectionOptionDto { Action = action, Label = label, Description = description, Recommended = recommended });

        switch (status)
        {
            case RcmClaimStatus.Draft:
            case RcmClaimStatus.ValidationFailed:
            case RcmClaimStatus.Error:
                dto.Guidance = status == RcmClaimStatus.Draft
                    ? "Not sent yet: edit any field, check the request JSON, then submit."
                    : $"NPHIES did not accept the last attempt ({status}): fix the data shown in the health check and submit again.";
                Opt("edit", "Edit", "Change header, items, diagnoses, care team or supporting info.", true);
                Opt("preview", "Preview request JSON", "Build and validate the bundle without sending it.");
                break;
            case RcmClaimStatus.Timeout:
                dto.Guidance = "The submission may have reached NPHIES. Never resend blindly; run a status-check first.";
                Opt("status-check", "Status check", "Ask NPHIES whether the message was received.", true);
                break;
            case RcmClaimStatus.Submitted:
            case RcmClaimStatus.Queued:
            case RcmClaimStatus.Pended:
                dto.Guidance = "The payer is processing it, so it is locked. If data is wrong, cancel (reason WI) and create a corrected version.";
                if (openRequests) Opt("communicate", "Answer payer request", "The payer asked for information; reply with text or attachments.", true);
                Opt("status-check", "Status check", "Ask NPHIES for the current processing status.", !openRequests);
                Opt("cancel", "Cancel", "Withdraw it (e.g. WI - wrong information) and then create a corrected version.");
                break;
            case RcmClaimStatus.CancelRequested:
                dto.Guidance = "A cancellation is pending at NPHIES.";
                Opt("status-check", "Status check", "Confirm the cancellation.", true);
                break;
            case RcmClaimStatus.Superseded:
                dto.Guidance = "Replaced by a newer version" + (newer != null ? "; open the newer version to make changes." : ".");
                break;
            case RcmClaimStatus.Nullified:
                dto.Guidance = "Nullified: it no longer exists for the payer. Create a new claim if the service must be billed.";
                break;
            case RcmClaimStatus.Cancelled:
                dto.Guidance = preAuth ? "Cancelled. Create a new prior authorization if still needed." : "Cancelled. You can create a corrected version linked to it.";
                if (!preAuth) Opt("resubmit", "Correct & resubmit", "Create an editable copy linked to this claim (related = prior).", true);
                break;
            default:
                if (preAuth)
                {
                    dto.Guidance = "The payer has decided. Send a new linked version to change it.";
                    Opt("update", "Update", "Create an editable copy (related = prior) to change services or amounts.", status == RcmClaimStatus.Rejected || status == RcmClaimStatus.PartiallyApproved);
                    if (status is RcmClaimStatus.Approved or RcmClaimStatus.PartiallyApproved)
                    {
                        Opt("extend", "Extend", "Request a longer validity period (related = extend).");
                        Opt("create-claim", "Create claim", "Bill the approved services with this preAuthRef.", status == RcmClaimStatus.Approved);
                    }
                    Opt("cancel", "Cancel", "Withdraw the prior authorization.");
                }
                else if (status is RcmClaimStatus.Recouped)
                {
                    dto.Guidance = "The payer recouped (took back) its payment. Correct & resubmit, or appeal with justification.";
                    if (openDenials) Opt("appeal", "Appeal", "Dispute the denied / reduced lines with justification.", true);
                    Opt("resubmit", "Correct & resubmit", "Create an editable copy linked to this claim (related = prior).", !openDenials);
                }
                else if (status is RcmClaimStatus.Paid or RcmClaimStatus.PartiallyPaid)
                {
                    dto.Guidance = "Paid claims cannot be edited. Nullify if it was billed in error; resubmit or appeal to dispute reductions.";
                    if (openDenials) Opt("appeal", "Appeal", "Dispute the denied / reduced lines with justification.", true);
                    Opt("resubmit", "Correct & resubmit", "Create an editable copy linked to this claim (related = prior).", !openDenials && status == RcmClaimStatus.PartiallyPaid);
                    Opt("nullify", "Nullify", "Reverse the claim at the payer (wrong patient, insurer or duplicate).");
                }
                else
                {
                    dto.Guidance = "Adjudicated claims are corrected by a new version linked to this one (same episode).";
                    Opt("resubmit", "Correct & resubmit", "Create an editable copy linked to this claim (related = prior).", status != RcmClaimStatus.Approved);
                    if (openDenials) Opt("appeal", "Appeal", "Dispute denied lines without changing the claim.");
                    Opt("nullify", "Nullify", "Reverse the claim (wrong patient, insurer or provider), then create a new one.");
                }
                if (openRequests) Opt("communicate", "Answer payer request", "The payer asked for information.");
                break;
        }
        return dto;
    }

    // ===================== Audit / compare =====================

    public async Task<List<ClaimAuditEntryDto>> GetAuditAsync(string claimId, string? providerScope, CancellationToken ct = default)
    {
        await LoadScopedAsync(claimId, providerScope, ct);
        var rows = await _db.AuditLogs.AsNoTracking().Where(a => a.EntityType == AuditEntityType && a.EntityId == claimId)
            .OrderByDescending(a => a.CreatedAt).Take(200).ToListAsync(ct);
        return rows.Select(a => new ClaimAuditEntryDto
        {
            Id = a.Id,
            At = a.CreatedAt,
            User = a.Username,
            Action = a.Action,
            Reason = a.ChangeDetails,
            Changes = Diff(Deserialize(a.OldValues), Deserialize(a.NewValues)),
        }).ToList();
    }

    public async Task<ClaimVersionCompareDto> CompareWithPreviousAsync(string claimId, string? providerScope, CancellationToken ct = default)
    {
        var claim = await LoadScopedAsync(claimId, providerScope, ct);
        var dto = new ClaimVersionCompareDto { ClaimId = claim.Id, ClaimNumber = claim.ClaimNumber };
        var link = claim.RelatedClaims.FirstOrDefault(r => r.ReferencedClaimId != null);
        if (link is null) return dto;
        var previous = await GraphQuery().AsNoTracking().FirstOrDefaultAsync(c => c.Id == link.ReferencedClaimId, ct);
        if (previous is null) return dto;
        dto.PreviousClaimId = previous.Id;
        dto.PreviousClaimNumber = previous.ClaimNumber;
        dto.Relationship = link.Relationship;
        dto.Changes = Diff(Snapshot(previous), Snapshot(claim));
        return dto;
    }

    /// <summary>Flat, human-readable view of everything that goes into the request bundle; diffed for audit and compare.</summary>
    private static Dictionary<string, string?> Snapshot(Claim c)
    {
        static string D(DateTime? d) => d?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture) ?? "";
        static string M(decimal? m) => m?.ToString("0.00", CultureInfo.InvariantCulture) ?? "";
        var s = new Dictionary<string, string?>
        {
            ["header.use"] = c.Use,
            ["header.type"] = $"{c.ClaimType}/{c.ClaimSubType}",
            ["header.priority"] = c.Priority,
            ["header.patientId"] = c.PatientId,
            ["header.coverageId"] = c.CoverageId,
            ["header.providerId"] = c.ProviderId,
            ["header.insurerId"] = c.InsurerId,
            ["header.practitionerId"] = c.PractitionerId,
            ["header.encounterId"] = c.EncounterId,
            ["header.locationId"] = c.LocationId,
            ["header.billablePeriod"] = $"{D(c.BillablePeriodStart)} → {D(c.BillablePeriodEnd)}",
            ["header.preAuthorizationRef"] = c.PreAuthorizationRef,
            ["header.episode"] = c.EpisodeIdentifierValue,
            ["header.referral"] = c.ReferralIdentifier,
            ["header.prescription"] = c.PrescriptionIdentifier,
            ["header.accident"] = c.AccidentDate is null && c.AccidentType is null ? null : $"{c.AccidentType} {D(c.AccidentDate)}".Trim(),
            ["header.shadowBilling"] = c.IsShadowBilling ? "yes" : "no",
            ["header.newborn"] = c.IsNewborn ? "yes" : "no",
            ["header.total"] = M(c.Total),
        };
        foreach (var i in c.Items)
            s[$"items[{i.Sequence}]"] = $"{i.ProductOrServiceCode}{(string.IsNullOrWhiteSpace(i.AltProductOrServiceCode) ? "" : $" ({i.AltProductOrServiceCode})")} ×{i.Quantity?.ToString("0.##", CultureInfo.InvariantCulture)} @ {M(i.UnitPrice)}"
                + $"{(i.Factor is decimal f && f != 1m ? $" f{f.ToString("0.##", CultureInfo.InvariantCulture)}" : "")} VAT {M(i.Tax)} = {M(i.Net)}; patient {M(i.PatientShare)}"
                + $"; date {D(i.ServicedDate ?? i.ServicedPeriodStart)}; dx {i.DiagnosisSequence}; ct {i.CareTeamSequence}; info {i.InformationSequence}"
                + $"{(string.IsNullOrWhiteSpace(i.BodySiteCode) ? "" : $"; site {i.BodySiteCode}{(string.IsNullOrWhiteSpace(i.SubSiteCode) ? "" : "/" + i.SubSiteCode)}")}";
        foreach (var d in c.Diagnoses)
            s[$"diagnoses[{d.Sequence}]"] = $"{d.DiagnosisCode} ({d.DiagnosisType}){(string.IsNullOrWhiteSpace(d.OnAdmissionCode) ? "" : " POA " + d.OnAdmissionCode)}";
        foreach (var m in c.CareTeam)
            s[$"careTeam[{m.Sequence}]"] = $"{m.PractitionerId} {m.Role} {m.Qualification}".Trim();
        foreach (var x in c.SupportingInfo)
            s[$"supportingInfo[{x.Sequence}]"] = $"{x.Category} {x.CodeValue} {x.StringValue} {x.QuantityValue?.ToString("0.##", CultureInfo.InvariantCulture)}{x.QuantityUnit} {D(x.DateValue)}"
                .Trim() + (!HasFile(x) ? "" : $" [file {x.AttachmentTitle ?? x.AttachmentUrl}{(x.DocumentId is null ? "" : $" doc:{x.DocumentId}")}{(x.AttachmentData is null ? "" : $" {x.AttachmentData.Length}b64")}]");
        return s;
    }

    private static List<ClaimFieldChangeDto> Diff(Dictionary<string, string?> before, Dictionary<string, string?> after)
    {
        var keys = before.Keys.Union(after.Keys).OrderBy(k => k.StartsWith("header") ? 0 : 1).ThenBy(k => k, StringComparer.Ordinal);
        var list = new List<ClaimFieldChangeDto>();
        foreach (var k in keys)
        {
            before.TryGetValue(k, out var b);
            after.TryGetValue(k, out var a);
            if (string.IsNullOrWhiteSpace(b) && string.IsNullOrWhiteSpace(a)) continue;
            if (!string.Equals(b ?? "", a ?? "", StringComparison.Ordinal))
                list.Add(new ClaimFieldChangeDto { Field = k, Before = b, After = a });
        }
        return list;
    }

    private static Dictionary<string, string?> Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new Dictionary<string, string?>();
        try { return JsonSerializer.Deserialize<Dictionary<string, string?>>(json) ?? new Dictionary<string, string?>(); }
        catch (JsonException) { return new Dictionary<string, string?>(); }
    }

    private static AuditLog BuildAudit(Claim c, string action, string? actor, string? reason, Dictionary<string, string?> before, Dictionary<string, string?> after)
    {
        // Only changed keys are persisted so the audit table stays small.
        var changed = Diff(before, after).Select(x => x.Field).ToHashSet();
        return new AuditLog
        {
            Username = string.IsNullOrWhiteSpace(actor) ? "system" : actor,
            Action = action,
            EntityType = AuditEntityType,
            EntityId = c.Id,
            OldValues = JsonSerializer.Serialize(before.Where(kv => changed.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value)),
            NewValues = JsonSerializer.Serialize(after.Where(kv => changed.Contains(kv.Key)).ToDictionary(kv => kv.Key, kv => kv.Value)),
            ChangeDetails = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim(),
            IpAddress = string.Empty,
            CreatedAt = DateTime.UtcNow,
            Endpoint = $"claims/{c.ClaimNumber}",
        };
    }

    // ===================== Loading =====================

    private IQueryable<Claim> GraphQuery() =>
        _db.Claims
            .Include(c => c.Patient)
            .Include(c => c.Coverage)
            .Include(c => c.Provider)
            .Include(c => c.Insurer)
            .Include(c => c.Items).ThenInclude(i => i.Details)
            .Include(c => c.Diagnoses)
            .Include(c => c.CareTeam).ThenInclude(m => m.Practitioner)
            .Include(c => c.SupportingInfo)
            .Include(c => c.RelatedClaims)
            .AsSplitQuery();

    private async Task<Claim> LoadScopedAsync(string claimId, string? providerScope, CancellationToken ct, bool tracking = false)
    {
        var q = tracking ? GraphQuery() : GraphQuery().AsNoTracking();
        var claim = await q.FirstOrDefaultAsync(c => c.Id == claimId, ct) ?? throw RcmWorkflowException.NotFound("Claim", claimId);
        if (!string.IsNullOrWhiteSpace(providerScope) && claim.ProviderId != providerScope)
            throw RcmWorkflowException.NotFound("Claim", claimId);
        return claim;
    }
}
