using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using NPhies_FHIR_Integration.Infrastructure.Nphies;
using NphiesTransaction = NPhies_FHIR_Integration.Domain.Entities.NphiesTransaction;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

/// <summary>
/// Recovery from a timeout by sending the identical message again (Claim Submission Guideline): same Bundle.id,
/// MessageHeader.id and body; only for the use cases NPHIES can queue.
/// </summary>
[TestClass]
public class RcmResendTests
{
    private RcmTestHost _host = null!;

    // The resend reads the original request from the message audit, so the audit must be on.
    [TestInitialize]
    public async Task Init() => _host = await RcmTestHost.CreateAsync(new() { ["Nphies:EnableMessageAudit"] = "true" });

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    private void ReplyWithTimeout() =>
        _host.ReplyWith(_ => throw new NphiesTransportException("The operation timed out.", isTimeout: true, requestMayHaveBeenDelivered: true));

    /// <summary>The test host audits into memory; the resend reads NphiesMessageLogs, so copy the records there.</summary>
    private Task PersistAuditAsync() => _host.QueryAsync(async db =>
    {
        db.NphiesMessageLogs.AddRange(_host.Audit.Logs.Where(l => !db.NphiesMessageLogs.Any(x => x.Id == l.Id)));
        return await db.SaveChangesAsync();
    });

    private Task<NphiesTransaction> OutboundAsync(string entityId) =>
        _host.QueryAsync(db => db.NphiesTransactions.AsNoTracking().SingleAsync(t => t.Direction == "outbound" && t.EntityId == entityId));

    [TestMethod]
    public async Task Timed_out_claim_is_resent_identically_and_its_stored_response_is_ingested()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        ReplyWithTimeout();
        var first = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));
        Assert.AreEqual(RcmClaimStatus.Timeout, first.ClaimStatus);
        await PersistAuditAsync();
        var tx = await OutboundAsync(claim.Id);

        _host.ReplyWith("7b - Claim Response.json");
        var resend = await _host.RunAsync<IRcmResendService, ResendResultDto>(s => s.ResendAsync(tx.Id, null));

        Assert.AreEqual(2, _host.Transport.Requests.Count);
        var (original, again) = (_host.Transport.Requests[0], _host.Transport.Requests[1]);
        Assert.AreEqual(original.MessageId, again.MessageId, "MessageHeader.id must not change.");
        Assert.AreEqual(original.BundleId, again.BundleId, "Bundle.id must not change.");
        Assert.AreEqual(original.Json, again.Json, "The body must be byte-for-byte the same.");

        Assert.AreEqual(RcmTransactionStatus.Completed, resend.TransactionStatus);
        Assert.AreEqual(1, resend.ResendCount);
        Assert.AreEqual(RcmClaimStatus.Approved, resend.ClaimStatus);
        var stored = await OutboundAsync(claim.Id);
        Assert.AreEqual(1, stored.ResendCount, "The resend updates the original transaction instead of adding one.");
        Assert.IsNotNull(stored.LastResentAt);
        Assert.AreEqual("approved", stored.AdjudicationOutcome);
    }

    [TestMethod]
    public async Task A_resend_that_times_out_again_keeps_the_claim_in_timeout()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        ReplyWithTimeout();
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));
        await PersistAuditAsync();
        var tx = await OutboundAsync(claim.Id);

        var resend = await _host.RunAsync<IRcmResendService, ResendResultDto>(s => s.ResendAsync(tx.Id, null));

        Assert.AreEqual(RcmTransactionStatus.Timeout, resend.TransactionStatus);
        Assert.AreEqual(RcmClaimStatus.Timeout, resend.ClaimStatus);
        Assert.AreEqual(1, resend.ResendCount);
    }

    [TestMethod]
    public async Task Eligibility_is_real_time_only_and_cannot_be_resent()
    {
        var seed = await _host.SeedPartiesAsync();
        ReplyWithTimeout();
        var check = await _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.CheckAsync(new EligibilityCheckRequestDto
        {
            PatientId = seed.Patient.Id,
            CoverageId = seed.Coverage.Id,
            ProviderId = seed.Provider.Id,
            InsurerId = seed.Insurer.Id,
            Purposes = { "benefits" },
        }));
        await PersistAuditAsync();
        var tx = await OutboundAsync(check.Id);
        Assert.AreEqual(RcmTransactionStatus.Timeout, tx.Status);

        var ex = await XAssert.ThrowsAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmResendService, ResendResultDto>(s => s.ResendAsync(tx.Id, null)));

        Assert.AreEqual(RcmErrorKind.Conflict, ex.Kind);
        StringAssert.Contains(ex.Message, "new");
        Assert.AreEqual(1, _host.Transport.Requests.Count, "Nothing may be sent.");
    }

    [TestMethod]
    public async Task A_transaction_that_already_has_an_answer_is_not_resent()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        _host.ReplyWith("7b - Claim Response.json");
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));
        await PersistAuditAsync();
        var tx = await OutboundAsync(claim.Id);

        var ex = await XAssert.ThrowsAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmResendService, ResendResultDto>(s => s.ResendAsync(tx.Id, null)));

        Assert.AreEqual(RcmErrorKind.Conflict, ex.Kind);
        Assert.AreEqual(1, _host.Transport.Requests.Count);
    }

    [TestMethod]
    public async Task Without_a_stored_copy_the_message_is_not_resent()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        ReplyWithTimeout();
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));
        var tx = await OutboundAsync(claim.Id); // audit records not persisted

        var ex = await XAssert.ThrowsAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmResendService, ResendResultDto>(s => s.ResendAsync(tx.Id, null)));

        Assert.AreEqual(RcmErrorKind.Conflict, ex.Kind);
        StringAssert.Contains(ex.Message, "no stored copy");
        Assert.AreEqual(1, _host.Transport.Requests.Count);
    }

    [TestMethod]
    public async Task A_transaction_of_another_provider_is_not_found()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        ReplyWithTimeout();
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));
        await PersistAuditAsync();
        var tx = await OutboundAsync(claim.Id);

        var ex = await XAssert.ThrowsAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmResendService, ResendResultDto>(s => s.ResendAsync(tx.Id, "another-hospital")));

        Assert.AreEqual(RcmErrorKind.NotFound, ex.Kind);
    }
}
