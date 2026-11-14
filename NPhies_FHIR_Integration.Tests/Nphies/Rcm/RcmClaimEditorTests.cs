using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using NPhies_FHIR_Integration.Application.Services;
using NPhies_FHIR_Integration.Application.DTOs;
using Microsoft.EntityFrameworkCore;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

[TestClass]
public class RcmClaimEditorTests
{
    private RcmTestHost _host = null!;

    [TestInitialize]
    public async Task Init() => _host = await RcmTestHost.CreateAsync();

    [TestCleanup]
    public async Task Cleanup() => await _host.DisposeAsync();

    private static ClaimEditDto NewDraft(RcmTestHost.Seed seed) => new()
    {
        Use = "claim",
        ClaimType = "oral",
        ClaimSubType = "op",
        PatientId = seed.Patient.Id,
        CoverageId = seed.Coverage.Id,
        ProviderId = seed.Provider.Id,
        InsurerId = seed.Insurer.Id,
        PractitionerId = seed.Practitioner.Id,
        ServicedPeriodStart = DateTime.UtcNow.Date,
        Diagnoses = { new ClaimEditDiagnosisDto { Sequence = 1, DiagnosisCode = "K02.1", DiagnosisType = "principal" } },
        CareTeam = { new ClaimEditCareTeamDto { Sequence = 1, PractitionerId = seed.Practitioner.Id, Role = "primary", Qualification = "22.00" } },
        SupportingInfo = { new ClaimEditSupportingInfoDto { Sequence = 1, Category = "chief-complaint", CodeValue = "K02.1", StringValue = "Tooth sensitivity" } },
        Items =
        {
            new ClaimEditItemDto
            {
                Sequence = 1, ProductOrServiceCode = "70000-00-00", ProductOrServiceSystem = "http://nphies.sa/terminology/CodeSystem/oral-health-op",
                Quantity = 2, UnitPrice = 100m, TaxRate = 15m, PatientShare = 20m, CareTeamSequence = 1, DiagnosisSequences = { 1 },
                ServicedDate = DateTime.UtcNow.Date, BodySiteCode = "11",
            },
        },
    };

    [TestMethod]
    public async Task Create_and_update_draft_recalculates_totals_and_audits_changes()
    {
        var seed = await _host.SeedPartiesAsync();
        var created = await _host.RunAsync<IRcmClaimEditorService, ClaimEditDto>(s => s.CreateAsync(NewDraft(seed), "tester", null));

        Assert.IsTrue(created.Editable);
        Assert.AreEqual(RcmClaimStatus.Draft, created.NphiesStatus);
        StringAssert.StartsWith(created.ClaimNumber, "CL-");
        var item = XAssert.Single(created.Items);
        Assert.AreEqual(30m, item.Tax);          // 200 × 15%
        Assert.AreEqual(230m, item.Net);
        Assert.AreEqual(230m, created.Total);
        Assert.AreEqual(30m, created.TotalTax);

        created.Items[0].Quantity = 3;
        created.ChangeReason = "Extra unit performed";
        var updated = await _host.RunAsync<IRcmClaimEditorService, ClaimEditDto>(s => s.UpdateAsync(created.Id!, created, "tester", null));
        Assert.AreEqual(345m, updated.Total);

        var audit = await _host.RunAsync<IRcmClaimEditorService, List<ClaimAuditEntryDto>>(s => s.GetAuditAsync(created.Id!, null));
        Assert.AreEqual(2, audit.Count);
        var edit = audit.First(a => a.Action == "ClaimEdited");
        Assert.AreEqual("Extra unit performed", edit.Reason);
        Assert.IsTrue(edit.Changes.Any(c => c.Field == "items[1]" && c.After != null && c.After.Contains("×3")),
            string.Join(", ", edit.Changes.Select(c => $"{c.Field}={c.After}")));

        var health = await _host.RunAsync<IRcmClaimEditorService, ClaimHealthDto>(s => s.CheckHealthAsync(created.Id!, null));
        Assert.IsTrue(health.Ready, string.Join("; ", health.Issues.Select(i => $"{i.Code}: {i.Message}")));
    }

    [TestMethod]
    public async Task Health_check_flags_current_ig_requirements()
    {
        var seed = await _host.SeedPartiesAsync();
        var draft = NewDraft(seed);
        draft.SupportingInfo.Clear();
        draft.SupportingInfo.Add(new ClaimEditSupportingInfoDto { Sequence = 1, Category = "onset" });
        draft.Items[0].SubSiteCode = "M,O";
        var created = await _host.RunAsync<IRcmClaimEditorService, ClaimEditDto>(s => s.CreateAsync(draft, "tester", null));

        var health = await _host.RunAsync<IRcmClaimEditorService, ClaimHealthDto>(s => s.CheckHealthAsync(created.Id!, null));

        var codes = health.Issues.Where(i => i.Severity == "Error").Select(i => i.Code).ToList();
        Assert.IsFalse(health.Ready);
        foreach (var expected in new[] { "CLINICAL_INFO_REQUIRED", "INFO_TIMING", "INFO_ONSET_CODE", "ITEM_SUBSITE" })
            CollectionAssert.Contains(codes, expected, string.Join(", ", codes));
    }

