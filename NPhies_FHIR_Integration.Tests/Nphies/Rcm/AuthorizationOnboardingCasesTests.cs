using System.Text.Json.Nodes;
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
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;
using NPhies_FHIR_Integration.Infrastructure.Data.Seeders;
using ClaimEntity = NPhies_FHIR_Integration.Domain.Entities.Claim;
using ClaimItemEntity = NPhies_FHIR_Integration.Domain.Entities.ClaimItem;
using ClaimDiagnosisEntity = NPhies_FHIR_Integration.Domain.Entities.ClaimDiagnosis;
using ClaimSupportingInfoEntity = NPhies_FHIR_Integration.Domain.Entities.ClaimSupportingInfo;
using PatientEntity = NPhies_FHIR_Integration.Domain.Entities.Patient;
using VisionPrescriptionEntity = NPhies_FHIR_Integration.Domain.Entities.VisionPrescription;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

/// <summary>
/// The ten NPHIES onboarding test cases for prior authorization (Authorization test cases V6.0, against the
/// pseudo-payer INS-FHIR), run end to end against the sandbox simulator with the sample data's IG-conformant claims.
/// </summary>
[TestClass]
public class AuthorizationOnboardingCasesTests
{
    private const string InfoCategories = "http://nphies.sa/terminology/CodeSystem/claim-information-category";
    private RcmTestHost _host = null!;
    private NphiesSandboxTransport _sandbox = null!;

    [TestInitialize]
    public async Task Init()
    {
        _host = await RcmTestHost.CreateAsync();
        _sandbox = _host.Services.GetRequiredService<NphiesSandboxTransport>();
        _host.ReplyWith(r => _sandbox.SendAsync(r).GetAwaiter().GetResult().Body);
    }

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    // ----- helpers -----

    private Task SeedSampleDataAsync() => _host.QueryAsync(async db =>
    {
        await SampleDataSeeder.SeedAsync(db, NullLogger.Instance);
        return true;
    });

    /// <summary>The first sample claim of a type; a claim is turned into a prior authorization when the case needs one.</summary>
    private Task<ClaimEntity> PickAsync(string type, bool preAuth, string? withItemCode = null)
    {
        var use = preAuth ? "preauthorization" : "claim";
        return _host.QueryAsync(db => db.Claims.AsNoTracking()
            .Where(c => c.ClaimNumber.StartsWith(SampleDataSeeder.ClaimPrefix) && c.ClaimType == type && c.Use == use
                        && (withItemCode == null || c.Items.Any(i => i.ProductOrServiceCode == withItemCode)))
            .OrderBy(c => c.ClaimNumber).FirstAsync());
    }

    private Task ChangeAsync(string claimId, Action<ApplicationDbContext, ClaimEntity> change) => _host.QueryAsync(async db =>
    {
        var claim = await db.Claims.Include(c => c.Items).Include(c => c.SupportingInfo).Include(c => c.Diagnoses).SingleAsync(c => c.Id == claimId);
        change(db, claim);
        return await db.SaveChangesAsync();
    });

    private static void AsPriorAuthorization(ClaimEntity claim)
    {
        claim.Use = "preauthorization";
        claim.ClaimIdentifierSystem = null;
        claim.ClaimIdentifierValue = null;
    }

