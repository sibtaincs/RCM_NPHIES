using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Parsing;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using NphiesTransaction = NPhies_FHIR_Integration.Domain.Entities.NphiesTransaction;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

/// <summary>The dispatcher stores the error kind, adjudication outcome and meta-tags, and acts on the tags.</summary>
[TestClass]
public class RcmResponseClassificationTests
{
    private RcmTestHost _host = null!;

    [TestInitialize]
    public async Task Init() => _host = await RcmTestHost.CreateAsync();

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    /// <summary>The 7b claim response, changed by <paramref name="change"/> and serialized back to JSON.</summary>
    private static string ClaimResponseJson(Action<Bundle, MessageHeader, ClaimResponse> change)
    {
        var bundle = DocsExamples.LoadBundle("7b - Claim Response.json");
        var header = bundle.Entry.Select(e => e.Resource).OfType<MessageHeader>().First();
        var claimResponse = bundle.Entry.Select(e => e.Resource).OfType<ClaimResponse>().First();
        change(bundle, header, claimResponse);
        return new FhirJsonSerializer().SerializeToString(bundle);
    }

    private static void Tag(MessageHeader header, params string[] codes)
    {
        header.Meta ??= new Meta();
        foreach (var code in codes) header.Meta.Tag.Add(new Coding(NphiesCodeSystems.MetaTags, code));
    }

    private Task<NphiesTransaction> OutboundTransactionAsync(string claimId) =>
        _host.QueryAsync(db => db.NphiesTransactions.SingleAsync(t => t.Direction == "outbound" && t.EntityId == claimId));

    [TestMethod]
    public async Task Approved_claim_stores_the_adjudication_outcome_and_no_error_kind()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        _host.ReplyWith("7b - Claim Response.json");

        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));

        var tx = await OutboundTransactionAsync(claim.Id);
        Assert.AreEqual(RcmTransactionStatus.Completed, tx.Status);
        Assert.AreEqual("approved", tx.AdjudicationOutcome);
        Assert.IsNull(tx.ErrorKind);
        Assert.IsNull(tx.MetaTags);
        Assert.IsFalse(tx.NphiesGenerated);
    }

    [TestMethod]
    public async Task Large_claim_response_from_nphies_is_queued_and_marked_nphies_generated()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        var json = ClaimResponseJson((_, header, response) =>
        {
            Tag(header, NphiesMetaTags.LargeClaim, NphiesMetaTags.NphiesGenerated);
            response.Outcome = ClaimProcessingCodes.Queued;
            response.GetExtension(NphiesExtensions.AdjudicationOutcome)!.Value = new CodeableConcept(NphiesCodeSystems.AdjudicationOutcome, "pended");
        });
        _host.ReplyWith(_ => json);

        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));

        var tx = await OutboundTransactionAsync(claim.Id);
        Assert.AreEqual(RcmTransactionStatus.Queued, tx.Status);
        Assert.IsTrue(tx.NphiesGenerated);
        StringAssert.Contains(tx.MetaTags, NphiesMetaTags.LargeClaim);
        Assert.AreEqual("pended", tx.AdjudicationOutcome);
    }

    [TestMethod]
    public async Task Queued_messages_tag_wakes_the_poll_worker()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        var json = ClaimResponseJson((_, header, _) => Tag(header, NphiesMetaTags.QueuedMessages));
        _host.ReplyWith(_ => json);
        var signal = _host.Services.GetRequiredService<IRcmPollSignal>();

        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));

        var reason = await signal.WaitAsync(TimeSpan.Zero, CancellationToken.None);
        Assert.IsNotNull(reason, "A queued-messages tag must request an early poll.");
        StringAssert.Contains(reason, NphiesMetaTags.QueuedMessages);
    }

    [TestMethod]
    public async Task Rejected_claim_is_recorded_as_a_rejection()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        var json = ClaimResponseJson((_, _, response) =>
            response.GetExtension(NphiesExtensions.AdjudicationOutcome)!.Value = new CodeableConcept(NphiesCodeSystems.AdjudicationOutcome, "rejected"));
        _host.ReplyWith(_ => json);

        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));

        var tx = await OutboundTransactionAsync(claim.Id);
        Assert.AreEqual(nameof(NphiesResponseErrorKind.Rejection), tx.ErrorKind);
        Assert.AreEqual("rejected", tx.AdjudicationOutcome);
    }
}
