using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

[TestClass]
public class RcmClaimWorkflowTests
{
    private RcmTestHost _host = null!;

    [TestInitialize]
    public async Task Init() => _host = await RcmTestHost.CreateAsync();

    [TestCleanup]
    public async Task Cleanup() => await _host.DisposeAsync();

    [TestMethod]
    public async Task Claim_submit_ingests_approved_adjudication_from_7b()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        _host.ReplyWith("7b - Claim Response.json");

        var result = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));

        CollectionAssert.AreEqual(Array.Empty<string>(), result.Errors);
        Assert.AreEqual("claim-request", XAssert.Single(_host.Transport.Requests).EventCode);
        Assert.AreEqual(RcmTransactionStatus.Completed, result.TransactionStatus);
        Assert.AreEqual(RcmClaimStatus.Approved, result.ClaimStatus);
        Assert.AreEqual("approved", result.AdjudicationOutcome);

        var stored = await _host.GetClaimAsync(claim.Id);
        Assert.AreEqual(RcmClaimStatus.Approved, stored.NphiesStatus);
        Assert.IsNotNull(stored.SubmittedAt);
        Assert.IsNotNull(stored.AdjudicatedAt);

        var adjudication = await _host.RunAsync<IRcmClaimWorkflowService, ClaimAdjudicationDto?>(s => s.GetLatestAdjudicationAsync(claim.Id));
        Assert.IsNotNull(adjudication);
        var item = XAssert.Single(adjudication!.Items);
        Assert.AreEqual(230m, item.Submitted);
        Assert.AreEqual(200m, item.Eligible);
        Assert.AreEqual(170m, item.Benefit);
        Assert.AreEqual(25.5m, item.Tax);
        Assert.AreEqual(30m, item.PatientShare);

        var timeline = await _host.RunAsync<IRcmClaimWorkflowService, ClaimTimelineDto?>(s => s.GetTimelineAsync(claim.Id));
        Assert.IsNotNull(timeline);
    }

    [TestMethod]
    public async Task Preview_bundle_builds_request_json_without_sending()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_preview");

        var preview = await _host.RunAsync<IRcmClaimWorkflowService, BundlePreviewDto>(s => s.PreviewBundleAsync(claim.Id));

        StringAssert.Contains(preview.Json, "\"resourceType\": \"Bundle\"");
        StringAssert.Contains(preview.Json, "claim-request");
        StringAssert.Contains(preview.Json, "req_preview");
        Assert.AreEqual(RcmOperations.Claim, preview.Operation);
        Assert.AreEqual(0, _host.Transport.Requests.Count);
        Assert.AreEqual(0, await _host.QueryAsync(db => db.NphiesTransactions.CountAsync(t => t.EntityId == claim.Id)));
        Assert.AreEqual(claim.NphiesStatus, (await _host.GetClaimAsync(claim.Id)).NphiesStatus);
    }

    [TestMethod]
    public async Task Transaction_json_returns_logged_request_and_response()
    {
        await _host.DisposeAsync();
        _host = await RcmTestHost.CreateAsync(new() { ["Nphies:EnableMessageAudit"] = "true" });
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        _host.ReplyWith("7b - Claim Response.json");
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));
        await _host.QueryAsync(async db =>
        {
            foreach (var log in _host.Audit.Logs) db.NphiesMessageLogs.Add(log);
            return await db.SaveChangesAsync();
        });

        var timeline = await _host.RunAsync<IRcmClaimWorkflowService, ClaimTimelineDto?>(s => s.GetTimelineAsync(claim.Id));
        var outbound = timeline!.Transactions.First(t => t.Direction == "outbound");
        var json = await _host.RunAsync<IRcmClaimWorkflowService, MessageJsonDto?>(s => s.GetTransactionJsonAsync(outbound.Id, null));

        Assert.IsNotNull(json);
        Assert.AreEqual("request", json!.Source);
        StringAssert.Contains(json.RequestJson, "claim-request");
        StringAssert.Contains(json.ResponseJson, "claim-response");

        var inbound = timeline.Transactions.FirstOrDefault(t => t.Direction == "inbound");
        if (inbound is not null)
        {
            var reply = await _host.RunAsync<IRcmClaimWorkflowService, MessageJsonDto?>(s => s.GetTransactionJsonAsync(inbound.Id, null));
            StringAssert.Contains(reply!.ResponseJson, "claim-response");
        }

        Assert.IsNull(await _host.RunAsync<IRcmClaimWorkflowService, MessageJsonDto?>(s => s.GetTransactionJsonAsync(outbound.Id, "other-facility")));
    }
}