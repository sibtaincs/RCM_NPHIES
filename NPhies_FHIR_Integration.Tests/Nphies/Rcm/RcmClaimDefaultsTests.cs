using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

/// <summary>
/// The NPHIES claim profiles require Claim.extension:episode (1..1) and Claim.item.extension:patientInvoice (1..1).
/// When the claim has none, the mapper sends deterministic defaults and stores them so follow-up claims keep the episode.
/// </summary>
[TestClass]
public class RcmClaimDefaultsTests
{
    private RcmTestHost _host = null!;

    [TestInitialize]
    public async Task Init() => _host = await RcmTestHost.CreateAsync();

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    [TestMethod]
    public async Task Claim_without_episode_or_invoice_sends_and_stores_defaults()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_defaults", false, ("70000-00-00", 100m), ("70000-00-01", 50m));
        Assert.IsNull(claim.EpisodeIdentifierValue);
        _host.ReplyWith("7b - Claim Response.json");

        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));

        var sent = SentClaim(0);
        Assert.AreEqual("ep_CLM-defaults", Identifier(sent.Extension, NphiesExtensions.Episode)?.Value);
        Assert.AreEqual(2, sent.Item.Count);
        foreach (var item in sent.Item)
            Assert.AreEqual("inv_CLM-defaults", Identifier(item.Extension, NphiesExtensions.PatientInvoice)?.Value, $"item {item.Sequence}");

        var stored = await _host.QueryAsync(db => System.Threading.Tasks.Task.FromResult(
            db.Claims.Where(c => c.Id == claim.Id).Select(c => new { c.EpisodeIdentifierValue, Invoices = c.Items.Select(i => i.PatientInvoiceValue).ToList() }).Single()));
        Assert.AreEqual("ep_CLM-defaults", stored.EpisodeIdentifierValue);
        CollectionAssert.AreEqual(new[] { "inv_CLM-defaults", "inv_CLM-defaults" }, stored.Invoices);
    }

    [TestMethod]
    public async Task Recorded_episode_and_invoice_are_sent_unchanged()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_explicit");
        await _host.QueryAsync(async db =>
        {
            var c = db.Claims.Single(x => x.Id == claim.Id);
            c.EpisodeIdentifierValue = "ADM-2026-0042";
            foreach (var i in db.ClaimItems.Where(i => i.ClaimId == claim.Id)) i.PatientInvoiceValue = "INV-9001";
            await db.SaveChangesAsync();
            return 0;
        });
        _host.ReplyWith("7b - Claim Response.json");

        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));

        var sent = SentClaim(0);
        Assert.AreEqual("ADM-2026-0042", Identifier(sent.Extension, NphiesExtensions.Episode)?.Value);
        Assert.AreEqual("INV-9001", Identifier(XAssert.Single(sent.Item).Extension, NphiesExtensions.PatientInvoice)?.Value);
    }

    [TestMethod]
    public async Task Resubmission_keeps_the_original_episode()
    {
        var seed = await _host.SeedPartiesAsync();
        // 7b adjudicates request identifier req_299060; only adjudicated claims can be resubmitted.
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        _host.ReplyWith("7b - Claim Response.json");
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));

        var resubmission = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s =>
            s.ResubmitClaimAsync(claim.Id, new DerivedClaimRequestDto { Submit = true, Reason = "corrected code" }));

        Assert.AreNotEqual(claim.ClaimNumber, resubmission.ClaimNumber);
        Assert.AreEqual(2, _host.Transport.Requests.Count);
        var resent = SentClaim(1);
        Assert.AreEqual("ep_CLM-299060", Identifier(resent.Extension, NphiesExtensions.Episode)?.Value,
            "A resubmission belongs to the same episode of care as the original claim.");
        Assert.AreEqual("ep_CLM-299060", (await _host.GetClaimAsync(resubmission.ClaimId)).EpisodeIdentifierValue);
    }

    [TestMethod]
    public async Task Claim_editor_reports_the_defaults_that_will_be_sent()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_health");

        var health = await _host.RunAsync<IRcmClaimEditorService, ClaimHealthDto>(s => s.CheckHealthAsync(claim.Id, null));

        var episode = health.Issues.SingleOrDefault(i => i.Code == "EPISODE_DEFAULT");
        Assert.IsNotNull(episode, string.Join(", ", health.Issues.Select(i => i.Code)));
        Assert.AreEqual("Info", episode!.Severity);
        StringAssert.Contains(episode.Message, "ep_CLM-health");
        var invoice = health.Issues.SingleOrDefault(i => i.Code == "INVOICE_DEFAULT");
        Assert.IsNotNull(invoice);
        StringAssert.Contains(invoice!.Message, "inv_CLM-health");
    }

    private Claim SentClaim(int index)
    {
        var request = _host.Transport.Requests[index];
        Assert.AreEqual("claim-request", request.EventCode);
#pragma warning disable CS0618 // relaxed parsing, as in DocsExamples
        var bundle = new FhirJsonParser(new ParserSettings { AcceptUnknownMembers = true, AllowUnrecognizedEnums = true, PermissiveParsing = true })
            .Parse<Bundle>(request.Json);
#pragma warning restore CS0618
        return bundle.Entry.Select(e => e.Resource).OfType<Claim>().Single();
    }

    private static Identifier? Identifier(IEnumerable<Extension> extensions, string url) =>
        extensions.SingleOrDefault(e => e.Url == url)?.Value as Identifier;
}