    private Task<SubmissionResultDto> SubmitAsync(string claimId) =>
        _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claimId));

    private Task<PollRunResultDto> PollAsync(PollRequestDto? request = null) =>
        _host.RunAsync<IRcmPollService, PollRunResultDto>(s => s.PollAsync(request ?? new PollRequestDto()));

    private async Task<string?> StatusAsync(string claimId) => (await _host.GetClaimAsync(claimId)).NphiesStatus;

    /// <summary>The last request of <paramref name="eventCode"/> sent, checked against the IG profiles.</summary>
    private Bundle Sent(string eventCode = "priorauth-request")
    {
        var request = _host.Transport.Requests.Last(r => r.EventCode == eventCode);
        var bundle = new FhirJsonParser().Parse<Bundle>(request.Json);
        var errors = new NphiesProfileValidator(NphiesConformancePackage.Embedded).Validate(bundle).Errors.ToList();
        Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors.Select(e => $"{e.RuleId} {e.Expression}: {e.Message}")));
        return bundle;
    }

    private static Claim ClaimOf(Bundle bundle) => bundle.Entry.Select(e => e.Resource).OfType<Claim>().Single();

    private static List<string> Categories(Claim claim) => claim.SupportingInfo.Select(s => s.Category.Coding.First().Code).ToList();

    private static void AssertAdjudicated(string? status) =>
        Assert.IsTrue(RcmClaimStatus.Adjudicated.Contains(status), $"Expected a final decision, got {status}.");

    // ----- the cases -----

    [TestMethod]
    public async Task Case_1_institutional_pended_with_an_unsolicited_communication_and_case_7_its_follow_up()
    {
        await SeedSampleDataAsync();
        var pa = await PickAsync("institutional", preAuth: true);
        await ChangeAsync(pa.Id, (_, c) => c.Priority = "deferred");

        var submit = await SubmitAsync(pa.Id);

        Assert.AreEqual(RcmClaimStatus.Queued, submit.ClaimStatus, string.Join("; ", submit.Errors));
        var first = Sent();
        var claim = ClaimOf(first);
        foreach (var category in new[] { "vital-sign-diastolic", "vital-sign-height", "vital-sign-systolic", "vital-sign-weight", "chief-complaint" })
            CollectionAssert.Contains(Categories(claim), category);
        var chief = claim.SupportingInfo.Single(s => s.Category.Coding.First().Code == "chief-complaint");
        Assert.IsNotNull(chief.Code?.Coding.FirstOrDefault()?.Code, "The chief complaint carries an ICD code.");
        Assert.IsFalse(string.IsNullOrWhiteSpace(chief.Code?.Text ?? (chief.Value as FhirString)?.Value), "The chief complaint carries text.");
        Assert.IsNotNull(first.Entry.Select(e => e.Resource).OfType<Encounter>().Single().Period?.Start, "The encounter has a start.");
        Assert.IsTrue(claim.Item.Any(i => (i.GetExtension(NphiesExtensions.Package)?.Value as FhirBoolean)?.Value == true), "At least one package item.");

        // The provider writes to the payer before it decides: free text about the request and about its first service
        // (extension-ClaimItemSequence, ClaimItem guide V1.1).
        var firstService = claim.Item.Min(i => i.Sequence)!.Value;
        var communication = await _host.RunAsync<IRcmCommunicationService, CommunicationDto>(s => s.SendAsync(new SendCommunicationDto
        {
            ClaimId = pa.Id,
            Texts = { "Coronary angiography confirms triple vessel disease; surgery is scheduled." },
            Items = { new CommunicationItemNoteDto { Sequence = firstService, Text = "The operating room is booked for this service." } },
        }));
        CollectionAssert.AreEqual(Array.Empty<string>(), communication.Errors);
        CollectionAssert.AreEqual(new[] { firstService }, communication.ItemSequences);
        var payloads = Sent("communication").Entry.Select(e => e.Resource).OfType<Communication>().Single().Payload;
        Assert.IsNull(payloads[0].GetExtension(NphiesExtensions.ClaimItemSequence), "The first text is about the whole request.");
        Assert.AreEqual(firstService, (payloads[1].GetExtension(NphiesExtensions.ClaimItemSequence)?.Value as PositiveInt)?.Value);

        var poll = await PollAsync();
        CollectionAssert.Contains(poll.ClaimIds, pa.Id);
        AssertAdjudicated(await StatusAsync(pa.Id));

        // Case 7: follow-up with the earlier services, the same dates, and one more service.
        var followUp = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s =>
            s.UpdatePreAuthAsync(pa.Id, new DerivedClaimRequestDto { Submit = false }));
        await ChangeAsync(followUp.ClaimId, (db, c) =>
        {
            c.Priority = "normal";
            // One more day of accommodation (an item without package details).
            var source = c.Items.Single(i => i.ProductOrServiceCode == "83600-00-00");
            var extra = (ClaimItemEntity)db.Entry(source).CurrentValues.ToObject();
            extra.Id = Guid.NewGuid().ToString();
            extra.Sequence = c.Items.Max(i => i.Sequence) + 1;
            extra.Quantity = 1;
            extra.Net = extra.UnitPrice;
            extra.ServicedDate = (source.ServicedDate ?? DateTime.UtcNow.Date).AddDays(1);
            db.Add(extra);
            c.Total += extra.Net ?? 0m;
        });
        await SubmitAsync(followUp.ClaimId);

        var second = ClaimOf(Sent());
        Assert.AreEqual(claim.Identifier.Single().Value, second.Related.Single().Claim.Identifier.Value, "Related to the first request.");
        Assert.AreEqual("prior", second.Related.Single().Relationship.Coding.Single().Code);
        Assert.AreEqual(claim.Item.Count + 1, second.Item.Count, "All earlier services plus one.");
        foreach (var earlier in claim.Item)
            Assert.AreEqual(earlier.Serviced?.ToString(), second.Item.Single(i => i.Sequence == earlier.Sequence).Serviced?.ToString(),
                $"Service {earlier.Sequence} keeps its date.");
        AssertAdjudicated(await StatusAsync(followUp.ClaimId));
    }

    [TestMethod]
    public async Task Case_2_professional_with_an_online_eligibility_link_and_a_solicited_communication()
    {
        await SeedSampleDataAsync();
        var consult = await PickAsync("professional", preAuth: false, withItemCode: "66500-00-00");
        await _host.QueryAsync(async db =>
        {
            var patient = await db.Patients.SingleAsync(p => p.Id == consult.PatientId);
            patient.NationalId = NphiesSandboxTransport.ActiveMemberId; // the test case's member, an Iqama number
            patient.NationalIdType = "PRC";
            return await db.SaveChangesAsync();
        });
        var check = await _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.CheckAsync(new EligibilityCheckRequestDto
        {
            PatientId = consult.PatientId, CoverageId = consult.CoverageId, ProviderId = consult.ProviderId, Purposes = { "validation" },
        }));
        Assert.AreEqual(true, check.IsEligible);
        await ChangeAsync(consult.Id, (db, c) =>
        {
            AsPriorAuthorization(c);
            c.Priority = "deferred";
            var sequence = c.SupportingInfo.Max(s => s.Sequence) + 1;
            db.Add(new ClaimSupportingInfoEntity
            {
                ClaimId = c.Id, Sequence = sequence, Category = "lab-test", CategorySystem = InfoCategories,
                CodeValue = "80096-1", CodeSystem = "http://loinc.org", QuantityValue = 5.4m, QuantityUnit = "mmol/L",
                DateValue = c.ServicedPeriodStart ?? DateTime.UtcNow.Date,
            });
            var lab = c.Items.Single(i => i.ProductOrServiceCode == "66500-00-00");
            lab.InformationSequence = string.IsNullOrWhiteSpace(lab.InformationSequence) ? $"{sequence}" : $"{lab.InformationSequence},{sequence}";
        });
        _sandbox.AskForInformationWhenPended = true;

        Assert.AreEqual(RcmClaimStatus.Queued, (await SubmitAsync(consult.Id)).ClaimStatus);

        var claim = ClaimOf(Sent());
        var eligibility = claim.GetExtension(NphiesExtensions.EligibilityResponse)?.Value as ResourceReference;
        Assert.AreEqual(check.ResponseIdentifier, eligibility?.Identifier?.Value, "The online eligibility response is referenced.");
        var labTest = claim.SupportingInfo.Single(s => s.Category.Coding.First().Code == "lab-test");
        Assert.AreEqual("80096-1", labTest.Code.Coding.Single().Code);

        // The payer asks for more information; the provider answers with an attachment.
        var first = await PollAsync();
        var requestId = first.CommunicationRequestIds.Single();
        Assert.IsFalse(RcmClaimStatus.Adjudicated.Contains(await StatusAsync(consult.Id)), "No decision before the answer.");
        var reply = await _host.RunAsync<IRcmCommunicationService, CommunicationDto>(s => s.RespondAsync(requestId, new SendCommunicationDto
        {
            Texts = { "The lipid profile report is attached." },
            Attachments = { new AttachmentDto { ContentType = "application/pdf", Title = "Lipid profile", Data = Convert.ToBase64String("%PDF-1.4 sandbox report"u8.ToArray()) } },
        }));
        CollectionAssert.AreEqual(Array.Empty<string>(), reply.Errors);
        var communication = Sent("communication").Entry.Select(e => e.Resource).OfType<Communication>().Single();
        Assert.IsTrue(communication.Payload.Any(p => p.Content is Attachment), "The answer carries an attachment.");

        var second = await PollAsync();
        CollectionAssert.Contains(second.ClaimIds, consult.Id);
        AssertAdjudicated(await StatusAsync(consult.Id));
    }

    [TestMethod]
    public async Task Case_3_dental_with_offline_eligibility_then_cancelled()
    {
        await SeedSampleDataAsync();
        var pa = await PickAsync("oral", preAuth: true);
        await _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.RecordOfflineAsync(new OfflineEligibilityRequestDto
        {
            PatientId = pa.PatientId, CoverageId = pa.CoverageId, ProviderId = pa.ProviderId, Reference = "OFF-ELIG-0003", Date = DateTime.UtcNow.AddHours(-1),
        }));

        await SubmitAsync(pa.Id);

        AssertAdjudicated(await StatusAsync(pa.Id));
        var claim = ClaimOf(Sent());
        Assert.AreEqual("normal", claim.Priority.Coding.Single().Code);
        Assert.AreEqual("OFF-ELIG-0003", (claim.GetExtension(NphiesExtensions.EligibilityOfflineReference)?.Value as FhirString)?.Value);
        StringAssert.EndsWith(claim.Item.Single().BodySite.Coding.Single().System, "fdi-oral-region", "The tooth is sent as an FDI oral region.");

        var cancel = await _host.RunAsync<IRcmClaimWorkflowService, TaskResultDto>(s => s.CancelAsync(pa.Id, new CancelClaimRequestDto { Reason = "WI" }));

        CollectionAssert.AreEqual(Array.Empty<string>(), cancel.Errors);
        Assert.AreEqual("cancel-request", _host.Transport.EventCodes.Last());
        Assert.AreEqual(RcmClaimStatus.Cancelled, await StatusAsync(pa.Id));
    }

    [TestMethod]
    public async Task Case_4_optical_with_a_vision_prescription()
    {
        await SeedSampleDataAsync();
        var pa = await PickAsync("vision", preAuth: true);
        await ChangeAsync(pa.Id, (db, c) =>
        {
            var lens = c.Items.Single();
            db.AddRange(
                new VisionPrescriptionEntity { ClaimItemId = lens.Id, LensSpecification = "OD -2.00 / -0.50 x 90", Product = "lens", Eye = "right", Sphere = -2.00m, Cylinder = -0.50m, Axis = 90, Brand = "Essilor" },
                new VisionPrescriptionEntity { ClaimItemId = lens.Id, LensSpecification = "OS -1.75 / -0.25 x 85", Product = "lens", Eye = "left", Sphere = -1.75m, Cylinder = -0.25m, Axis = 85, Brand = "Essilor" });
        });

        await SubmitAsync(pa.Id);

        AssertAdjudicated(await StatusAsync(pa.Id));
        Assert.IsTrue(Sent().Entry.Any(e => e.Resource is VisionPrescription), "The vision prescription is sent.");
    }

    [TestMethod]
    public async Task Case_5_pharmacy_with_an_unlisted_drug_is_partly_rejected_and_case_6_resubmitted_without_it()
    {
        await SeedSampleDataAsync();
        var pharmacy = await PickAsync("pharmacy", preAuth: false);
        await ChangeAsync(pharmacy.Id, (db, c) =>
        {
            AsPriorAuthorization(c);
            c.IsShadowBilling = true;
            var items = c.Items.OrderBy(i => i.Sequence).ToList();
            items[0].ProductOrServiceCode = "06285097000056";
            items[0].AltProductOrServiceCode = "PH-0001";
            items[1].ProductOrServiceCode = "06285096000798";
            items[1].AltProductOrServiceCode = "PH-0002";
            var sequence = c.SupportingInfo.Max(s => s.Sequence) + 1;
            db.Add(new ClaimSupportingInfoEntity { ClaimId = c.Id, Sequence = sequence, Category = "days-supply", CategorySystem = InfoCategories, QuantityValue = 10, QuantityUnit = "d" });
            var unlisted = (ClaimItemEntity)db.Entry(items[1]).CurrentValues.ToObject();
            unlisted.Id = Guid.NewGuid().ToString();
            unlisted.Sequence = 3;
            unlisted.ProductOrServiceCode = "99999999999999";
            unlisted.ProductOrServiceDisplay = "Unlisted drug";
            unlisted.AltProductOrServiceCode = "SFDA-2512-0001"; // the SFDA registration number of the drug
            unlisted.InformationSequence = $"{sequence}";
            db.Add(unlisted);
            c.Total = items.Sum(i => i.Net ?? 0m) + (unlisted.Net ?? 0m);
        });

        var submit = await SubmitAsync(pharmacy.Id);

        Assert.AreEqual(RcmClaimStatus.PartiallyApproved, submit.ClaimStatus, string.Join("; ", submit.Errors));
        var claim = ClaimOf(Sent());
        Assert.IsTrue(claim.SupportingInfo.Count(s => s.Category.Coding.First().Code == "days-supply") >= 2, "Several days-supply entries.");
        foreach (var item in claim.Item)
            Assert.IsTrue(item.InformationSequence.Any(seq => claim.SupportingInfo.Single(s => s.Sequence == seq).Category.Coding.First().Code == "days-supply"),
                $"Item {item.Sequence} points at its days-supply.");
        var drug = claim.Item.Single(i => i.Sequence == 3);
        CollectionAssert.AreEquivalent(new[] { "99999999999999", "SFDA-2512-0001" }, drug.ProductOrService.Coding.Select(c => c.Code).ToList(), "Shadow billing.");
        var adjudication = await _host.RunAsync<IRcmClaimWorkflowService, ClaimAdjudicationDto?>(s => s.GetLatestAdjudicationAsync(pharmacy.Id));
        Assert.AreEqual("rejected", adjudication!.Items.Single(i => i.Sequence == 3).Outcome);

        // Case 6: resubmitted as a related "prior" request without the rejected drug.
        var resubmit = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s =>
            s.UpdatePreAuthAsync(pharmacy.Id, new DerivedClaimRequestDto { RemoveItemSequences = { 3 } }));

        var again = ClaimOf(Sent());
        Assert.AreEqual(claim.Identifier.Single().Value, again.Related.Single().Claim.Identifier.Value);
        Assert.AreEqual("prior", again.Related.Single().Relationship.Coding.Single().Code);
        Assert.AreEqual(2, again.Item.Count);
        Assert.AreEqual(RcmClaimStatus.Approved, resubmit.ClaimStatus);
    }

    [TestMethod]
    public async Task Case_8_newborn_pended_then_decided()
    {
        await SeedSampleDataAsync();
        var consult = await PickAsync("professional", preAuth: false, withItemCode: "66500-00-00");
        var newborn = new PatientEntity
        {
            MRN = "MRN-NB-0008", NationalIdType = "MRN", FirstName = "Baby", LastName = "Saleh", FullName = "Baby Saleh", Gender = "F",
            DateOfBirth = DateTime.UtcNow.Date.AddDays(-20),
        };
        await _host.QueryAsync(async db => { db.Patients.Add(newborn); return await db.SaveChangesAsync(); });
        await ChangeAsync(consult.Id, (db, c) =>
        {
            AsPriorAuthorization(c);
            c.Priority = "deferred";
            c.PatientId = newborn.Id; // the coverage stays the mother's
            c.IsNewborn = true;
            db.RemoveRange(c.SupportingInfo.Where(s => s.Category == "last-menstrual-period"));
            db.Add(new ClaimSupportingInfoEntity
            {
                ClaimId = c.Id, Sequence = c.SupportingInfo.Max(s => s.Sequence) + 1, Category = "birth-weight", CategorySystem = InfoCategories,
                QuantityValue = 3.2m, QuantityUnit = "kg", DateValue = newborn.DateOfBirth,
            });
            db.Add(new ClaimDiagnosisEntity
            {
                ClaimId = c.Id, Sequence = c.Diagnoses.Max(d => d.Sequence) + 1, DiagnosisCode = "Z38.0",
                DiagnosisSystem = c.Diagnoses.First().DiagnosisSystem, DiagnosisDisplay = "Singleton, born in hospital", DiagnosisType = "secondary",
                DiagnosisTypeSystem = c.Diagnoses.First().DiagnosisTypeSystem,
            });
        });

        Assert.AreEqual(RcmClaimStatus.Queued, (await SubmitAsync(consult.Id)).ClaimStatus);

        var claim = ClaimOf(Sent());
        Assert.AreEqual(true, (claim.GetExtension(NphiesExtensions.Newborn)?.Value as FhirBoolean)?.Value);
        CollectionAssert.Contains(Categories(claim), "birth-weight");
        Assert.IsTrue(claim.Diagnosis.Any(d => (d.Diagnosis as CodeableConcept)?.Coding.Any(c => c.Code!.StartsWith("Z38")) == true));
        await PollAsync();
        AssertAdjudicated(await StatusAsync(consult.Id));
    }

    [TestMethod]
    public async Task Case_9_referral_with_the_transfer_extension_pended_then_decided()
    {
        await SeedSampleDataAsync();
        var consult = await PickAsync("professional", preAuth: false, withItemCode: "66500-00-00");
        await ChangeAsync(consult.Id, (_, c) =>
        {
            AsPriorAuthorization(c);
            c.Priority = "deferred";
            c.IsTransfer = true;
        });

        Assert.AreEqual(RcmClaimStatus.Queued, (await SubmitAsync(consult.Id)).ClaimStatus);

        Assert.AreEqual(true, (ClaimOf(Sent()).GetExtension(NphiesExtensions.Transfer)?.Value as FhirBoolean)?.Value);
        await PollAsync();
        AssertAdjudicated(await StatusAsync(consult.Id));
    }

    [TestMethod]
    public async Task Case_10_advanced_authorization_received_discussed_and_cancelled_by_the_payer()
    {
        var seed = await _host.SeedPartiesAsync();
        var apa = RcmRecoupmentAndAdvancedAuthTests.BuildAdvancedAuthorization();
        _sandbox.Deliver(RcmTestHost.ProviderLicense, apa);
        _sandbox.Deliver(RcmTestHost.ProviderLicense, CommunicationRequestAbout("aa_resp_1"));
        var onlyAuthorizations = new PollRequestDto { ProviderId = seed.Provider.Id, IncludeMessageTypes = { "advanced-authorization" } };

        // The poll asks for advanced authorizations only: the communication request stays queued.
        var first = await PollAsync(onlyAuthorizations);
        var id = first.AdvancedAuthorizationIds.Single();
        Assert.AreEqual(0, first.CommunicationRequestIds.Count);
        Assert.AreEqual(1, _sandbox.Pending(RcmTestHost.ProviderLicense));
        var auth = await _host.RunAsync<IRcmAdvancedAuthorizationService, AdvancedAuthorizationDto?>(s => s.GetAsync(id, null));
        Assert.AreEqual("referral", auth!.Reason);
        Assert.AreEqual("AA-12345", auth.PreAuthRef);
        Assert.AreEqual(seed.Insurer.Id, auth.InsurerId);
        Assert.AreEqual("Active", auth.Status);

        // The payer's request for information, and the provider's answer about the authorization.
        var second = await PollAsync(new PollRequestDto { ProviderId = seed.Provider.Id });
        var requestId = second.CommunicationRequestIds.Single();
        var request = await _host.RunAsync<IRcmCommunicationService, CommunicationRequestDto?>(s => s.GetRequestAsync(requestId));
        Assert.AreEqual(id, request!.AdvancedAuthorizationId, "The request is linked to the authorization it names.");
        Assert.AreEqual("AdvancedAuthorization", request.Kind);
        Assert.IsTrue(request.IsOpen);
        Assert.IsNotNull(request.DueAt);
        var answer = await _host.RunAsync<IRcmCommunicationService, CommunicationDto>(s => s.RespondAsync(requestId, new SendCommunicationDto
        {
            Texts = { "The patient will attend on the referral date; the requested report is on file." },
        }));
        CollectionAssert.AreEqual(Array.Empty<string>(), answer.Errors);
        var reply = Sent("communication").Entry.Select(e => e.Resource).OfType<Communication>().Single();
        Assert.AreEqual("CommReq_APA_1", reply.BasedOn.Single().Identifier.Value);
        Assert.AreEqual("aa_resp_1", reply.About.Single().Identifier.Value);
        Assert.IsFalse((await _host.RunAsync<IRcmCommunicationService, CommunicationRequestDto?>(s => s.GetRequestAsync(requestId)))!.IsOpen);

        // The payer cancels it by sending it again with reissue reason "cancel".
        _sandbox.Deliver(RcmTestHost.ProviderLicense, RcmRecoupmentAndAdvancedAuthTests.Reissue(apa, "cancel", newResponse: true));
        await PollAsync(onlyAuthorizations);
        Assert.AreEqual("Cancelled", (await _host.RunAsync<IRcmAdvancedAuthorizationService, AdvancedAuthorizationDto?>(s => s.GetAsync(id, null)))!.Status);
    }

    /// <summary>The docs' communication request (18a), sent by INS-FHIR about an advanced authorization.</summary>
    private static string CommunicationRequestAbout(string authorizationIdentifier)
    {
        var bundle = JsonNode.Parse(DocsExamples.ReadJson("18a - Communication Request.json"))!;
        bundle["id"] = Guid.NewGuid().ToString();
        var entries = bundle["entry"]!.AsArray();
        var header = entries[0]!["resource"]!;
        header["id"] = Guid.NewGuid().ToString();
        header["sender"]!["identifier"]!["value"] = RcmTestHost.PayerLicense;
        header["destination"]![0]!["receiver"]!["identifier"]!["value"] = RcmTestHost.ProviderLicense;
        var request = entries.Select(e => e!["resource"]!).First(r => (string?)r["resourceType"] == "CommunicationRequest");
        request["identifier"]![0]!["value"] = "CommReq_APA_1";
        request["about"] = new JsonArray(new JsonObject
        {
            ["type"] = "ClaimResponse",
            ["identifier"] = new JsonObject { ["system"] = "http://sni.com.sa/claimresponse", ["value"] = authorizationIdentifier },
        });
        return bundle.ToJsonString();
    }
}
