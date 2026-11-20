using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Sandbox;
using NPhies_FHIR_Integration.Application.Nphies.Validation;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using NPhies_FHIR_Integration.Infrastructure.Data.Seeders;
using ClaimEntity = NPhies_FHIR_Integration.Domain.Entities.Claim;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

/// <summary>
/// Appeals inside the same claim (Claim Re-submission guide): a re-adjudication communication names the denied
/// services; the payer reissues its ClaimResponse (<c>reissue-reason = adjudication</c>) or explains in a notification
/// why it keeps the denial. Run against the sandbox with the sample data's IG-conformant claims.
/// </summary>
[TestClass]
public class RcmReAdjudicationAppealTests
{
    private RcmTestHost _host = null!;
    private NphiesSandboxTransport _sandbox = null!;
    private string? _lastClaimReply;

    [TestInitialize]
    public async Task Init()
    {
        _host = await RcmTestHost.CreateAsync();
        _sandbox = _host.Services.GetRequiredService<NphiesSandboxTransport>();
        _host.ReplyWith(r =>
        {
            var body = _sandbox.SendAsync(r).GetAwaiter().GetResult().Body;
            if (r.EventCode == NphiesMessageEvents.ClaimRequest) _lastClaimReply = body;
            return body;
        });
        await _host.QueryAsync(async db =>
        {
            await SampleDataSeeder.SeedAsync(db, NullLogger.Instance);
            return true;
        });
    }

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    // ----- helpers -----

