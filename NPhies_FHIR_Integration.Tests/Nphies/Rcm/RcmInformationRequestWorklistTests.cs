using System.Text;
using System.Text.Json.Nodes;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Sandbox;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Documents;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using NPhies_FHIR_Integration.Infrastructure.Data.Seeders;
using ClaimEntity = NPhies_FHIR_Integration.Domain.Entities.Claim;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

/// <summary>
/// The information-request worklist (payer CommunicationRequests with a due date, per hospital) and the replies:
/// about the latest version of a prior authorization chain (Follow-up guide v2.2), about an advanced authorization,
/// and with stored claim documents attached.
/// </summary>
[TestClass]
public class RcmInformationRequestWorklistTests
{
    private RcmTestHost _host = null!;
    private NphiesSandboxTransport _sandbox = null!;

    [TestInitialize]
    public async Task Init()
    {
        _host = await RcmTestHost.CreateAsync();
        _sandbox = _host.Services.GetRequiredService<NphiesSandboxTransport>();
        _host.ReplyWith(r => _sandbox.SendAsync(r).GetAwaiter().GetResult().Body);
        await _host.QueryAsync(async db =>
        {
            await SampleDataSeeder.SeedAsync(db, NullLogger.Instance);
            return true;
        });
    }

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    // ----- helpers -----

