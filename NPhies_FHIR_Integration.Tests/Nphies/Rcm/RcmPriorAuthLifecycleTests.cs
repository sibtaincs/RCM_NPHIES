using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Validation;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

/// <summary>
/// Phase 4 prior authorization lifecycle: links to the eligibility and prior authorization responses (G3), changing
/// an approved authorization (always related to the first request, same preAuthRef, all or fewer items), the cancel
/// rule for an expired authorization, and the transfer-of-care details on the response (G9).
/// </summary>
[TestClass]
public class RcmPriorAuthLifecycleTests
{
    private const string FirstRequest = "req_298390";
    private const string PreAuthRef = "623525114";
    private RcmTestHost _host = null!;

    [TestInitialize]
    public async Task Init() => _host = await RcmTestHost.CreateAsync();

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    private async Task<(RcmTestHost.Seed Seed, string PreAuthId)> ApprovedPreAuthAsync(Func<string, string>? changeResponse = null)
    {
        var seed = await _host.SeedPartiesAsync();
        var preAuth = await _host.SeedClaimAsync(seed, FirstRequest, preAuth: true, ("70000-00-00", 230m), ("70000-00-01", 345m), ("70000-00-02", 1495m));
        var json = DocsExamples.ReadJson("2b - First Response for first Preauth Dental Request.json");
        _host.ReplyWith(_ => changeResponse is null ? json : changeResponse(json));
        var result = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(preAuth.Id));
        Assert.AreEqual(RcmClaimStatus.PartiallyApproved, result.ClaimStatus);
        return (seed, preAuth.Id);
    }

    private Claim SentClaim(int index = -1)
    {
        var request = index < 0 ? _host.Transport.Requests.Last() : _host.Transport.Requests[index];
        var bundle = new FhirJsonParser().Parse<Bundle>(request.Json);
        // The seeded test claim has no chief complaint; every other profile rule must hold, including the new extensions.
        var errors = new NphiesProfileValidator(NphiesConformancePackage.Embedded).Validate(bundle).Errors
            .Where(e => !e.Message.Contains("chief", StringComparison.OrdinalIgnoreCase)).ToList();
        Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors.Select(e => $"{e.RuleId} {e.Expression}: {e.Message}")));
        return bundle.Entry.Select(e => e.Resource).OfType<Claim>().Single();
    }

    private Task<SubmissionResultDto> UpdateAsync(string id, DerivedClaimRequestDto? request = null) =>
        _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.UpdatePreAuthAsync(id, request ?? new DerivedClaimRequestDto()));

    [TestMethod]
    public async Task Every_new_version_relates_to_the_first_request_and_keeps_the_preauth_ref()
    {
        var (_, a) = await ApprovedPreAuthAsync();
        _host.ReplyWith(RcmFixtures.Acknowledgement);

        var b = await UpdateAsync(a);
        var c = await UpdateAsync(b.ClaimId);

        foreach (var index in new[] { 1, 2 })
        {
            var sent = SentClaim(index);
            var related = sent.Related.Single();
            Assert.AreEqual(FirstRequest, related.Claim.Identifier.Value, "Always the first request (A), never the version in between (B).");
            Assert.AreEqual("prior", related.Relationship.Coding.Single().Code);
            CollectionAssert.AreEqual(new[] { PreAuthRef }, sent.Insurance.Single().PreAuthRef.ToList(), "The same preAuthRef is kept.");
            Assert.AreEqual(3, sent.Item.Count, "All items are sent again.");
        }

        // Our own chain still links each version to the one it replaced.
        var timeline = await _host.RunAsync<IRcmClaimWorkflowService, ClaimTimelineDto?>(s => s.GetTimelineAsync(c.ClaimId));
        Assert.AreEqual(b.ClaimId, timeline!.Related.Single().ClaimId);
        Assert.AreEqual(RcmClaimStatus.Superseded, (await _host.GetClaimAsync(a)).NphiesStatus);
        Assert.AreEqual(RcmClaimStatus.Superseded, (await _host.GetClaimAsync(b.ClaimId)).NphiesStatus);
    }

    [TestMethod]
    public async Task Items_can_be_removed_from_an_adjudicated_authorization_but_not_a_pending_one()
    {
        var (_, a) = await ApprovedPreAuthAsync();
        _host.ReplyWith(RcmFixtures.Acknowledgement);

        var unknown = await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() => UpdateAsync(a, new DerivedClaimRequestDto { RemoveItemSequences = { 9 } }));
        Assert.AreEqual(RcmErrorKind.BadRequest, unknown.Kind);
        await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() => UpdateAsync(a, new DerivedClaimRequestDto { RemoveItemSequences = { 1, 2, 3 } }), "At least one item must remain.");

        var b = await UpdateAsync(a, new DerivedClaimRequestDto { RemoveItemSequences = { 3 } });

        var sent = SentClaim();
        CollectionAssert.AreEqual(new int?[] { 1, 2 }, sent.Item.Select(i => i.Sequence).ToList());
        Assert.AreEqual(575m, (await _host.GetClaimAsync(b.ClaimId)).Total, "The total follows the remaining items.");

        // B was only acknowledged: the payer has not adjudicated it, so items cannot be dropped from it.
        var pending = await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() => UpdateAsync(b.ClaimId, new DerivedClaimRequestDto { RemoveItemSequences = { 2 } }));
        Assert.AreEqual(RcmErrorKind.Conflict, pending.Kind);
        StringAssert.Contains(pending.Message, "Cancel it");
    }

    [TestMethod]
    public async Task A_partly_done_item_keeps_the_done_quantity_and_the_rest_becomes_a_new_item()
    {
        var (_, a) = await ApprovedPreAuthAsync();
        await _host.QueryAsync(async db =>
        {
            var item = await db.ClaimItems.SingleAsync(i => i.ClaimId == a && i.Sequence == 2);
            item.Quantity = 4;
            item.UnitPrice = 86.25m; // net 345 for 4 sessions
            return await db.SaveChangesAsync();
        });
        _host.ReplyWith(RcmFixtures.Acknowledgement);

        foreach (var bad in new[] { 0m, 4m, 5m })
            await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() => UpdateAsync(a, new DerivedClaimRequestDto { SplitItems = { new SplitItemDto { Sequence = 2, DoneQuantity = bad } } }));

        var b = await UpdateAsync(a, new DerivedClaimRequestDto { SplitItems = { new SplitItemDto { Sequence = 2, DoneQuantity = 1 } } });

        var items = await _host.QueryAsync(db => db.ClaimItems.AsNoTracking().Where(i => i.ClaimId == b.ClaimId).OrderBy(i => i.Sequence).ToListAsync());
        Assert.AreEqual(4, items.Count, "The remainder is a new item.");
        var done = items.Single(i => i.Sequence == 2);
        var rest = items.Single(i => i.Sequence == 4);
        Assert.AreEqual(1m, done.Quantity);
        Assert.AreEqual(86.25m, done.Net);
        Assert.IsNotNull(done.ServicedDate, "The part already done keeps its service date.");
        Assert.AreEqual(3m, rest.Quantity);
        Assert.AreEqual(258.75m, rest.Net);
        Assert.IsNull(rest.ServicedDate, "The rest is still planned.");
        Assert.AreEqual(done.ProductOrServiceCode, rest.ProductOrServiceCode);
        Assert.AreEqual(2070m, items.Sum(i => i.Net), "Nothing is added or lost.");
        Assert.AreEqual(4, SentClaim().Item.Count);
    }

    [TestMethod]
    public async Task An_expired_authorization_cannot_be_cancelled()
    {
        var (_, a) = await ApprovedPreAuthAsync(); // 2b grants the authorization until 2021
        var before = _host.Transport.Requests.Count;

        var ex = await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmClaimWorkflowService, TaskResultDto>(s => s.CancelAsync(a, new CancelClaimRequestDto { Reason = "No longer needed" })));

        Assert.AreEqual(RcmErrorKind.Conflict, ex.Kind);
        StringAssert.Contains(ex.Message, "expired");
        Assert.AreEqual(before, _host.Transport.Requests.Count, "Nothing is sent.");
    }

    [TestMethod]
    public async Task A_claim_links_to_its_eligibility_and_prior_authorization_responses()
    {
        var (seed, a) = await ApprovedPreAuthAsync();
        _host.ReplyWith("14b - Eligibility Response Ben-Val.json");
        var check = await _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.CheckAsync(new EligibilityCheckRequestDto
        {
            PatientId = seed.Patient.Id, CoverageId = seed.Coverage.Id, ProviderId = seed.Provider.Id, Purposes = { "validation", "benefits" },
        }));
        Assert.AreEqual(true, check.IsEligible);
        var stored = await _host.QueryAsync(db => db.EligibilityChecks.AsNoTracking().SingleAsync(e => e.Id == check.Id));
        Assert.IsNotNull(stored.ResponseIdentifierSystem);

        var created = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.CreateClaimFromPreAuthAsync(a, new DerivedClaimRequestDto { Submit = false }));
        _host.ReplyWith(RcmFixtures.Acknowledgement);
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(created.ClaimId));

        var sent = SentClaim();
        var eligibility = sent.GetExtension(NphiesExtensions.EligibilityResponse)?.Value as ResourceReference;
        Assert.AreEqual(stored.ResponseIdentifier, eligibility?.Identifier?.Value, "The stored eligibility response is referenced by its identifier.");
        Assert.AreEqual(stored.ResponseIdentifierSystem, eligibility?.Identifier?.System);

        var response = await _host.QueryAsync(db => db.ClaimResponses.AsNoTracking().SingleAsync(r => r.ClaimId == a));
        var priorAuth = sent.GetExtension(NphiesExtensions.PriorAuthResponse)?.Value as ResourceReference;
        Assert.AreEqual(response.ResponseIdentifierValue, priorAuth?.Identifier?.Value, "The prior authorization response is referenced by its identifier.");
        Assert.AreEqual(check.Id, (await _host.GetClaimAsync(created.ClaimId)).EligibilityCheckId, "The link is remembered on the claim.");
    }

    [TestMethod]
    public async Task An_offline_eligibility_record_is_carried_instead_of_a_response_link()
    {
        var seed = await _host.SeedPartiesAsync();
        await _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.RecordOfflineAsync(new OfflineEligibilityRequestDto
        {
            PatientId = seed.Patient.Id, ProviderId = seed.Provider.Id, Reference = "PORTAL-1", Date = DateTime.UtcNow.AddHours(-2),
        }));
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        _host.ReplyWith("7b - Claim Response.json");

        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));

        var sent = SentClaim();
        Assert.AreEqual("PORTAL-1", (sent.GetExtension(NphiesExtensions.EligibilityOfflineReference)?.Value as FhirString)?.Value);
        Assert.IsNull(sent.GetExtension(NphiesExtensions.EligibilityResponse));
    }

    [TestMethod]
    public async Task The_receiving_provider_of_a_transfer_is_warned_until_the_claim_carries_the_transfer_authorization()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        Task<List<string>> WarningsAsync() => _host.RunAsync<IRcmClaimEditorService, ClaimHealthDto>(s => s.CheckHealthAsync(claim.Id, null))
            .ContinueWith(t => t.Result.Issues.Where(i => i.Code.StartsWith("TRANSFER_")).Select(i => i.Code).ToList());
        Assert.AreEqual(0, (await WarningsAsync()).Count, "An ordinary visit needs no transfer details.");

        await _host.QueryAsync(async db =>
        {
            db.Add(new NPhies_FHIR_Integration.Domain.Entities.ClaimSupportingInfo
            {
                ClaimId = claim.Id, Sequence = 1, Category = "reason-for-visit", CodeValue = "referral",
                CodeSystem = NphiesCodeSystems.VisitReason,
            });
            return await db.SaveChangesAsync();
        });
        CollectionAssert.AreEquivalent(new[] { "TRANSFER_PREAUTH_MISSING", "TRANSFER_DATE_MISSING" }, await WarningsAsync());

        await _host.QueryAsync(async db =>
        {
            var c = await db.Claims.SingleAsync(x => x.Id == claim.Id);
            c.PreAuthorizationRef = "TRF-778899";
            c.AuthorizationOfflineDate = DateTime.UtcNow.Date;
            c.ReferralIdentifier = "Referring Hospital";
            return await db.SaveChangesAsync();
        });
        Assert.AreEqual(0, (await WarningsAsync()).Count);

        _host.ReplyWith("7b - Claim Response.json");
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));
        var sent = SentClaim();
        Assert.AreEqual("Referring Hospital", sent.Referral?.Display, "Who referred the patient is sent as Claim.referral.");
        CollectionAssert.AreEqual(new[] { "TRF-778899" }, sent.Insurance.Single().PreAuthRef.ToList());
        Assert.IsNotNull(sent.GetExtension(NphiesExtensions.AuthorizationOfflineDate));
    }

    [TestMethod]
    public async Task Transfer_of_care_details_on_the_response_are_stored()
    {
        var (_, a) = await ApprovedPreAuthAsync(json =>
        {
            var bundle = new FhirJsonParser().Parse<Bundle>(json);
            var response = bundle.Entry.Select(e => e.Resource).OfType<ClaimResponse>().Single();
            response.Extension.Add(new Extension(NphiesExtensions.TransferAuthorizationNumber, new FhirString("TRF-778899")));
            response.Extension.Add(new Extension(NphiesExtensions.TransferAuthorizationPeriod, new Period { Start = "2026-11-01", End = "2026-12-01" }));
            response.Extension.Add(new Extension(NphiesExtensions.TransferAuthorizationProvider,
                new ResourceReference { Identifier = new Identifier(NphiesIdentifierSystems.ProviderLicense, "PR-RECEIVER"), Display = "Receiving Hospital" }));
            return new FhirJsonSerializer().SerializeToString(bundle);
        });

        var stored = await _host.QueryAsync(db => db.ClaimResponses.AsNoTracking().SingleAsync(r => r.ClaimId == a));

        Assert.AreEqual("TRF-778899", stored.TransferAuthorizationNumber);
        Assert.AreEqual(new DateTime(2026, 11, 1), stored.TransferAuthorizationPeriodStart);
        Assert.AreEqual(new DateTime(2026, 12, 1), stored.TransferAuthorizationPeriodEnd);
        Assert.AreEqual("PR-RECEIVER", stored.TransferAuthorizationProvider);
    }
}
