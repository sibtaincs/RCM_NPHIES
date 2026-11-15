using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NPhies_FHIR_Integration.Application.Nphies.Parsing;
using NPhies_FHIR_Integration.Application.Nphies.Sandbox;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using NPhies_FHIR_Integration.Infrastructure.Nphies;
using NphiesTransaction = NPhies_FHIR_Integration.Domain.Entities.NphiesTransaction;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

/// <summary>
/// The Phase 2 transport rules, end to end against the sandbox simulator: a timeout recovered by an exact resend,
/// a duplicate answered with the stored response, the large-claim tag, and a queued answer sent by NPHIES itself.
/// </summary>
[TestClass]
public class NphiesSandboxTransportRulesTests
{
    private RcmTestHost _host = null!;
    private NphiesSandboxTransport _sandbox = null!;

    [TestInitialize]
    public async Task Init()
    {
        // The resend reads the original request from the message audit, so the audit must be on.
        _host = await RcmTestHost.CreateAsync(new() { ["Nphies:EnableMessageAudit"] = "true" });
        _sandbox = _host.Services.GetRequiredService<NphiesSandboxTransport>();
        _host.ReplyWith(Sandbox);
    }

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    private string Sandbox(NphiesTransportRequest request) => _sandbox.SendAsync(request).GetAwaiter().GetResult().Body;

    /// <summary>The test host audits into memory; the resend reads NphiesMessageLogs, so copy the records there.</summary>
    private Task PersistAuditAsync() => _host.QueryAsync(async db =>
    {
        db.NphiesMessageLogs.AddRange(_host.Audit.Logs.Where(l => !db.NphiesMessageLogs.Any(x => x.Id == l.Id)));
        return await db.SaveChangesAsync();
    });

    private Task<NphiesTransaction> OutboundAsync(string claimId) =>
        _host.QueryAsync(db => db.NphiesTransactions.AsNoTracking()
            .SingleAsync(t => t.Direction == "outbound" && t.EntityId == claimId && t.Operation == RcmOperations.Claim));

    [TestMethod]
    public async Task Timeout_is_recovered_by_an_exact_resend_and_the_duplicate_gets_the_stored_response()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");

        // NPHIES receives and answers the claim, but the answer is lost on the way back.
        string? lostAnswer = null;
        _host.ReplyWith(request =>
        {
            lostAnswer = Sandbox(request);
            throw new NphiesTransportException("The operation timed out.", isTimeout: true, requestMayHaveBeenDelivered: true);
        });
        var first = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));
        Assert.AreEqual(RcmClaimStatus.Timeout, first.ClaimStatus);
        Assert.IsNotNull(lostAnswer);
        await PersistAuditAsync();
        var tx = await OutboundAsync(claim.Id);

        string? duplicateAnswer = null;
        _host.ReplyWith(request => duplicateAnswer = Sandbox(request));
        var resend = await _host.RunAsync<IRcmResendService, ResendResultDto>(s => s.ResendAsync(tx.Id, null));

        var (original, again) = (_host.Transport.Requests[0], _host.Transport.Requests[1]);
        Assert.AreEqual(original.MessageId, again.MessageId, "MessageHeader.id must not change.");
        Assert.AreEqual(original.BundleId, again.BundleId, "Bundle.id must not change.");
        Assert.AreEqual(original.Json, again.Json, "The body must be byte-for-byte the same.");
        Assert.AreEqual(lostAnswer, duplicateAnswer, "A duplicate gets the stored response, not a new adjudication.");

        Assert.AreNotEqual(RcmTransactionStatus.Timeout, resend.TransactionStatus);
        Assert.AreNotEqual(RcmClaimStatus.Timeout, resend.ClaimStatus);
        Assert.AreEqual(1, resend.ResendCount);
        Assert.AreEqual(1, await _host.QueryAsync(db => db.NphiesTransactions.CountAsync(t => t.Direction == "outbound" && t.EntityId == claim.Id)),
            "The resend reuses the original transaction.");
    }

    [TestMethod]
    public async Task Large_claim_is_held_by_nphies_and_its_result_arrives_by_poll()
    {
        _sandbox.LargeClaimItemThreshold = 2;
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060", false, ("70000-00-00", 100m), ("70000-00-00", 120m), ("70000-00-00", 80m));

        var submit = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));

        CollectionAssert.AreEqual(Array.Empty<string>(), submit.Errors);
        Assert.AreEqual(RcmClaimStatus.Queued, submit.ClaimStatus);
        var tx = await OutboundAsync(claim.Id);
        Assert.AreEqual(RcmTransactionStatus.Queued, tx.Status);
        Assert.IsTrue(tx.NphiesGenerated);
        StringAssert.Contains(tx.MetaTags, NphiesMetaTags.LargeClaim);
        StringAssert.Contains(tx.MetaTags, NphiesMetaTags.NphiesGenerated);

        var poll = await _host.RunAsync<IRcmPollService, PollRunResultDto>(s => s.PollOnceAsync(seed.Provider.Id));

        CollectionAssert.AreEqual(Array.Empty<string>(), poll.Errors);
        CollectionAssert.Contains(poll.ClaimIds, claim.Id);
        CollectionAssert.Contains(RcmClaimStatus.Adjudicated, (await _host.GetClaimAsync(claim.Id)).NphiesStatus);
    }

    [TestMethod]
    public async Task Offline_payer_is_answered_by_nphies_as_queued_and_the_queue_is_drained_by_poll()
    {
        _sandbox.SetPayerOffline(RcmTestHost.PayerLicense);
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");

        var submit = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));

        Assert.AreEqual(RcmClaimStatus.Queued, submit.ClaimStatus);
        var tx = await OutboundAsync(claim.Id);
        Assert.AreEqual(RcmTransactionStatus.Queued, tx.Status);
        Assert.AreEqual("queued", tx.Outcome);
        Assert.IsTrue(tx.NphiesGenerated, "The sender is NPHIES, so the queue is NPHIES's and not the payer's.");
        Assert.IsNull(tx.ErrorKind);
        StringAssert.Contains(tx.MetaTags, NphiesMetaTags.NphiesGenerated);
        StringAssert.Contains(tx.MetaTags, NphiesMetaTags.QueuedMessages, "NPHIES says a message is waiting in the queue.");
        Assert.IsTrue(_sandbox.Pending(RcmTestHost.ProviderLicense) > 0);

        var poll = await _host.RunAsync<IRcmPollService, PollRunResultDto>(s => s.PollOnceAsync(seed.Provider.Id));

        CollectionAssert.AreEqual(Array.Empty<string>(), poll.Errors);
        Assert.AreEqual(0, _sandbox.Pending(RcmTestHost.ProviderLicense), "The poll keeps going while queued-messages is set.");
        CollectionAssert.Contains(RcmClaimStatus.Adjudicated, (await _host.GetClaimAsync(claim.Id)).NphiesStatus);
    }
}
