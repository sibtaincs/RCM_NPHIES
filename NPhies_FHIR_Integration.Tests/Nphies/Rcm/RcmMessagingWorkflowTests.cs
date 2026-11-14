using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

[TestClass]
public class RcmMessagingWorkflowTests
{
    private RcmTestHost _host = null!;

    [TestInitialize]
    public async Task Init() => _host = await RcmTestHost.CreateAsync();

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    [TestMethod]
    public async Task Eligibility_check_stores_benefits_from_14b()
    {
        var seed = await _host.SeedPartiesAsync();
        _host.ReplyWith("14b - Eligibility Response Ben-Val.json");

        var result = await _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.CheckAsync(new EligibilityCheckRequestDto
        {
            PatientId = seed.Patient.Id,
            CoverageId = seed.Coverage.Id,
            ProviderId = seed.Provider.Id,
            InsurerId = seed.Insurer.Id,
            Purposes = { "benefits" },
        }));

        Assert.AreEqual("eligibility-request", XAssert.Single(_host.Transport.Requests).EventCode);
        Assert.AreEqual(RcmTransactionStatus.Completed, result.Status, result.Errors?.ToString());
        Assert.AreEqual("complete", result.Outcome);
        Assert.IsTrue(result.IsEligible);
        Assert.IsNotNull(result.Benefits);