    /// <summary>A sample professional claim the payer approves except for its last service.</summary>
    private async Task<(ClaimEntity Claim, DenialDto Denial)> PartlyApprovedClaimAsync(int skip = 0)
    {
        var claim = await _host.QueryAsync(db => db.Claims.AsNoTracking()
            .Where(c => c.ClaimNumber.StartsWith(SampleDataSeeder.ClaimPrefix) && c.Use == "claim" && c.ClaimType == "professional" && c.Priority != "deferred" && c.Items.Count > 1)
            .OrderBy(c => c.ClaimNumber).Skip(skip).FirstAsync());
        _sandbox.DecideAs = "partial";
        var submit = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));
        _sandbox.DecideAs = null;
        Assert.AreEqual("partial", submit.AdjudicationOutcome, string.Join("; ", submit.Errors));

        var denials = await DenialsAsync(claim.Id);
        var denial = denials.Single();
        Assert.AreEqual(RcmDenialStatus.Open, denial.Status);
        Assert.IsNotNull(denial.ItemSequence, "The sandbox denies the last service.");
        return (claim, denial);
    }

    private async Task<List<DenialDto>> DenialsAsync(string claimId) =>
        (await _host.RunAsync<IRcmDenialService, RcmPagedResult<DenialDto>>(s => s.ListAsync(new DenialQuery { ClaimId = claimId }))).Items;

    private Task<AppealDto> AppealAsync(CreateAppealDto request) =>
        _host.RunAsync<IRcmAppealService, AppealDto>(s => s.CreateAsync(request));

    private async Task<AppealDto> GetAppealAsync(string id) =>
        (await _host.RunAsync<IRcmAppealService, AppealDto?>(s => s.GetAsync(id)))!;

    private Task<PollRunResultDto> PollAsync() =>
        _host.RunAsync<IRcmPollService, PollRunResultDto>(s => s.PollAsync(new PollRequestDto()));

    private Task<CommunicationDto> SendAsync(SendCommunicationDto request) =>
        _host.RunAsync<IRcmCommunicationService, CommunicationDto>(s => s.SendAsync(request));

    private async Task<string?> DecisionAsync(string claimId) => (await _host.GetClaimAsync(claimId)).AdjudicationOutcome;

    private Communication SentCommunication()
    {
        var request = _host.Transport.Requests.Last(r => r.EventCode == NphiesMessageEvents.Communication);
        var bundle = new FhirJsonParser().Parse<Bundle>(request.Json);
        var errors = new NphiesProfileValidator(NphiesConformancePackage.Embedded).Validate(bundle).Errors.ToList();
        Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors.Select(e => $"{e.RuleId} {e.Expression}: {e.Message}")));
        return bundle.Entry.Select(e => e.Resource).OfType<Communication>().Single();
    }

    private static int? Sequence(Communication.PayloadComponent payload) =>
        (payload.GetExtension(NphiesExtensions.ClaimItemSequence)?.Value as PositiveInt)?.Value;

    private static AttachmentDto Pdf(string title) => new() { ContentType = "application/pdf", Title = title, Data = Convert.ToBase64String(new byte[] { 37, 80, 68, 70 }) };

    // ----- the tests -----

    [TestMethod]
    public async Task A_service_appealed_in_the_same_claim_is_approved_by_a_reissued_response()
    {
        var (claim, denial) = await PartlyApprovedClaimAsync();
        var sequence = denial.ItemSequence!.Value;

        var appeal = await AppealAsync(new CreateAppealDto
        {
            ClaimId = claim.Id,
            DenialIds = { denial.Id },
            Resubmit = false,
            Reason = "The service was medically necessary.",
            Items = { new CommunicationItemNoteDto { Sequence = sequence, Text = "Clinical notes show the service was medically necessary." } },
            Attachments = { Pdf("physician-notes.pdf") },
        });

        CollectionAssert.AreEqual(Array.Empty<string>(), appeal.Errors);
        Assert.AreEqual(RcmAppealMethod.Communication, appeal.Method);
        Assert.AreEqual("Submitted", appeal.Status);
        Assert.IsNotNull(appeal.CommunicationId);
        Assert.IsNull(appeal.ResubmissionClaimId);
        Assert.AreEqual(1, _host.Transport.EventCodes.Count(e => e == NphiesMessageEvents.ClaimRequest), "No new claim is sent.");
        Assert.AreEqual(RcmDenialStatus.Appealed, (await DenialsAsync(claim.Id)).Single().Status);

        var communication = SentCommunication();
        Assert.AreEqual(NphiesCommunicationReasons.ReAdjudication, communication.ReasonCode.Single().Coding.Single().Code);
        Assert.AreEqual(NphiesCodeSystems.CommunicationReason, communication.ReasonCode.Single().Coding.Single().System);
        Assert.AreEqual(claim.ClaimIdentifierValue, communication.About.Single().Identifier.Value, "About the same claim.");
        Assert.AreEqual(0, communication.BasedOn.Count);
        var text = communication.Payload.Single(p => p.Content is FhirString);
        Assert.AreEqual(sequence, Sequence(text));
        Assert.AreEqual("Clinical notes show the service was medically necessary.", ((FhirString)text.Content).Value);
        Assert.AreEqual(sequence, Sequence(communication.Payload.Single(p => p.Content is Attachment)), "A document supports the only appealed service.");

        var poll = await PollAsync();

        CollectionAssert.Contains(poll.ClaimIds, claim.Id);
        Assert.AreEqual("approved", await DecisionAsync(claim.Id));
        var adjudication = await _host.RunAsync<IRcmClaimWorkflowService, ClaimAdjudicationDto?>(s => s.GetLatestAdjudicationAsync(claim.Id));
        Assert.AreEqual("adjudication", adjudication!.ReissueReason);
        Assert.AreEqual("approved", adjudication.Items.Single(i => i.Sequence == sequence).Outcome);

        var after = (await DenialsAsync(claim.Id)).Single();
        Assert.AreEqual(denial.Id, after.Id, "The appealed denial is decided; no new one is opened.");
        Assert.AreEqual(RcmDenialStatus.Overturned, after.Status);
        var decided = await GetAppealAsync(appeal.Id);
        Assert.AreEqual("Decided", decided.Status);
        Assert.AreEqual("Overturned", decided.Outcome);
        Assert.AreEqual(denial.DeniedAmount, decided.RecoveredAmount);
        Assert.AreEqual(denial.DeniedAmount, decided.AppealedAmount);
    }

    [TestMethod]
    public async Task An_upheld_re_adjudication_keeps_the_denial_and_it_can_be_appealed_again()
    {
        var (claim, denial) = await PartlyApprovedClaimAsync();
        var sequence = denial.ItemSequence!.Value;
        _sandbox.ReadjudicateAs = "uphold";

        var first = await AppealAsync(new CreateAppealDto
        {
            ClaimId = claim.Id, Resubmit = false, Reason = "Medically necessary.", Texts = { "Please review the service again." },
        });
        Assert.AreEqual("Please review the service again.", ((FhirString)SentCommunication().Payload.Single().Content).Value,
            "The general justification goes with the service; it is not sent twice.");
        var poll = await PollAsync();

        // The payer's notification: informational, about the appealed service, needing no reply.
        var notices = await _host.QueryAsync(db => db.CommunicationRequests.AsNoTracking().Where(r => r.ClaimId == claim.Id && r.Reason == "re-adjudication").ToListAsync());
        var notice = (await _host.RunAsync<IRcmCommunicationService, CommunicationRequestDto?>(s => s.GetRequestAsync(notices.Single().Id)))!;
        CollectionAssert.Contains(poll.CommunicationRequestIds, notice.Id);
        Assert.AreEqual("notification", notice.Category);
        CollectionAssert.AreEqual(new[] { sequence }, notice.ItemSequences);
        Assert.IsFalse(notice.RequiresResponse);
        Assert.IsFalse(notice.IsOpen);
        var open = await _host.RunAsync<IRcmCommunicationService, RcmPagedResult<CommunicationRequestDto>>(s => s.ListRequestsAsync(new RcmQuery(), openOnly: true));
        Assert.IsFalse(open.Items.Any(r => r.Id == notice.Id), "Notifications are not in the open requests.");

        var upheld = (await DenialsAsync(claim.Id)).Single();
        Assert.AreEqual(RcmDenialStatus.Upheld, upheld.Status);
        StringAssert.Contains(upheld.Notes, "does not justify");
        Assert.AreEqual("partial", await DecisionAsync(claim.Id), "The decision is unchanged.");
        var firstDecided = await GetAppealAsync(first.Id);
        Assert.AreEqual("Upheld", firstDecided.Outcome);
        StringAssert.Contains(firstDecided.DecisionExplanation, "does not justify");

        // A second appeal of the same service, now with a document.
        _sandbox.ReadjudicateAs = null;
        var second = await AppealAsync(new CreateAppealDto
        {
            ClaimId = claim.Id, DenialIds = { denial.Id }, Resubmit = false, Reason = "Second opinion.", Attachments = { Pdf("second-opinion.pdf") },
        });
        Assert.AreEqual(2, second.Level);
        Assert.IsTrue(second.AppealNumber.EndsWith("-2"), second.AppealNumber);
        await PollAsync();

        Assert.AreEqual(RcmDenialStatus.Overturned, (await DenialsAsync(claim.Id)).Single().Status);
        Assert.AreEqual("Overturned", (await GetAppealAsync(second.Id)).Outcome);
        Assert.AreEqual("Upheld", (await GetAppealAsync(first.Id)).Outcome, "The first appeal keeps its outcome.");
        Assert.AreEqual("approved", await DecisionAsync(claim.Id));
    }

    [TestMethod]
    public async Task A_reissue_that_denies_less_splits_the_denial_into_the_recovered_and_the_upheld_part()
    {
        var (claim, denial) = await PartlyApprovedClaimAsync();
        var sequence = denial.ItemSequence!.Value;
        var firstReply = _lastClaimReply!;
        _sandbox.ReadjudicateAs = "none";
        var appeal = await AppealAsync(new CreateAppealDto { ClaimId = claim.Id, Resubmit = false, Reason = "Medically necessary." });
        Assert.AreEqual("Submitted", appeal.Status);
        await PollAsync();
        Assert.AreEqual("Submitted", (await GetAppealAsync(appeal.Id)).Status, "The payer is still reviewing.");

        // The payer now allows 60% of the service.
        var reissue = new FhirJsonParser().Parse<Bundle>(firstReply);
        reissue.Id = Guid.NewGuid().ToString();
        reissue.Entry.Select(e => e.Resource).OfType<MessageHeader>().Single().Id = Guid.NewGuid().ToString();
        var response = reissue.Entry.Select(e => e.Resource).OfType<ClaimResponse>().Single();
        response.Identifier.Single().Value += "_reissue";
        response.Extension.Add(new Extension(NphiesExtensions.AdjudicationReissue, new CodeableConcept(NphiesCodeSystems.ReissueReason, "adjudication")));
        var item = response.Item.Single(i => i.ItemSequence == sequence);
        item.GetExtension(NphiesExtensions.AdjudicationOutcome)!.Value = new CodeableConcept(NphiesCodeSystems.AdjudicationOutcome, "approved");
        var allowed = Math.Round(denial.DeniedAmount * 0.6m, 2);
        item.Adjudication.Single(a => a.Category.Coding.Any(c => c.Code == "eligible")).Amount!.Value = allowed;
        var sender = new FhirJsonParser().Parse<Bundle>(_host.Transport.Requests.Last(r => r.EventCode == NphiesMessageEvents.ClaimRequest).Json)
            .Entry.Select(e => e.Resource).OfType<MessageHeader>().Single().Sender.Identifier.Value;
        _sandbox.Deliver(sender, new FhirJsonSerializer().SerializeToString(reissue));

        var poll = await PollAsync();
        CollectionAssert.Contains(poll.ClaimIds, claim.Id, $"{poll.MessagesReceived} received; {string.Join("; ", poll.Errors)}");

        var rows = await DenialsAsync(claim.Id);
        Assert.AreEqual(2, rows.Count, string.Join("; ", rows.Select(r => $"{r.Status} {r.DeniedAmount}")));
        var recovered = rows.Single(r => r.Status == RcmDenialStatus.Overturned);
        var kept = rows.Single(r => r.Status == RcmDenialStatus.Upheld);
        Assert.AreEqual(denial.Id, recovered.Id);
        Assert.AreEqual(allowed, recovered.DeniedAmount);
        Assert.AreEqual(denial.DeniedAmount - allowed, kept.DeniedAmount);
        Assert.AreEqual(sequence, kept.ItemSequence);
        Assert.AreEqual(appeal.Id, kept.AppealRequestId);
        var decided = await GetAppealAsync(appeal.Id);
        Assert.AreEqual("PartiallyOverturned", decided.Outcome);
        Assert.AreEqual(allowed, decided.RecoveredAmount);
        Assert.AreEqual(denial.DeniedAmount, decided.AppealedAmount);

        // What is left can be written off, not reopened by hand.
        await XAssert.ThrowsAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmDenialService, DenialDto>(s => s.UpdateAsync(kept.Id, new UpdateDenialDto { Status = RcmDenialStatus.Open })));
        var writtenOff = await _host.RunAsync<IRcmDenialService, DenialDto>(s => s.UpdateAsync(kept.Id, new UpdateDenialDto { Status = RcmDenialStatus.WrittenOff }));
        Assert.AreEqual(RcmDenialStatus.WrittenOff, writtenOff.Status);
    }

    [TestMethod]
    public async Task Re_adjudication_is_refused_where_nphies_would_reject_it()
    {
        var (claim, denial) = await PartlyApprovedClaimAsync();
        var sequence = denial.ItemSequence!.Value;
        SendCommunicationDto ReAdjudicate(params int[] sequences) => new()
        {
            ClaimId = claim.Id, ReasonCode = "re-adjudication",
            Items = sequences.Select(s => new CommunicationItemNoteDto { Sequence = s, Text = "Please look again." }).ToList(),
        };

        var unknown = await XAssert.ThrowsAsync<RcmWorkflowException>(() => SendAsync(ReAdjudicate(99)));
        Assert.AreEqual(RcmErrorKind.BadRequest, unknown.Kind);
        StringAssert.Contains(unknown.Message, "99");
        Assert.AreEqual(RcmErrorKind.BadRequest, (await XAssert.ThrowsAsync<RcmWorkflowException>(() =>
            SendAsync(new SendCommunicationDto { ClaimId = claim.Id, ReasonCode = "re-adjudication", Texts = { "No service named." } }))).Kind);
        Assert.AreEqual(RcmErrorKind.BadRequest, (await XAssert.ThrowsAsync<RcmWorkflowException>(() =>
            SendAsync(new SendCommunicationDto { ClaimId = claim.Id, ReasonCode = "appeal", Texts = { "Not a communication-reason code." } }))).Kind);

        // An item reference without a reason is a plain communication about that service (Claims test case 2).
        var note = await SendAsync(new SendCommunicationDto { ClaimId = claim.Id, ReasonCode = "Missing-info", Items = { new() { Sequence = sequence, Text = "Lab report attached separately." } } });
        Assert.AreEqual("missing-info", note.ReasonCode, "Sent with the code system's spelling.");
        CollectionAssert.AreEqual(new[] { sequence }, note.ItemSequences);
        Assert.AreEqual(sequence, Sequence(SentCommunication().Payload.Single()));

        // NPHIES rejects a re-adjudication about a claim it does not hold as posted and active (BV-00148).
        await _host.QueryAsync(async db =>
        {
            var c = await db.Claims.SingleAsync(x => x.Id == claim.Id);
            c.ClaimIdentifierValue += "_unknown";
            return await db.SaveChangesAsync();
        });
        var failed = await AppealAsync(new CreateAppealDto { ClaimId = claim.Id, Resubmit = false, Reason = "Medically necessary." });
        Assert.AreEqual("Failed", failed.Status);
        Assert.IsTrue(failed.Errors.Any(e => e.Contains("BV-00148") || e.Contains("about element")), string.Join("; ", failed.Errors));
        var still = (await DenialsAsync(claim.Id)).Single();
        Assert.AreEqual(RcmDenialStatus.Open, still.Status, "Nothing reached the payer.");
        Assert.IsNull(still.AppealRequestId);

        // A cancelled claim cannot be re-adjudicated.
        await _host.QueryAsync(async db =>
        {
            var c = await db.Claims.SingleAsync(x => x.Id == claim.Id);
            c.NphiesStatus = RcmClaimStatus.Cancelled;
            return await db.SaveChangesAsync();
        });
        Assert.AreEqual(RcmErrorKind.Conflict, (await XAssert.ThrowsAsync<RcmWorkflowException>(() => SendAsync(ReAdjudicate(sequence)))).Kind);

        // A prior authorization is changed by an update, not re-adjudicated.
        var preAuth = await _host.QueryAsync(db => db.Claims.AsNoTracking()
            .Where(c => c.ClaimNumber.StartsWith(SampleDataSeeder.ClaimPrefix) && c.Use == "preauthorization" && c.Priority != "deferred")
            .OrderBy(c => c.ClaimNumber).FirstAsync());
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(preAuth.Id));
        var first = await _host.QueryAsync(db => db.ClaimItems.Where(i => i.ClaimId == preAuth.Id).MinAsync(i => i.Sequence));
        var pa = await XAssert.ThrowsAsync<RcmWorkflowException>(() => SendAsync(new SendCommunicationDto
        {
            ClaimId = preAuth.Id, ReasonCode = "re-adjudication", Items = { new() { Sequence = first, Text = "Please look again." } },
        }));
        Assert.AreEqual(RcmErrorKind.BadRequest, pa.Kind);
    }
}