    [TestMethod]
    public async Task Submitted_claim_is_locked_for_editor_and_legacy_put()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        _host.ReplyWith("7b - Claim Response.json");
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));

        var view = await _host.RunAsync<IRcmClaimEditorService, ClaimEditDto>(s => s.GetAsync(claim.Id, null));
        Assert.IsFalse(view.Editable);
        Assert.IsFalse(string.IsNullOrWhiteSpace(view.LockedReason));

        var ex = await XAssert.ThrowsAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmClaimEditorService, ClaimEditDto>(s => s.UpdateAsync(claim.Id, view, "tester", null)));
        Assert.AreEqual(RcmErrorKind.Conflict, ex.Kind);

        var legacy = await XAssert.ThrowsAsync<RcmWorkflowException>(() =>
            _host.QueryAsync(db => new ClaimService(new NPhies_FHIR_Integration.Infrastructure.Repositories.ClaimRepository(db))
                .UpdateClaimAsync(claim.Id, new UpdateClaimDto { Status = "Paid", Total = 1m })));
        Assert.AreEqual(RcmErrorKind.Conflict, legacy.Kind);
        var stored = await _host.GetClaimAsync(claim.Id);
        Assert.AreEqual(230m, stored.Total);

        var options = await _host.RunAsync<IRcmClaimEditorService, ClaimCorrectionOptionsDto>(s => s.GetCorrectionOptionsAsync(claim.Id, null));
        Assert.IsFalse(options.Editable);
        Assert.IsTrue(options.Options.Any(o => o.Action == "resubmit"));
        Assert.IsFalse(options.Options.Any(o => o.Action == "edit"));
    }

    [TestMethod]
    public async Task Health_flags_broken_references_and_unknown_preauth()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_health");
        await _host.QueryAsync(async db =>
        {
            var c = await db.Claims.Include(x => x.Items).SingleAsync(x => x.Id == claim.Id);
            c.PreAuthorizationRef = "PA-NOT-REAL";
            c.Items.First().CareTeamSequence = 9;
            c.Items.First().DiagnosisSequence = "4";
            await db.SaveChangesAsync();
            return 0;
        });

        var health = await _host.RunAsync<IRcmClaimEditorService, ClaimHealthDto>(s => s.CheckHealthAsync(claim.Id, null));
        var codes = health.Issues.Select(i => i.Code).ToList();
        Assert.IsFalse(health.Ready);
        CollectionAssert.Contains(codes, "ITEM_CARETEAM_REF");
        CollectionAssert.Contains(codes, "ITEM_DIAGNOSIS_REF");
        CollectionAssert.Contains(codes, "PREAUTH_UNKNOWN");

        var report = await _host.RunAsync<IRcmClaimEditorService, List<ClaimHealthDto>>(s => s.CheckHealthBulkAsync(new RcmClaimQuery(), true));
        Assert.IsTrue(report.Any(r => r.ClaimId == claim.Id));
    }

    [TestMethod]
    public async Task Resubmission_draft_is_editable_and_compare_shows_changes()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        _host.ReplyWith("7b - Claim Response.json");
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));

        var draft = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s =>
            s.ResubmitClaimAsync(claim.Id, new DerivedClaimRequestDto { Submit = false, Reason = "price correction" }));

        var view = await _host.RunAsync<IRcmClaimEditorService, ClaimEditDto>(s => s.GetAsync(draft.ClaimId, null));
        Assert.IsTrue(view.Editable, view.LockedReason);
        view.Items[0].UnitPrice = 250m;
        view.ChangeReason = "price correction";
        await _host.RunAsync<IRcmClaimEditorService, ClaimEditDto>(s => s.UpdateAsync(draft.ClaimId, view, "tester", null));

        var compare = await _host.RunAsync<IRcmClaimEditorService, ClaimVersionCompareDto>(s => s.CompareWithPreviousAsync(draft.ClaimId, null));
        Assert.AreEqual(claim.Id, compare.PreviousClaimId);
        Assert.IsTrue(compare.Changes.Any(c => c.Field.Contains("items[1]") && c.After != null && c.After.Contains("250")),
            string.Join(", ", compare.Changes.Select(c => $"{c.Field}={c.After}")));
    }
}