        var latest = await _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto?>(s => s.GetLatestForPatientAsync(seed.Patient.Id, null));
        Assert.AreEqual(result.Id, latest!.Id);
    }

    [TestMethod]
    public async Task Eligibility_discovery_error_from_15c_is_stored_as_error()
    {
        var seed = await _host.SeedPartiesAsync();
        _host.ReplyWith("15c - Eligibility Response Discovery - Outcome_Error.json");

        var result = await _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.CheckAsync(new EligibilityCheckRequestDto
        {
            PatientId = seed.Patient.Id,
            ProviderId = seed.Provider.Id,
            InsurerId = seed.Insurer.Id,
            Purposes = { "discovery" },
        }));

        Assert.AreEqual("Error", result.Status);
        Assert.AreEqual("error", result.Outcome);
        Assert.IsNotNull(result.Errors);
    }

    private async Task<string> SeedSubmittedClaimAsync(RcmTestHost.Seed seed, string identifier, string status)
    {
        var claim = await _host.SeedClaimAsync(seed, identifier);
        await _host.QueryAsync(async db =>
        {
            var c = await db.Claims.SingleAsync(x => x.Id == claim.Id);
            c.NphiesStatus = status;
            c.SubmittedAt = DateTime.UtcNow.AddHours(-2);
            return await db.SaveChangesAsync();
        });
        return claim.Id;
    }

    [TestMethod]
    public async Task Status_check_moves_timed_out_claim_to_queued_from_9c()
    {
        var seed = await _host.SeedPartiesAsync();
        var claimId = await SeedSubmittedClaimAsync(seed, "req_00112487303", RcmClaimStatus.Timeout);
        _host.ReplyWith("9c - StatusCheck - Response (Do Not Process).json");

        var status = await _host.RunAsync<IRcmClaimWorkflowService, TaskResultDto>(s => s.StatusCheckAsync(claimId));

        CollectionAssert.AreEqual(Array.Empty<string>(), status.Errors);
        Assert.AreEqual("status-check", XAssert.Single(_host.Transport.Requests).EventCode);
        Assert.AreEqual("completed", status.TaskStatus);
        Assert.AreEqual(RcmClaimStatus.Queued, status.ClaimStatus);
    }

    [TestMethod]
    public async Task Cancel_marks_claim_cancelled_from_17b()
    {
        var seed = await _host.SeedPartiesAsync();
        var claimId = await SeedSubmittedClaimAsync(seed, "req_00112482930", RcmClaimStatus.Approved);
        _host.ReplyWith("17b - Cancel Response (Do not process).json");

        var cancel = await _host.RunAsync<IRcmClaimWorkflowService, TaskResultDto>(s => s.CancelAsync(claimId, new CancelClaimRequestDto { Reason = "WI" }));

        CollectionAssert.AreEqual(Array.Empty<string>(), cancel.Errors);
        Assert.AreEqual("cancel-request", XAssert.Single(_host.Transport.Requests).EventCode);
        Assert.AreEqual(RcmClaimStatus.Cancelled, cancel.ClaimStatus);
        Assert.AreEqual(RcmClaimStatus.Cancelled, (await _host.GetClaimAsync(claimId)).NphiesStatus);

        var again = await XAssert.ThrowsAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmClaimWorkflowService, TaskResultDto>(s => s.CancelAsync(claimId, new CancelClaimRequestDto())));
        Assert.AreEqual(RcmErrorKind.Conflict, again.Kind);
    }

    [TestMethod]
    public async Task Task_response_for_other_claim_is_not_applied()
    {
        var seed = await _host.SeedPartiesAsync();
        var claimId = await SeedSubmittedClaimAsync(seed, "req_some_other", RcmClaimStatus.Approved);
        _host.ReplyWith("17b - Cancel Response (Do not process).json");

        var cancel = await _host.RunAsync<IRcmClaimWorkflowService, TaskResultDto>(s => s.CancelAsync(claimId, new CancelClaimRequestDto { Reason = "WI" }));

        Assert.AreEqual(RcmClaimStatus.Approved, cancel.ClaimStatus);
        Assert.AreEqual(1, await _host.QueryAsync(db => db.NphiesTransactions.CountAsync(t => t.Status == RcmTransactionStatus.Unmatched)));
    }

    [TestMethod]
    public async Task Payment_reconciliation_is_matched_reported_and_acknowledged()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        _host.ReplyWith("7b - Claim Response.json");
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));
        _host.ReplyWith(RcmFixtures.Acknowledgement);

        var json = DocsExamples.ReadJson("13a - Payment Reconciliation.json");
        var inbound = await _host.RunAsync<IRcmInboundService, RcmInboundResultDto>(s => s.IngestRawAsync(json));
        Assert.IsTrue(inbound.Parsed);
        Assert.AreEqual(1, inbound.Processed);
        var reconId = XAssert.Single(inbound.PaymentReconciliationIds);

        var duplicate = await _host.RunAsync<IRcmInboundService, RcmInboundResultDto>(s => s.IngestRawAsync(json));
        Assert.AreEqual(1, duplicate.Duplicates);
        Assert.AreEqual(1, await _host.QueryAsync(db => db.PaymentReconciliations.CountAsync()));

        var recon = await _host.RunAsync<IRcmPaymentService, PaymentReconciliationDto?>(s => s.GetAsync(reconId));
        Assert.AreEqual(193.55m, recon!.PaymentAmount);
        var detail = XAssert.Single(recon.Details);
        Assert.AreEqual(claim.Id, detail.MatchedClaimId);
        Assert.AreEqual(195.5m, detail.ExpectedAmount, "benefit 170 + VAT 25.5 from 7b");
        Assert.AreEqual(195.5m, detail.ComponentPayment);
        Assert.AreEqual(-1.95m, detail.NphiesFee);
        Assert.AreEqual("Matched", detail.MatchStatus);
        Assert.AreEqual("Matched", recon.MatchStatus);
        Assert.AreEqual(0, (await _host.RunAsync<IRcmPaymentService, List<PaymentDetailDto>>(s => s.GetDiscrepanciesAsync(new RcmQuery()))).Count);

        var acked = await _host.RunAsync<IRcmPaymentService, PaymentReconciliationDto>(s => s.AcknowledgeAsync(reconId));
        Assert.AreEqual("Acknowledged", acked.AcknowledgementStatus, acked.AcknowledgementError);
        Assert.AreEqual("payment-notice", _host.Transport.EventCodes.Last());

        var stored = await _host.GetClaimAsync(claim.Id);
        Assert.AreEqual(195.5m, stored.PaidAmount);
        Assert.AreEqual(RcmClaimStatus.Paid, stored.NphiesStatus);
    }

    [TestMethod]
    public async Task Short_payment_is_reported_as_discrepancy()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        _host.ReplyWith("7b - Claim Response.json");
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));

        var json = DocsExamples.ReadJson("13a - Payment Reconciliation.json").Replace("195.5", "150");
        var inbound = await _host.RunAsync<IRcmInboundService, RcmInboundResultDto>(s => s.IngestRawAsync(json));
        var recon = await _host.RunAsync<IRcmPaymentService, PaymentReconciliationDto?>(s => s.GetAsync(XAssert.Single(inbound.PaymentReconciliationIds)));

        var detail = XAssert.Single(recon!.Details);
        Assert.AreEqual("Underpaid", detail.MatchStatus);
        Assert.AreEqual(-45.5m, detail.VarianceAmount);
        Assert.AreEqual("Discrepancy", recon.MatchStatus);
        var discrepancies = await _host.RunAsync<IRcmPaymentService, List<PaymentDetailDto>>(s => s.GetDiscrepanciesAsync(new RcmQuery()));
        Assert.AreEqual(detail.Id, XAssert.Single(discrepancies).Id);
        Assert.AreEqual(RcmClaimStatus.PartiallyPaid, (await _host.GetClaimAsync(claim.Id)).NphiesStatus);
    }

    [TestMethod]
    public async Task Poll_ingests_deferred_claim_response_idempotently()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "iMXbxXMGMR");
        await _host.QueryAsync(async db =>
        {
            var c = await db.Claims.SingleAsync(x => x.Id == claim.Id);
            c.NphiesStatus = RcmClaimStatus.Queued;
            c.SubmittedAt = DateTime.UtcNow.AddHours(-1);
            return await db.SaveChangesAsync();
        });
        _host.ReplyWith("12b - Poll Response (Do Not Process).json");

        var first = await _host.RunAsync<IRcmPollService, PollRunResultDto>(s => s.PollOnceAsync(seed.Provider.Id));
        CollectionAssert.AreEqual(Array.Empty<string>(), first.Errors);
        Assert.AreEqual("poll-request", _host.Transport.EventCodes.First());
        Assert.AreEqual(1, first.MessagesReceived);
        Assert.AreEqual(1, first.Processed);
        CollectionAssert.Contains(first.ClaimIds, claim.Id);
        Assert.AreEqual(RcmClaimStatus.Approved, (await _host.GetClaimAsync(claim.Id)).NphiesStatus);

        var second = await _host.RunAsync<IRcmPollService, PollRunResultDto>(s => s.PollOnceAsync(seed.Provider.Id));
        Assert.AreEqual(0, second.Processed);
        Assert.AreEqual(1, second.Duplicates);
        Assert.AreEqual(1, await _host.QueryAsync(db => db.ClaimResponses.CountAsync(r => r.ClaimId == claim.Id)));
    }

    [TestMethod]
    public async Task Communication_request_is_answered_with_attachment()
    {
        var seed = await _host.SeedPartiesAsync();
        var claimId = await SeedSubmittedClaimAsync(seed, "req_00112489089", RcmClaimStatus.Queued);
        var claim = await _host.GetClaimAsync(claimId);
        _host.ReplyWith(RcmFixtures.Acknowledgement);

        var inbound = await _host.RunAsync<IRcmInboundService, RcmInboundResultDto>(s =>
            s.IngestRawAsync(DocsExamples.ReadJson("18a - Communication Request.json")));
        var crId = XAssert.Single(inbound.CommunicationRequestIds);

        var cr = await _host.RunAsync<IRcmCommunicationService, CommunicationRequestDto?>(s => s.GetRequestAsync(crId));
        Assert.AreEqual(claim.Id, cr!.ClaimId);
        Assert.IsFalse(string.IsNullOrEmpty(cr.PayloadText));
        var open = await _host.RunAsync<IRcmCommunicationService, RcmPagedResult<CommunicationRequestDto>>(s => s.ListRequestsAsync(new RcmQuery(), true));
        Assert.AreEqual(1, open.Total);

        var reply = await _host.RunAsync<IRcmCommunicationService, CommunicationDto>(s => s.RespondAsync(crId, new SendCommunicationDto
        {
            Texts = { "Lab report attached" },
            Attachments = { new AttachmentDto { ContentType = "application/pdf", Title = "lab.pdf", Data = Convert.ToBase64String(new byte[] { 37, 80, 68, 70 }) } },
        }));
        CollectionAssert.AreEqual(Array.Empty<string>(), reply.Errors);
        Assert.AreEqual("communication", _host.Transport.EventCodes.Last());
        Assert.AreEqual(crId, reply.CommunicationRequestId);
        Assert.AreEqual(claim.Id, reply.ClaimId);
        StringAssert.Contains(_host.Transport.Requests.Last().Json, "lab.pdf");

        var answered = await _host.RunAsync<IRcmCommunicationService, CommunicationRequestDto?>(s => s.GetRequestAsync(crId));
        Assert.AreEqual(reply.Id, answered!.RespondedCommunicationId);
        var stillOpen = await _host.RunAsync<IRcmCommunicationService, RcmPagedResult<CommunicationRequestDto>>(s => s.ListRequestsAsync(new RcmQuery(), true));
        Assert.AreEqual(0, stillOpen.Total);
    }

    [TestMethod]
    public async Task Unsolicited_communication_is_sent_for_claim()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.GetClaimAsync(await SeedSubmittedClaimAsync(seed, "req_unsolicited_1", RcmClaimStatus.Queued));
        _host.ReplyWith(RcmFixtures.Acknowledgement);

        var sent = await _host.RunAsync<IRcmCommunicationService, CommunicationDto>(s => s.SendAsync(new SendCommunicationDto
        {
            ClaimId = claim.Id,
            Texts = { "Additional clinical notes" },
            Attachments = { new AttachmentDto { Title = "notes.pdf", Data = Convert.ToBase64String(new byte[] { 1 }) } },
        }));

        CollectionAssert.AreEqual(Array.Empty<string>(), sent.Errors);
        Assert.AreEqual("communication", XAssert.Single(_host.Transport.Requests).EventCode);
        Assert.AreEqual("outbound", sent.Direction.ToLowerInvariant());
        var list = await _host.RunAsync<IRcmCommunicationService, RcmPagedResult<CommunicationDto>>(s => s.ListAsync(new RcmQuery(), claim.Id));
        Assert.AreEqual(1, list.Total);
    }

    [TestMethod]
    public async Task Batch_submission_sends_one_batch_request()
    {
        var seed = await _host.SeedPartiesAsync();
        var a = await _host.SeedClaimAsync(seed, "req_batch_a");
        var b = await _host.SeedClaimAsync(seed, "req_batch_b");
        _host.ReplyWith(RcmFixtures.Acknowledgement);

        var result = await _host.RunAsync<IRcmClaimWorkflowService, BatchSubmitResultDto>(s =>
            s.SubmitBatchAsync(new BatchSubmitRequestDto { ClaimIds = { a.Id, b.Id } }));

        CollectionAssert.AreEqual(Array.Empty<string>(), result.Errors);
        Assert.AreEqual("batch-request", XAssert.Single(_host.Transport.Requests).EventCode);
        Assert.AreEqual(2, result.ClaimCount);
        CollectionAssert.AreEqual(new int?[] { 1, 2 }, result.Claims.Select(c => c.BatchNumber).ToArray());

        var fetched = await _host.RunAsync<IRcmClaimWorkflowService, BatchSubmitResultDto?>(s => s.GetBatchAsync(result.BatchIdentifier));
        Assert.AreEqual(2, fetched!.Claims.Count);
        Assert.AreEqual(result.BatchIdentifier, (await _host.GetClaimAsync(a.Id)).BatchIdentifier);

        await XAssert.ThrowsAsync<RcmWorkflowException>(() => _host.RunAsync<IRcmClaimWorkflowService, BatchSubmitResultDto>(s =>
            s.SubmitBatchAsync(new BatchSubmitRequestDto { ClaimIds = { a.Id } })));
    }

    [TestMethod]
    public async Task Analytics_reflect_adjudication_and_payment()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        _host.ReplyWith("7b - Claim Response.json");
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));
        _host.ReplyWith(RcmFixtures.Acknowledgement);
        await _host.RunAsync<IRcmInboundService, RcmInboundResultDto>(s => s.IngestRawAsync(DocsExamples.ReadJson("13a - Payment Reconciliation.json")));

        var query = new RcmQuery { From = DateTime.UtcNow.AddYears(-5), To = DateTime.UtcNow.AddDays(1) };
        var dashboard = await _host.RunAsync<IRcmAnalyticsService, RcmDashboardDto>(s => s.GetDashboardAsync(query));
        Assert.AreEqual(1, dashboard.ClaimsSubmitted);
        Assert.AreEqual(1, dashboard.ClaimsApproved);
        Assert.AreEqual(230m, dashboard.BilledAmount);
        Assert.AreEqual(195.5m, dashboard.PaidAmount);
        Assert.AreEqual(0, dashboard.PaymentDiscrepancies);

        var aging = await _host.RunAsync<IRcmAnalyticsService, List<AgingBucketDto>>(s => s.GetAgingAsync(query));
        Assert.IsTrue(aging.Count > 0);

        var rates = await _host.RunAsync<IRcmAnalyticsService, List<DenialRateDto>>(s => s.GetDenialRateAsync(query, "month"));
        Assert.AreEqual(1, rates.Sum(r => r.Adjudicated));
        Assert.AreEqual(0, rates.Sum(r => r.Denied));

        var payers = await _host.RunAsync<IRcmAnalyticsService, List<PayerBreakdownDto>>(s => s.GetPayerBreakdownAsync(query));
        var payer = XAssert.Single(payers);
        Assert.AreEqual(seed.Insurer.Id, payer.InsurerId);
        Assert.AreEqual(1, payer.Approved);
    }

    [TestMethod]
    public async Task Review_gate_blocks_unreviewed_claims_when_enabled()
    {
        await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);
        _host = await RcmTestHost.CreateAsync(new Dictionary<string, string?> { ["Rcm:RequireReviewApprovalForClaims"] = "true" });
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_gate_1");
        await _host.QueryAsync(async db =>
        {
            var c = await db.Claims.SingleAsync(x => x.Id == claim.Id);
            c.MedicalReviewStatus = "Pending";
            return await db.SaveChangesAsync();
        });

        var ex = await XAssert.ThrowsAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id)));
        Assert.AreEqual(RcmErrorKind.Conflict, ex.Kind);
        Assert.AreEqual(0, _host.Transport.Requests.Count);
    }
}
