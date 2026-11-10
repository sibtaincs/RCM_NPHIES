using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

[TestClass]
public class RcmPriorAuthWorkflowTests
{
    private RcmTestHost _host = null!;

    [TestInitialize]
    public async Task Init() => _host = await RcmTestHost.CreateAsync();

    [TestCleanup]
    public async Task Cleanup() => await _host.DisposeAsync();

    private async Task<(RcmTestHost.Seed Seed, string PreAuthId)> SubmitPartialPreAuthAsync()
    {
        var seed = await _host.SeedPartiesAsync();
        var preAuth = await _host.SeedClaimAsync(seed, "req_298390", preAuth: true,
            ("70000-00-00", 230m), ("70000-00-01", 345m), ("70000-00-02", 1495m));
        _host.ReplyWith("2b - First Response for first Preauth Dental Request.json");

        var result = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(preAuth.Id));

        CollectionAssert.AreEqual(Array.Empty<string>(), result.Errors);
        Assert.AreEqual("priorauth-request", _host.Transport.EventCodes.Last());
        Assert.AreEqual(RcmClaimStatus.PartiallyApproved, result.ClaimStatus);
        return (seed, preAuth.Id);
    }

    [TestMethod]
    public async Task PreAuth_partial_response_stores_preauth_ref_and_item_denials()
    {
        var (_, preAuthId) = await SubmitPartialPreAuthAsync();

        var stored = await _host.GetClaimAsync(preAuthId);
        Assert.AreEqual("preauthorization", stored.Use);
        Assert.AreEqual(RcmClaimStatus.PartiallyApproved, stored.NphiesStatus);
        Assert.IsNotNull(stored.PreAuthPeriodEnd);

        var adjudication = await _host.RunAsync<IRcmClaimWorkflowService, ClaimAdjudicationDto?>(s => s.GetLatestAdjudicationAsync(preAuthId));
        Assert.AreEqual("623525114", adjudication!.PreAuthRef);
        Assert.AreEqual(3, adjudication.Items.Count);
        Assert.AreEqual("rejected", adjudication.Items.Single(i => i.Sequence == 3).Outcome);

        var denials = await _host.RunAsync<IRcmDenialService, RcmPagedResult<DenialDto>>(s => s.ListAsync(new DenialQuery { ClaimId = preAuthId }));
        CollectionAssert.AreEquivalent(new[] { "N-DC-079", "N-DC-085" }, denials.Items.Select(d => d.ReasonCode).ToArray());
        var full = denials.Items.Single(d => d.ReasonCode == "N-DC-085");
        Assert.AreEqual(3, full.ItemSequence);
        Assert.AreEqual(1495m, full.DeniedAmount);
        Assert.IsTrue(full.AppealDeadline > full.DenialDate);

        var summary = await _host.RunAsync<IRcmDenialService, DenialSummaryDto>(s => s.GetSummaryAsync(new RcmQuery()));
        Assert.AreEqual(2, summary.TotalDenials);
        Assert.AreEqual(2, summary.OpenDenials);
    }

    [TestMethod]
    public async Task PreAuth_update_and_extend_clone_with_related_reference()
    {
        var (_, preAuthId) = await SubmitPartialPreAuthAsync();
        _host.ReplyWith(RcmFixtures.Acknowledgement);

        var extend = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s =>
            s.ExtendPreAuthAsync(preAuthId, new DerivedClaimRequestDto { Submit = false, NewPeriodEnd = DateTime.UtcNow.AddDays(60) }));
        Assert.AreEqual(1, _host.Transport.Requests.Count, "Submit=false must not send");
        Assert.AreEqual(RcmClaimStatus.PartiallyApproved, (await _host.GetClaimAsync(preAuthId)).NphiesStatus);

        var update = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s =>
            s.UpdatePreAuthAsync(preAuthId, new DerivedClaimRequestDto { Reason = "add info" }));
        CollectionAssert.AreEqual(Array.Empty<string>(), update.Errors);
        Assert.AreNotEqual(preAuthId, update.ClaimId);
        Assert.AreEqual("priorauth-request", _host.Transport.EventCodes.Last());
        Assert.AreEqual(RcmClaimStatus.Superseded, (await _host.GetClaimAsync(preAuthId)).NphiesStatus);

        var denials = await _host.RunAsync<IRcmDenialService, RcmPagedResult<DenialDto>>(s => s.ListAsync(new DenialQuery { ClaimId = preAuthId }));
        Assert.IsTrue(denials.Items.All(d => d.Status == "Resubmitted" && d.ResubmissionClaimId == update.ClaimId));

        var updateTimeline = await _host.RunAsync<IRcmClaimWorkflowService, ClaimTimelineDto?>(s => s.GetTimelineAsync(update.ClaimId));
        var related = XAssert.Single(updateTimeline!.Related);
        Assert.AreEqual(preAuthId, related.ClaimId);
        Assert.AreEqual("prior", related.Relationship);
        Assert.AreEqual("req_298390", related.Identifier);

        var extendTimeline = await _host.RunAsync<IRcmClaimWorkflowService, ClaimTimelineDto?>(s => s.GetTimelineAsync(extend.ClaimId));
        Assert.AreEqual("extend", XAssert.Single(extendTimeline!.Related).Relationship);

        var original = await _host.RunAsync<IRcmClaimWorkflowService, ClaimTimelineDto?>(s => s.GetTimelineAsync(preAuthId));
        Assert.AreEqual(2, original!.Related.Count(r => r.Relationship!.EndsWith("(newer version)")));
    }

    [TestMethod]
    public async Task Claim_created_from_preauth_carries_preauth_ref_and_is_linkable()
    {
        var (seed, preAuthId) = await SubmitPartialPreAuthAsync();

        var created = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s =>
            s.CreateClaimFromPreAuthAsync(preAuthId, new DerivedClaimRequestDto { Submit = false }));
        var claim = await _host.GetClaimAsync(created.ClaimId);
        Assert.AreEqual("claim", claim.Use);
        Assert.AreEqual("623525114", claim.PreAuthorizationRef);
        Assert.AreEqual(1, _host.Transport.Requests.Count);

        // 2b grants the authorization until 2021, so linking must be refused until the period is extended.
        var other = await _host.SeedClaimAsync(seed, "req_link_1");
        var expired = await XAssert.ThrowsAsync<RcmWorkflowException>(() => _host.RunAsync<IRcmClaimWorkflowService, RcmClaimSummaryDto>(s =>
            s.LinkPreAuthAsync(other.Id, new LinkPreAuthDto { PreAuthClaimId = preAuthId })));
        StringAssert.Contains(expired.Message, "expired");

        await _host.QueryAsync(async db =>
        {
            var p = await db.Claims.SingleAsync(c => c.Id == preAuthId);
            p.PreAuthPeriodEnd = DateTime.UtcNow.AddDays(30);
            return await db.SaveChangesAsync();
        });
        var linked = await _host.RunAsync<IRcmClaimWorkflowService, RcmClaimSummaryDto>(s =>
            s.LinkPreAuthAsync(other.Id, new LinkPreAuthDto { PreAuthClaimId = preAuthId }));
        Assert.AreEqual("623525114", linked.PreAuthRef);
    }

    [TestMethod]
    public async Task Professional_preauth_approved_from_5b()
    {
        var seed = await _host.SeedPartiesAsync();
        var preAuth = await _host.SeedClaimAsync(seed, "req_177363", preAuth: true);
        _host.ReplyWith("5b - Preauth Professional Response.json");

        var result = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(preAuth.Id));

        CollectionAssert.AreEqual(Array.Empty<string>(), result.Errors);
        Assert.AreEqual(RcmTransactionStatus.Completed, result.TransactionStatus);
        Assert.AreEqual(RcmClaimStatus.Approved, result.ClaimStatus);
        var adjudication = await _host.RunAsync<IRcmClaimWorkflowService, ClaimAdjudicationDto?>(s => s.GetLatestAdjudicationAsync(preAuth.Id));
        Assert.IsFalse(string.IsNullOrEmpty(adjudication!.PreAuthRef));
    }

    [TestMethod]
    public async Task Appeal_resubmits_denied_claim_and_can_be_withdrawn()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_298390", preAuth: false,
            ("70000-00-00", 230m), ("70000-00-01", 345m), ("70000-00-02", 1495m));
        // 2b adjudicated as a claim (use=claim) gives a claim with one partial and one rejected item.
        _host.ReplyWith(_ => DocsExamples.ReadJson("2b - First Response for first Preauth Dental Request.json")
            .Replace("\"use\": \"preauthorization\"", "\"use\": \"claim\""));
        var submitted = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));
        Assert.AreEqual(RcmClaimStatus.PartiallyApproved, submitted.ClaimStatus);
        _host.ReplyWith(RcmFixtures.Acknowledgement);

        var denials = await _host.RunAsync<IRcmDenialService, RcmPagedResult<DenialDto>>(s => s.ListAsync(new DenialQuery { ClaimId = claim.Id }));
        Assert.AreEqual(2, denials.Total);

        var preAuthAppeal = await _host.SeedClaimAsync(seed, "req_pa_x", preAuth: true);
        await XAssert.ThrowsAsync<RcmWorkflowException>(() => _host.RunAsync<IRcmAppealService, AppealDto>(s =>
            s.CreateAsync(new CreateAppealDto { ClaimId = preAuthAppeal.Id, Reason = "x" })));

        var appeal = await _host.RunAsync<IRcmAppealService, AppealDto>(s => s.CreateAsync(new CreateAppealDto
        {
            ClaimId = claim.Id,
            DenialIds = denials.Items.Select(d => d.Id).ToList(),
            Reason = "Contract allows this procedure",
            Texts = { "Please reconsider, documentation attached." },
            Attachments = { new AttachmentDto { Title = "xray.pdf", Data = Convert.ToBase64String(new byte[] { 1, 2, 3 }) } },
            Resubmit = true,
        }));

        CollectionAssert.AreEqual(Array.Empty<string>(), appeal.Errors);
        Assert.IsNotNull(appeal.ResubmissionClaimId);
        Assert.AreEqual(denials.Items.Sum(d => d.DeniedAmount), appeal.AppealedAmount);
        CollectionAssert.AreEquivalent(denials.Items.Select(d => d.Id).ToList(), appeal.DenialIds);
        Assert.AreEqual(2, _host.Transport.EventCodes.Count(e => e == "claim-request"));

        var resubmission = await _host.RunAsync<IRcmClaimWorkflowService, ClaimTimelineDto?>(s => s.GetTimelineAsync(appeal.ResubmissionClaimId!));
        Assert.AreEqual("prior", XAssert.Single(resubmission!.Related).Relationship);
        var afterDenials = await _host.RunAsync<IRcmDenialService, RcmPagedResult<DenialDto>>(s => s.ListAsync(new DenialQuery { ClaimId = claim.Id }));
        Assert.IsTrue(afterDenials.Items.All(d => d.AppealRequestId == appeal.Id));

        var withdrawn = await _host.RunAsync<IRcmAppealService, AppealDto>(s => s.WithdrawAsync(appeal.Id, "settled"));
        Assert.AreEqual("Withdrawn", withdrawn.Status);
    }
}