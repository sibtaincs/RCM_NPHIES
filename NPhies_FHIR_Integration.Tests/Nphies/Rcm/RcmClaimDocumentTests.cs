using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Documents;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;
using NPhies_FHIR_Integration.Infrastructure.Nphies;
using Task = System.Threading.Tasks.Task;
using Patient = NPhies_FHIR_Integration.Domain.Entities.Patient;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

[TestClass]
public class RcmClaimDocumentTests
{
    private RcmTestHost _host = null!;

    [TestInitialize]
    public async Task Init() => _host = await RcmTestHost.CreateAsync();

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    private static byte[] FakePdf(int size, int seed = 1)
    {
        var bytes = new byte[size];
        new Random(seed).NextBytes(bytes);
        var header = Encoding.ASCII.GetBytes("%PDF-1.4\n");
        Array.Copy(header, bytes, header.Length);
        return bytes;
    }

    private Task<ClaimDocumentDto> UploadAsync(RcmTestHost.Seed seed, byte[] content, Action<DocumentMetadataDto>? configure = null, RcmTestHost? host = null)
    {
        var metadata = new DocumentMetadataDto
        {
            ProviderId = seed.Provider.Id,
            PatientId = seed.Patient.Id,
            EncounterNumber = "ENC-IP-100",
            EncounterType = "IMP",
            InvoiceNumber = "INV-100",
            InvoiceDate = new DateTime(2024, 5, 31),
            ServiceDate = new DateTime(2024, 5, 3),
            DocumentType = "medical-report",
            Title = "Discharge summary",
        };
        configure?.Invoke(metadata);
        return (host ?? _host).RunAsync<IRcmDocumentService, ClaimDocumentDto>(s =>
            s.UploadAsync(metadata, "discharge.pdf", "application/pdf", content, "tester", null));
    }

    [TestMethod]
    public async Task Upload_stores_metadata_and_filters_by_encounter_invoice_and_attachment_state()
    {
        var seed = await _host.SeedPartiesAsync();
        var content = FakePdf(4096);
        var first = await UploadAsync(seed, content);
        await UploadAsync(seed, FakePdf(2048, 2), m =>
        {
            m.EncounterNumber = "ENC-OP-200";
            m.EncounterType = "AMB";
            m.InvoiceNumber = "INV-200";
            m.InvoiceDate = new DateTime(2024, 7, 1);
            m.ServiceDate = new DateTime(2024, 7, 1);
        });

        Assert.AreEqual(4096, first.SizeBytes);
        Assert.AreEqual(ClaimDocumentStorage.Sha256Hex(content), first.Sha256);
        Assert.AreEqual("Sql", first.StorageProvider);

        async Task<List<string>> ListAsync(Action<DocumentQuery> filter)
        {
            var q = new DocumentQuery { PageSize = 50 };
            filter(q);
            var page = await _host.RunAsync<IRcmDocumentService, RcmPagedResult<ClaimDocumentDto>>(s => s.ListAsync(q));
            return page.Items.Select(d => d.InvoiceNumber!).OrderBy(x => x).ToList();
        }

        CollectionAssert.AreEqual(new[] { "INV-100" }, await ListAsync(q => q.EncounterNumber = "ENC-IP-100"));
        CollectionAssert.AreEqual(new[] { "INV-200" }, await ListAsync(q => q.EncounterType = "AMB"));
        CollectionAssert.AreEqual(new[] { "INV-200" }, await ListAsync(q => { q.InvoiceFrom = new DateTime(2024, 6, 1); }));
        CollectionAssert.AreEqual(new[] { "INV-100" }, await ListAsync(q => { q.From = new DateTime(2024, 5, 1); q.To = new DateTime(2024, 5, 31); }));
        CollectionAssert.AreEqual(new[] { "INV-100", "INV-200" }, await ListAsync(q => q.PatientId = seed.Patient.Id));
        CollectionAssert.AreEqual(new[] { "INV-100", "INV-200" }, await ListAsync(q => q.Unattached = true));

        var download = await _host.RunAsync<IRcmDocumentService, DocumentContent?>(s => s.DownloadAsync(first.Id, false, null));
        CollectionAssert.AreEqual(content, download!.Data);
    }

