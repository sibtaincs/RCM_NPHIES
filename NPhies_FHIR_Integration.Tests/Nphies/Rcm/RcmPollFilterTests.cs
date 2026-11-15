using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using FhirTask = Hl7.Fhir.Model.Task;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

/// <summary>Poll filters from the IG polling use case: count, include or exclude message types, period and focus.</summary>
[TestClass]
public class RcmPollFilterTests
{
    private RcmTestHost _host = null!;

    [TestInitialize]
    public async Task Init() => _host = await RcmTestHost.CreateAsync();

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    /// <summary>The poll Task of the n-th message sent.</summary>
    private FhirTask SentTask(int index = 0)
    {
        var bundle = new FhirJsonParser().Parse<Bundle>(_host.Transport.Requests[index].Json);
        return bundle.Entry.Select(e => e.Resource).OfType<FhirTask>().Single();
    }

    private static List<string> InputCodes(FhirTask task, string type) =>
        task.Input.Where(i => i.Type.Coding.Any(c => c.Code == type)).Select(i => ((Code)i.Value).Value).ToList();

    private async Task<string> SeedSentClaimAsync(RcmTestHost.Seed seed, string identifier)
    {
        var claim = await _host.SeedClaimAsync(seed, identifier);
        await _host.QueryAsync(async db =>
        {
            var c = await db.Claims.SingleAsync(x => x.Id == claim.Id);
            c.NphiesStatus = RcmClaimStatus.Queued;
            c.SubmittedAt = DateTime.UtcNow.AddHours(-1);
            return await db.SaveChangesAsync();
        });
        return claim.Id;
    }

    [TestMethod]
    public async Task Focus_poll_sends_the_claim_identifier_and_ingests_its_response()
    {
        var seed = await _host.SeedPartiesAsync();
        var claimId = await SeedSentClaimAsync(seed, "iMXbxXMGMR");
        _host.ReplyWith("12b - Poll Response (Do Not Process).json");

        var result = await _host.RunAsync<IRcmPollService, PollRunResultDto>(s => s.PollAsync(new PollRequestDto { FocusClaimId = claimId }));

        CollectionAssert.AreEqual(Array.Empty<string>(), result.Errors);
        Assert.AreEqual(1, result.PollRequests, "A focused poll asks once, even when the response says more messages are queued.");
        Assert.AreEqual("iMXbxXMGMR", SentTask().Focus?.Identifier?.Value);
        CollectionAssert.Contains(result.ClaimIds, claimId);
        Assert.AreEqual(RcmClaimStatus.Approved, (await _host.GetClaimAsync(claimId)).NphiesStatus);
    }

    [TestMethod]
    public async Task Include_types_count_and_period_are_sent_as_task_inputs()
    {
        var seed = await _host.SeedPartiesAsync();

        await _host.RunAsync<IRcmPollService, PollRunResultDto>(s => s.PollAsync(new PollRequestDto
        {
            ProviderId = seed.Provider.Id,
            Count = 5,
            IncludeMessageTypes = { "payment-reconciliation" },
            PeriodStart = new DateTime(2026, 11, 1),
            PeriodEnd = new DateTime(2026, 11, 30),
        }));

        var task = SentTask();
        CollectionAssert.AreEqual(new[] { "payment-reconciliation" }, InputCodes(task, "include-message-type"));
        Assert.AreEqual(0, InputCodes(task, "exclude-message-type").Count);
        Assert.AreEqual(5, ((PositiveInt)task.Input.Single(i => i.Type.Coding.Any(c => c.Code == "count")).Value).Value);
        var period = (Period)task.Input.Single(i => i.Type.Coding.Any(c => c.Code == "period")).Value;
        Assert.AreEqual("2026-11-01", period.Start);
        Assert.AreEqual("2026-11-30", period.End);
        Assert.IsNull(task.Focus);
    }

    [TestMethod]
    public async Task Exclude_types_are_sent_as_task_inputs()
    {
        var seed = await _host.SeedPartiesAsync();

        await _host.RunAsync<IRcmPollService, PollRunResultDto>(s => s.PollAsync(new PollRequestDto
        {
            ProviderId = seed.Provider.Id,
            ExcludeMessageTypes = { "payment-reconciliation" },
        }));

        CollectionAssert.AreEqual(new[] { "payment-reconciliation" }, InputCodes(SentTask(), "exclude-message-type"));
    }

    [TestMethod]
    public async Task Invalid_filters_are_rejected_before_anything_is_sent()
    {
        var seed = await _host.SeedPartiesAsync();
        var unsent = await _host.SeedClaimAsync(seed, "req_not_sent");

        var both = await XAssert.ThrowsAsync<RcmWorkflowException>(() => _host.RunAsync<IRcmPollService, PollRunResultDto>(s => s.PollAsync(new PollRequestDto
        {
            ProviderId = seed.Provider.Id,
            IncludeMessageTypes = { "claim-response" },
            ExcludeMessageTypes = { "payment-reconciliation" },
        })));
        Assert.AreEqual(RcmErrorKind.BadRequest, both.Kind);

        var count = await XAssert.ThrowsAsync<RcmWorkflowException>(() => _host.RunAsync<IRcmPollService, PollRunResultDto>(s =>
            s.PollAsync(new PollRequestDto { ProviderId = seed.Provider.Id, Count = 101 })));
        Assert.AreEqual(RcmErrorKind.BadRequest, count.Kind);

        var notSent = await XAssert.ThrowsAsync<RcmWorkflowException>(() => _host.RunAsync<IRcmPollService, PollRunResultDto>(s =>
            s.PollAsync(new PollRequestDto { FocusClaimId = unsent.Id })));
        Assert.AreEqual(RcmErrorKind.Conflict, notSent.Kind);

        Assert.AreEqual(0, _host.Transport.Requests.Count);
    }
}