    private async Task<ClaimEntity> SubmittedAsync(bool preAuth, int skip = 0)
    {
        var use = preAuth ? "preauthorization" : "claim";
        var claim = await _host.QueryAsync(db => db.Claims.AsNoTracking()
            .Where(c => c.ClaimNumber.StartsWith(SampleDataSeeder.ClaimPrefix) && c.Use == use && c.Priority != "deferred" && c.Items.Count > 1)
            .OrderBy(c => c.ClaimNumber).Skip(skip).FirstAsync());
        _sandbox.DecideAs = "approved";
        var submit = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));
        _sandbox.DecideAs = null;
        Assert.AreEqual("approved", submit.AdjudicationOutcome, string.Join("; ", submit.Errors));
        await PollAsync(); // the payer's own follow-ups for this claim
        return await _host.GetClaimAsync(claim.Id);
    }

    private Task<PollRunResultDto> PollAsync() =>
        _host.RunAsync<IRcmPollService, PollRunResultDto>(s => s.PollAsync(new PollRequestDto()));

    /// <summary>The provider license the sample claims are sent with: the payer's messages are queued for it.</summary>
    private string Sender() => new FhirJsonParser().Parse<Bundle>(_host.Transport.Requests.Last().Json)
        .Entry.Select(e => e.Resource).OfType<MessageHeader>().Single().Sender.Identifier.Value;

    /// <summary>The docs' communication request (18a), about <paramref name="claim"/>, delivered by the payer.</summary>
    private void Deliver(ClaimEntity claim, string identifier, string priority = "routine", string category = "instruction", DateTime? occurrence = null)
    {
        var bundle = JsonNode.Parse(DocsExamples.ReadJson("18a - Communication Request.json"))!;
        bundle["id"] = Guid.NewGuid().ToString();
        var entries = bundle["entry"]!.AsArray();
        entries[0]!["resource"]!["id"] = Guid.NewGuid().ToString();
        var request = entries.Select(e => e!["resource"]!).First(r => (string?)r["resourceType"] == "CommunicationRequest").AsObject();
        request["id"] = Guid.NewGuid().ToString();
        request["identifier"]![0]!["value"] = identifier;
        request["priority"] = priority;
        request["category"] = JsonNode.Parse($$"""[{"coding":[{"system":"http://terminology.hl7.org/CodeSystem/communication-category","code":"{{category}}"}]}]""");
        request["about"] = new JsonArray(new JsonObject
        {
            ["type"] = "Claim",
            ["identifier"] = new JsonObject { ["system"] = claim.ClaimIdentifierSystem, ["value"] = claim.ClaimIdentifierValue },
        });
        request.Remove("occurrenceDateTime");
        if (occurrence.HasValue) request["occurrenceDateTime"] = occurrence.Value.ToString("yyyy-MM-ddTHH:mm:ssZ");
        _sandbox.Deliver(Sender(), bundle.ToJsonString());
    }

    private async Task<CommunicationRequestDto> RequestAsync(string identifier)
    {
        var id = await _host.QueryAsync(db => db.CommunicationRequests.Where(r => r.IdentifierValue == identifier).Select(r => r.Id).SingleAsync());
        return (await _host.RunAsync<IRcmCommunicationService, CommunicationRequestDto?>(s => s.GetRequestAsync(id)))!;
    }

    private Task<InformationRequestSummaryDto> SummaryAsync(RcmQuery? query = null) =>
        _host.RunAsync<IRcmCommunicationService, InformationRequestSummaryDto>(s => s.GetRequestSummaryAsync(query ?? new RcmQuery()));

    private Task<RcmPagedResult<CommunicationRequestDto>> ListAsync(CommunicationRequestQuery query) =>
        _host.RunAsync<IRcmCommunicationService, RcmPagedResult<CommunicationRequestDto>>(s => s.ListRequestsAsync(query));

    private Task SetDueAsync(string id, DateTime due) => _host.QueryAsync(async db =>
    {
        var r = await db.CommunicationRequests.SingleAsync(x => x.Id == id);
        r.DueAt = due;
        return await db.SaveChangesAsync();
    });

    private Communication SentCommunication() =>
        new FhirJsonParser().Parse<Bundle>(_host.Transport.Requests.Last(r => r.EventCode == NphiesMessageEvents.Communication).Json)
            .Entry.Select(e => e.Resource).OfType<Communication>().Single();

    private static void AssertNear(DateTime expected, DateTime? actual) =>
        Assert.IsTrue(actual.HasValue && Math.Abs((actual.Value - expected).TotalMinutes) < 2, $"Expected about {expected:u}, got {actual:u}.");

    // ----- the tests -----

    [TestMethod]
    public async Task Payer_requests_wait_in_the_hospital_worklist_until_their_due_date()
    {
        var claim = await SubmittedAsync(preAuth: false);
        var before = await SummaryAsync();

        var payerDate = DateTime.UtcNow.AddDays(5);
        Deliver(claim, "CR_urgent", priority: "urgent");
        Deliver(claim, "CR_dated", occurrence: payerDate);
        Deliver(claim, "CR_notice", category: "notification");
        await PollAsync();

        var urgent = await RequestAsync("CR_urgent");
        AssertNear(urgent.ReceivedAt!.Value.AddHours(24), urgent.DueAt); // the hospital's target for "urgent"
        Assert.IsTrue(urgent.IsOpen);
        Assert.AreEqual("Claim", urgent.Kind);
        Assert.AreEqual(claim.ClaimNumber, urgent.ClaimNumber);
        Assert.AreEqual(claim.Id, urgent.ReplyClaimId);
        Assert.IsFalse(string.IsNullOrWhiteSpace(urgent.PatientName));
        Assert.IsFalse(string.IsNullOrWhiteSpace(urgent.InsurerName));
        Assert.IsTrue(urgent.HoursLeft is > 23 and <= 24, $"{urgent.HoursLeft}");
        AssertNear(payerDate, (await RequestAsync("CR_dated")).DueAt); // the payer's occurrenceDateTime
        var notice = await RequestAsync("CR_notice");
        Assert.IsNull(notice.DueAt, "A notification needs no reply.");
        Assert.IsFalse(notice.IsOpen);

        var summary = await SummaryAsync();
        Assert.AreEqual(before.Open + 2, summary.Open);
        Assert.AreEqual(before.Notifications + 1, summary.Notifications);
        Assert.AreEqual(before.Overdue, summary.Overdue);

        // One is overdue, one due soon.
        await SetDueAsync(urgent.Id, DateTime.UtcNow.AddHours(-2));
        var dated = await RequestAsync("CR_dated");
        await SetDueAsync(dated.Id, DateTime.UtcNow.AddHours(2));
        summary = await SummaryAsync();
        Assert.AreEqual(before.Overdue + 1, summary.Overdue);
        Assert.AreEqual(before.DueSoon + 1, summary.DueSoon);
        var overdue = await ListAsync(new CommunicationRequestQuery { OverdueOnly = true });
        var late = overdue.Items.Single(r => r.Id == urgent.Id);
        Assert.IsTrue(late.IsOverdue);
        Assert.IsTrue(late.HoursLeft < 0);
        Assert.IsFalse(overdue.Items.Any(r => r.Id == dated.Id));
        Assert.IsTrue((await RequestAsync("CR_dated")).IsDueSoon);

        // The most urgent first.
        var open = (await ListAsync(new CommunicationRequestQuery { OpenOnly = true, ClaimId = claim.Id })).Items;
        CollectionAssert.AreEqual(new[] { urgent.Id, dated.Id }, open.Take(2).Select(r => r.Id).ToArray());
        Assert.AreEqual(0, (await ListAsync(new CommunicationRequestQuery { OpenOnly = true, ClaimId = claim.Id, Kind = "PriorAuthorization" })).Total);
        Assert.AreEqual(open.Count, (await ListAsync(new CommunicationRequestQuery { OpenOnly = true, ClaimId = claim.Id, Kind = "Claim" })).Total);
        var otherHospital = await _host.QueryAsync(db => db.Organizations.Where(o => o.Id != claim.ProviderId).Select(o => o.Id).FirstAsync());
        Assert.AreEqual(0, (await ListAsync(new CommunicationRequestQuery { OpenOnly = true, ClaimId = claim.Id, ProviderId = otherHospital })).Total,
            "Another hospital does not see the request.");

        // Answered late.
        await _host.RunAsync<IRcmCommunicationService, CommunicationDto>(s => s.RespondAsync(urgent.Id, new SendCommunicationDto { Texts = { "The report is attached." } }));
        summary = await SummaryAsync();
        Assert.AreEqual(before.Open + 1, summary.Open);
        Assert.AreEqual(before.Answered + 1, summary.Answered);
        Assert.AreEqual(before.AnsweredLate + 1, summary.AnsweredLate);
        Assert.IsNotNull(summary.AverageResponseHours);
        Assert.IsTrue((await RequestAsync("CR_urgent")).AnsweredLate);
    }

    [TestMethod]
    public async Task Communications_about_a_prior_authorization_go_to_the_latest_version_of_its_chain()
    {
        var first = await SubmittedAsync(preAuth: true);
        _sandbox.DecideAs = "approved";
        var update = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.UpdatePreAuthAsync(first.Id, new DerivedClaimRequestDto { Submit = true }));
        _sandbox.DecideAs = null;
        var latest = await _host.GetClaimAsync(update.ClaimId);
        Assert.AreEqual(RcmClaimStatus.Superseded, (await _host.GetClaimAsync(first.Id)).NphiesStatus);
        Assert.AreNotEqual(first.ClaimIdentifierValue, latest.ClaimIdentifierValue);

        // Unsolicited, picked from the first version.
        var note = await _host.RunAsync<IRcmCommunicationService, CommunicationDto>(s => s.SendAsync(new SendCommunicationDto
        {
            ClaimId = first.Id, Texts = { "The patient's latest report is on file." },
        }));
        Assert.AreEqual(latest.Id, note.ClaimId);
        Assert.AreEqual(latest.ClaimIdentifierValue, SentCommunication().About.Single().Identifier.Value);

        // The payer still names the first version; the reply goes to the latest.
        Deliver(first, "CR_chain");
        await PollAsync();
        var request = await RequestAsync("CR_chain");
        Assert.AreEqual(first.Id, request.ClaimId);
        Assert.AreEqual("PriorAuthorization", request.Kind);
        Assert.AreEqual(latest.Id, request.ReplyClaimId);
        Assert.AreEqual(latest.ClaimNumber, request.ReplyClaimNumber);

        var reply = await _host.RunAsync<IRcmCommunicationService, CommunicationDto>(s => s.RespondAsync(request.Id, new SendCommunicationDto { Texts = { "Here it is." } }));
        CollectionAssert.AreEqual(Array.Empty<string>(), reply.Errors);
        var sent = SentCommunication();
        Assert.AreEqual(latest.ClaimIdentifierValue, sent.About.Single().Identifier.Value);
        Assert.AreEqual("CR_chain", sent.BasedOn.Single().Identifier.Value);
    }

    [TestMethod]
    public async Task Stored_claim_documents_are_sent_as_attachments_of_the_claims_patient_only()
    {
        var claim = await SubmittedAsync(preAuth: false);
        var sequence = await _host.QueryAsync(db => db.ClaimItems.Where(i => i.ClaimId == claim.Id).MinAsync(i => i.Sequence));
        var pdf = Encoding.ASCII.GetBytes("%PDF-1.4\n% lipid profile\n%%EOF");
        Task<ClaimDocumentDto> Upload(string patientId, string title) => _host.RunAsync<IRcmDocumentService, ClaimDocumentDto>(s => s.UploadAsync(new DocumentMetadataDto
        {
            ProviderId = claim.ProviderId, PatientId = patientId, DocumentType = "lab-report", Title = title, ServiceDate = DateTime.UtcNow.Date,
        }, "lipid.pdf", "application/pdf", pdf, "tester", null));
        var lab = await Upload(claim.PatientId, "Lipid profile");

        var sent = await _host.RunAsync<IRcmCommunicationService, CommunicationDto>(s => s.SendAsync(new SendCommunicationDto
        {
            ClaimId = claim.Id, Texts = { "Lab report for the first service." },
            Documents = { new CommunicationDocumentDto { DocumentId = lab.Id, ItemSequence = sequence } },
        }));

        CollectionAssert.AreEqual(Array.Empty<string>(), sent.Errors);
        CollectionAssert.AreEqual(new[] { sequence }, sent.ItemSequences);
        var attachment = SentCommunication().Payload.Single(p => p.Content is Attachment);
        var content = (Attachment)attachment.Content;
        Assert.AreEqual("Lipid profile", content.Title);
        Assert.AreEqual("application/pdf", content.ContentType);
        CollectionAssert.AreEqual(pdf, content.Data);
        Assert.AreEqual(sequence, (attachment.GetExtension(NphiesExtensions.ClaimItemSequence)?.Value as PositiveInt)?.Value);

        var otherPatient = await _host.QueryAsync(db => db.Patients.Where(p => p.Id != claim.PatientId).Select(p => p.Id).FirstAsync());
        var foreign = await Upload(otherPatient, "Someone else's report");
        var refused = await XAssert.ThrowsAsync<RcmWorkflowException>(() => _host.RunAsync<IRcmCommunicationService, CommunicationDto>(s => s.SendAsync(new SendCommunicationDto
        {
            ClaimId = claim.Id, Documents = { new CommunicationDocumentDto { DocumentId = foreign.Id } },
        })));
        Assert.AreEqual(RcmErrorKind.BadRequest, refused.Kind);
        StringAssert.Contains(string.Join(" ", refused.Errors), "another patient");
        Assert.AreEqual(RcmErrorKind.NotFound, (await XAssert.ThrowsAsync<RcmWorkflowException>(() => _host.RunAsync<IRcmCommunicationService, CommunicationDto>(s => s.SendAsync(new SendCommunicationDto
        {
            ClaimId = claim.Id, Documents = { new CommunicationDocumentDto { DocumentId = "no-such-document" } },
        })))).Kind);
    }
}