    [TestMethod]
    public async Task Ingest_is_idempotent_on_external_id()
    {
        var seed = await _host.SeedPartiesAsync();
        var request = new DocumentIngestDto
        {
            ProviderId = seed.Provider.Id,
            PatientId = seed.Patient.Id,
            ExternalId = "HIS-DOC-1",
            FileName = "lab.pdf",
            ContentBase64 = Convert.ToBase64String(FakePdf(1500)),
            DocumentType = "lab-report",
        };

        var first = await _host.RunAsync<IRcmDocumentService, ClaimDocumentDto>(s => s.IngestAsync(request, "his", null));
        var second = await _host.RunAsync<IRcmDocumentService, ClaimDocumentDto>(s => s.IngestAsync(request, "his", null));

        Assert.IsFalse(first.Duplicate);
        Assert.IsTrue(second.Duplicate);
        Assert.AreEqual(first.Id, second.Id);
        Assert.AreEqual("His", first.Source);
        Assert.AreEqual(1, await _host.QueryAsync(db => db.ClaimDocuments.CountAsync()));
    }

    [TestMethod]
    public async Task Attached_document_is_embedded_in_sent_claim_and_stripped_from_audit()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        var content = FakePdf(6000);
        var doc = await UploadAsync(seed, content, m => m.ClaimId = claim.Id);

        var budget = await _host.RunAsync<IRcmDocumentService, ClaimSizeBudgetDto>(s => s.GetSizeBudgetAsync(claim.Id, null));
        Assert.AreEqual(1, budget.AttachmentCount);
        Assert.AreEqual(doc.Id, budget.Attachments[0].DocumentId);
        Assert.AreEqual("ok", budget.Status);

        var linked = await _host.RunAsync<IRcmDocumentService, ClaimDocumentDto?>(s => s.GetAsync(doc.Id, null));
        Assert.AreEqual(claim.Id, XAssert.Single(linked!.Claims).ClaimId);

        _host.ReplyWith("7b - Claim Response.json");
        var result = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));
        CollectionAssert.AreEqual(Array.Empty<string>(), result.Errors);

        var sent = XAssert.Single(_host.Transport.Requests).Json;
        var base64 = Convert.ToBase64String(content);
        StringAssert.Contains(sent, base64);

        // The audit copy references the stored document instead of repeating the payload, and rehydrates byte-for-byte.
        await using var scope = _host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var stripped = await NphiesAuditAttachments.StripAsync(sent, db);
        Assert.IsFalse(stripped.Contains(base64));
        Assert.IsTrue(NphiesAuditAttachments.HasMarkers(stripped));
        Assert.IsTrue(stripped.Length < sent.Length - base64.Length / 2);

        var storage = scope.ServiceProvider.GetRequiredService<IClaimDocumentStorage>();
        var rehydrated = await NphiesAuditAttachments.RehydrateAsync(stripped, async (id, ct) =>
        {
            var d = await db.ClaimDocuments.AsNoTracking().SingleAsync(x => x.Id == id, ct);
            return await storage.ReadAsync(d.StorageProvider, d.StorageKey, ct);
        });
        Assert.AreEqual(sent, rehydrated);

        // Sent claims are no longer editable: attaching more or deleting the attached document is refused.
        var extra = await UploadAsync(seed, FakePdf(1000, 3));
        var attach = await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmDocumentService, ClaimSizeBudgetDto>(s => s.AttachToClaimAsync(claim.Id, new AttachDocumentsDto { DocumentIds = { extra.Id } }, null)));
        Assert.AreEqual(RcmErrorKind.Conflict, attach.Kind);
        var delete = await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmDocumentService, int>(async s => { await s.DeleteAsync(doc.Id, null); return 0; }));
        Assert.AreEqual(RcmErrorKind.Conflict, delete.Kind);
    }

    [TestMethod]
    public async Task Audit_store_persists_stripped_request_json()
    {
        await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);
        _host = await RcmTestHost.CreateAsync(new Dictionary<string, string?> { ["Nphies:EnableMessageAudit"] = "true" });
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        var content = FakePdf(5000, 7);
        await UploadAsync(seed, content, m => m.ClaimId = claim.Id);
        _host.ReplyWith("7b - Claim Response.json");
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));

        var log = XAssert.Single(_host.Audit.Logs);
        await using var scope = _host.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var store = new EfNphiesMessageAuditStore(db, Microsoft.Extensions.Logging.Abstractions.NullLogger<EfNphiesMessageAuditStore>.Instance, stripStoredAttachments: true);
        await store.SaveAsync(log);

        var saved = await _host.QueryAsync(d => d.NphiesMessageLogs.AsNoTracking().SingleAsync(l => l.Id == log.Id));
        Assert.IsFalse(saved.RequestJson!.Contains(Convert.ToBase64String(content)));
        Assert.IsTrue(NphiesAuditAttachments.HasMarkers(saved.RequestJson));
    }

    [TestMethod]
    public async Task Upload_with_only_claim_id_takes_patient_and_provider_from_claim()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_doc_claim_only");
        var doc = await UploadAsync(seed, FakePdf(1200, 13), m =>
        {
            m.ClaimId = claim.Id;
            m.PatientId = string.Empty;
            m.ProviderId = null;
        });

        Assert.AreEqual(seed.Patient.Id, doc.PatientId);
        Assert.AreEqual(claim.Id, XAssert.Single(doc.Claims).ClaimId);
    }

    [TestMethod]
    public async Task Attach_rejects_document_of_another_patient_and_detach_removes_link()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_doc_patient");
        var other = new Patient { MRN = "MRN-2", NationalId = "1000000002", NationalIdType = "NI", FirstName = "Omar", LastName = "Ali", FullName = "Omar Ali", Gender = "M", DateOfBirth = new DateTime(1990, 1, 1) };
        await _host.QueryAsync(async db => { db.Add(other); await db.SaveChangesAsync(); return 0; });
        var foreign = await UploadAsync(seed, FakePdf(1000), m => m.PatientId = other.Id);

        var ex = await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmDocumentService, ClaimSizeBudgetDto>(s => s.AttachToClaimAsync(claim.Id, new AttachDocumentsDto { DocumentIds = { foreign.Id } }, null)));
        Assert.AreEqual(RcmErrorKind.BadRequest, ex.Kind);

        var own = await UploadAsync(seed, FakePdf(1000, 4));
        var budget = await _host.RunAsync<IRcmDocumentService, ClaimSizeBudgetDto>(s => s.AttachToClaimAsync(claim.Id, new AttachDocumentsDto { DocumentIds = { own.Id } }, null));
        Assert.AreEqual(1, budget.AttachmentCount);
        var info = await _host.QueryAsync(db => db.ClaimSupportingInfos.AsNoTracking().SingleAsync(s => s.ClaimId == claim.Id && s.DocumentId == own.Id));
        Assert.AreEqual("attachment", info.Category);
        var listed = await _host.RunAsync<IRcmClaimWorkflowService, RcmPagedResult<RcmClaimSummaryDto>>(s => s.ListAsync(new RcmClaimQuery()));
        Assert.AreEqual(1, listed.Items.Single(c => c.Id == claim.Id).DocumentCount);

        budget = await _host.RunAsync<IRcmDocumentService, ClaimSizeBudgetDto>(s => s.DetachFromClaimAsync(claim.Id, own.Id, null));
        Assert.AreEqual(0, budget.AttachmentCount);
        listed = await _host.RunAsync<IRcmClaimWorkflowService, RcmPagedResult<RcmClaimSummaryDto>>(s => s.ListAsync(new RcmClaimQuery()));
        Assert.AreEqual(0, listed.Items.Single(c => c.Id == claim.Id).DocumentCount);
        await _host.RunAsync<IRcmDocumentService, int>(async s => { await s.DeleteAsync(own.Id, null); return 0; });
        Assert.AreEqual(1, await _host.QueryAsync(db => db.ClaimDocuments.CountAsync()));
    }

    [TestMethod]
    public async Task Candidates_match_encounter_or_unlinked_documents_in_billable_period()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_doc_candidates");
        var encounter = new Encounter
        {
            PatientId = seed.Patient.Id,
            EncounterId = "ENC-IP-777",
            IdentifierValue = "ENC-IP-777",
            Class = "IMP",
            Status = "finished",
            PeriodStart = DateTime.UtcNow.Date.AddDays(-20),
            PeriodEnd = DateTime.UtcNow.Date,
        };
        await _host.QueryAsync(async db =>
        {
            db.Add(encounter);
            await db.SaveChangesAsync();
            var c = await db.Claims.SingleAsync(x => x.Id == claim.Id);
            c.EncounterId = encounter.Id;
            await db.SaveChangesAsync();
            return 0;
        });

        var byEncounter = await UploadAsync(seed, FakePdf(900, 5), m => { m.EncounterId = encounter.Id; m.EncounterNumber = null; m.ServiceDate = DateTime.UtcNow.Date.AddDays(-15); });
        var inPeriod = await UploadAsync(seed, FakePdf(900, 6), m => { m.EncounterNumber = null; m.ServiceDate = DateTime.UtcNow.Date; });
        await UploadAsync(seed, FakePdf(900, 8), m => { m.EncounterNumber = "ENC-OLD"; m.ServiceDate = DateTime.UtcNow.Date.AddYears(-2); });
        var undated = await UploadAsync(seed, FakePdf(900, 14), m => { m.EncounterNumber = null; m.EncounterType = null; m.ServiceDate = null; });

        Assert.AreEqual("ENC-IP-777", byEncounter.EncounterNumber);
        Assert.AreEqual("IMP", byEncounter.EncounterType);

        var candidates = await _host.RunAsync<IRcmDocumentService, List<ClaimDocumentDto>>(s => s.GetCandidatesAsync(claim.Id, null));
        CollectionAssert.AreEquivalent(new[] { byEncounter.Id, inPeriod.Id, undated.Id }, candidates.Select(c => c.Id).ToList());
        Assert.IsFalse(candidates.Single(c => c.Id == undated.Id).InClaimPeriod);
    }

    [TestMethod]
    public async Task Health_flags_attachments_over_message_budget()
    {
        await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);
        _host = await RcmTestHost.CreateAsync(new Dictionary<string, string?>
        {
            ["Rcm:Documents:MaxMessageBytes"] = "100000",
            ["Rcm:Documents:WarnMessageBytes"] = "70000",
            ["Rcm:Documents:BundleOverheadBytes"] = "60000",
        });
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_doc_budget");

        await UploadAsync(seed, FakePdf(15000, 9), m => m.ClaimId = claim.Id);
        var health = await _host.RunAsync<IRcmClaimEditorService, ClaimHealthDto>(s => s.CheckHealthAsync(claim.Id, null));
        Assert.IsTrue(health.Issues.Any(i => i.Code == "ATTACHMENT_SIZE_WARN" && i.Severity == "Warning"));
        Assert.IsFalse(health.Issues.Any(i => i.Code == "ATTACHMENTS_TOO_LARGE"));

        await UploadAsync(seed, FakePdf(30000, 10), m => m.ClaimId = claim.Id);
        health = await _host.RunAsync<IRcmClaimEditorService, ClaimHealthDto>(s => s.CheckHealthAsync(claim.Id, null));
        Assert.IsTrue(health.Issues.Any(i => i.Code == "ATTACHMENTS_TOO_LARGE" && i.Severity == "Error"));
        var budget = await _host.RunAsync<IRcmDocumentService, ClaimSizeBudgetDto>(s => s.GetSizeBudgetAsync(claim.Id, null));
        Assert.AreEqual("over-limit", budget.Status);
        Assert.AreEqual(2, budget.AttachmentCount);
    }

    [TestMethod]
    public async Task Migrate_inline_attachments_moves_base64_into_store()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_doc_migrate");
        var content = FakePdf(3000, 11);
        await _host.QueryAsync(async db =>
        {
            db.ClaimSupportingInfos.Add(new ClaimSupportingInfo
            {
                ClaimId = claim.Id,
                Sequence = 1,
                Category = "attachment",
                AttachmentTitle = "Old inline report",
                AttachmentContentType = "application/pdf",
                AttachmentData = Convert.ToBase64String(content),
            });
            await db.SaveChangesAsync();
            return 0;
        });

        var result = await _host.RunAsync<IRcmDocumentService, DocumentMigrationResultDto>(s => s.MigrateInlineAttachmentsAsync(100, null));

        Assert.AreEqual(1, result.Migrated);
        Assert.AreEqual(3000, result.BytesMoved);
        var info = await _host.QueryAsync(db => db.ClaimSupportingInfos.AsNoTracking().SingleAsync(s => s.ClaimId == claim.Id));
        Assert.IsNull(info.AttachmentData);
        Assert.IsNotNull(info.DocumentId);
        var download = await _host.RunAsync<IRcmDocumentService, DocumentContent?>(s => s.DownloadAsync(info.DocumentId!, false, null));
        CollectionAssert.AreEqual(content, download!.Data);
        Assert.AreEqual(0, (await _host.RunAsync<IRcmDocumentService, DocumentMigrationResultDto>(s => s.MigrateInlineAttachmentsAsync(100, null))).Scanned);
    }

    [TestMethod]
    public async Task File_system_store_writes_outside_database()
    {
        var root = Path.Combine(Path.GetTempPath(), "rcm-docs-" + Guid.NewGuid().ToString("N"));
        await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);
        _host = await RcmTestHost.CreateAsync(new Dictionary<string, string?>
        {
            ["Rcm:Documents:Store"] = "FileSystem",
            ["Rcm:Documents:RootPath"] = root,
        });
        try
        {
            var seed = await _host.SeedPartiesAsync();
            var content = FakePdf(2500, 12);
            var doc = await UploadAsync(seed, content);

            Assert.AreEqual("FileSystem", doc.StorageProvider);
            Assert.AreEqual(1, Directory.GetFiles(root, "*", SearchOption.AllDirectories).Length);
            Assert.AreEqual(0, await _host.QueryAsync(db => db.ClaimDocumentContents.CountAsync()));
            var download = await _host.RunAsync<IRcmDocumentService, DocumentContent?>(s => s.DownloadAsync(doc.Id, false, null));
            CollectionAssert.AreEqual(content, download!.Data);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, true);
        }
    }
}
