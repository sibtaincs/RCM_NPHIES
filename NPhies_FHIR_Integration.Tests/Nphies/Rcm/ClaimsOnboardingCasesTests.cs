using System.Text.Json.Nodes;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Parsing;
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
/// The nine NPHIES onboarding test cases for claims (Claims test cases V4.0, against the pseudo-payer INS-FHIR), run end
/// to end against the sandbox simulator with the sample data's IG-conformant claims.
/// </summary>
[TestClass]
public class ClaimsOnboardingCasesTests
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
        await _host.QueryAsync(async db =>
        {
            await SampleDataSeeder.SeedAsync(db, NullLogger.Instance);
            return true;
        });
    }

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    // ----- helpers -----

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

    /// <summary>Gives the claim's patient one of the test case's Iqama numbers.</summary>
    private Task MemberAsync(string patientId, string iqama) => _host.QueryAsync(async db =>
    {
        var patient = await db.Patients.SingleAsync(p => p.Id == patientId);
        patient.NationalId = iqama;
        patient.NationalIdType = "PRC";
        return await db.SaveChangesAsync();
    });

    private Task<EligibilityCheckDto> EligibilityAsync(ClaimEntity claim) =>
        _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.CheckAsync(new EligibilityCheckRequestDto
        {
            PatientId = claim.PatientId, CoverageId = claim.CoverageId, ProviderId = claim.ProviderId, Purposes = { "validation" },
        }));

    private static void AsPriorAuthorization(ClaimEntity claim)
    {
        claim.Use = "preauthorization";
        claim.ClaimIdentifierSystem = null;
        claim.ClaimIdentifierValue = null;
    }

    private Task<SubmissionResultDto> SubmitAsync(string claimId) =>
        _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claimId));

    private Task<SubmissionResultDto> ClaimFromAsync(string preAuthId) =>
        _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.CreateClaimFromPreAuthAsync(preAuthId, new DerivedClaimRequestDto { Submit = false }));

    private Task<PollRunResultDto> PollAsync() =>
        _host.RunAsync<IRcmPollService, PollRunResultDto>(s => s.PollAsync(new PollRequestDto()));

    private async Task<string?> StatusAsync(string claimId) => (await _host.GetClaimAsync(claimId)).NphiesStatus;

    /// <summary>The payer's decision (adjudication-outcome); the status may already have moved on to Paid.</summary>
    private async Task<string?> DecisionAsync(string claimId) => (await _host.GetClaimAsync(claimId)).AdjudicationOutcome;

    private Bundle Sent(string eventCode = "claim-request")
    {
        var request = _host.Transport.Requests.Last(r => r.EventCode == eventCode);
        var bundle = new FhirJsonParser().Parse<Bundle>(request.Json);
        var errors = new NphiesProfileValidator(NphiesConformancePackage.Embedded).Validate(bundle).Errors.ToList();
        Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors.Select(e => $"{e.RuleId} {e.Expression}: {e.Message}")));
        return bundle;
    }

    private static Claim ClaimOf(Bundle bundle) => bundle.Entry.Select(e => e.Resource).OfType<Claim>().Single();

    private static List<string> Categories(Claim claim) => claim.SupportingInfo.Select(s => s.Category.Coding.First().Code).ToList();

    private static ClaimSupportingInfoEntity Info(ClaimEntity c, int sequence, string category, decimal? quantity = null, string? unit = null,
        string? code = null, string? codeSystem = null, string? text = null, bool timed = true) => new()
    {
        ClaimId = c.Id, Sequence = sequence, Category = category, CategorySystem = InfoCategories, QuantityValue = quantity, QuantityUnit = unit,
        CodeValue = code, CodeSystem = codeSystem, StringValue = text, DateValue = timed ? c.ServicedPeriodStart ?? DateTime.UtcNow.Date : null,
    };

    private static ClaimItemEntity CopyItem(ApplicationDbContext db, ClaimItemEntity source, int sequence, string code, string? system = null, string? display = null)
    {
        var item = (ClaimItemEntity)db.Entry(source).CurrentValues.ToObject();
        item.Id = Guid.NewGuid().ToString();
        item.Sequence = sequence;
        item.ProductOrServiceCode = code;
        if (system != null) item.ProductOrServiceSystem = system;
        if (display != null) item.ProductOrServiceDisplay = display;
        db.Add(item);
        return item;
    }

    private static void AssertAdjudicated(string? status) =>
        Assert.IsTrue(RcmClaimStatus.Adjudicated.Contains(status), $"Expected a final decision, got {status}.");

    /// <summary>The pharmacy authorization of case 1: days-supply per medication, an unlisted drug with its SFDA code, and a medical device.</summary>
    private async Task<string> PharmacyPriorAuthorizationAsync()
    {
        var pharmacy = await PickAsync("pharmacy", preAuth: false);
        await ChangeAsync(pharmacy.Id, (db, c) =>
        {
            AsPriorAuthorization(c);
            c.IsShadowBilling = true;
            var items = c.Items.OrderBy(i => i.Sequence).ToList();
            items[0].AltProductOrServiceCode = "PH-0001";
            items[1].AltProductOrServiceCode = "PH-0002";
            var sequence = c.SupportingInfo.Max(s => s.Sequence) + 1;
            db.Add(Info(c, sequence, "days-supply", 10, "d", timed: false));
            var unlisted = CopyItem(db, items[1], 3, "99999999999999", display: "Unlisted drug");
            unlisted.AltProductOrServiceCode = "SFDA-2512-0001"; // the drug's SFDA registration number
            unlisted.InformationSequence = $"{sequence}";
            var device = CopyItem(db, items[1], 4, "58380", RcmClaimRules.MedicationCodes.Replace("medication-codes", "medical-devices"), "Total testosterone IVD, control");
            device.AltProductOrServiceCode = "PH-0004";
            device.InformationSequence = null;
            c.Total = items.Sum(i => i.Net ?? 0m) + (unlisted.Net ?? 0m) + (device.Net ?? 0m);
        });
        return pharmacy.Id;
    }

    // ----- the cases -----

    [TestMethod]
    public async Task Case_1_pharmacy_claim_with_its_authorization_reference_and_a_solicited_communication_is_approved()
    {
        var preAuthId = await PharmacyPriorAuthorizationAsync();
        _sandbox.DecideAs = "approved";
        Assert.AreEqual(RcmClaimStatus.Approved, (await SubmitAsync(preAuthId)).ClaimStatus);

        var claimId = (await ClaimFromAsync(preAuthId)).ClaimId;
        await ChangeAsync(claimId, (_, c) => c.Priority = "deferred");
        _sandbox.AskForInformationWhenPended = true;
        Assert.AreEqual(RcmClaimStatus.Queued, (await SubmitAsync(claimId)).ClaimStatus);

        var claim = ClaimOf(Sent());
        var preAuthRef = (await _host.GetClaimAsync(claimId)).PreAuthorizationRef;
        CollectionAssert.AreEqual(new[] { preAuthRef }, claim.Insurance.Single().PreAuthRef.ToList(), "The authorization's preAuthRef.");
        Assert.IsNotNull(claim.GetExtension(NphiesExtensions.PriorAuthResponse), "The authorization response is referenced.");
        Assert.IsTrue(claim.SupportingInfo.Count(s => s.Category.Coding.First().Code == "days-supply") >= 2);
        foreach (var medication in claim.Item.Where(i => i.ProductOrService.Coding.First().System == RcmClaimRules.MedicationCodes))
            Assert.AreEqual(1, medication.InformationSequence.Count(seq => claim.SupportingInfo.Single(s => s.Sequence == seq).Category.Coding.First().Code == "days-supply"));
        CollectionAssert.Contains(claim.Item.Single(i => i.Sequence == 3).ProductOrService.Coding.Select(c => c.Code).ToList(), "SFDA-2512-0001", "Shadow billing.");
        StringAssert.EndsWith(claim.Item.Single(i => i.Sequence == 4).ProductOrService.Coding.First().System, "medical-devices");

        var requestId = (await PollAsync()).CommunicationRequestIds.Single();
        await _host.RunAsync<IRcmCommunicationService, CommunicationDto>(s => s.RespondAsync(requestId, new SendCommunicationDto { Texts = { "The prescription is on file." } }));
        await PollAsync();
        Assert.AreEqual("approved", await DecisionAsync(claimId), "Approved (the payer may also have paid it by now).");
    }

    [TestMethod]
    public async Task Case_2_professional_claim_with_an_eligibility_reference_and_an_unsolicited_communication_is_rejected()
    {
        var consult = await PickAsync("professional", preAuth: false, withItemCode: "66500-00-00");
        await MemberAsync(consult.PatientId, NphiesSandboxTransport.ActiveMemberId);
        var check = await EligibilityAsync(consult);
        await ChangeAsync(consult.Id, (db, c) =>
        {
            c.Priority = "deferred";
            var sequence = c.SupportingInfo.Max(s => s.Sequence);
            foreach (var (category, quantity, unit) in new[] { ("temperature", 37.2m, "Cel"), ("pulse", 78m, "/min") })
                if (c.SupportingInfo.All(s => s.Category != category)) db.Add(Info(c, ++sequence, category, quantity, unit));
            if (c.SupportingInfo.All(s => s.Category != "onset")) db.Add(Info(c, ++sequence, "onset", code: "N94.6", codeSystem: c.Diagnoses.First().DiagnosisSystem));
            db.Add(Info(c, ++sequence, "lab-test", 5.4m, "mmol/L", "80096-1", "http://loinc.org"));
            var lab = c.Items.Single(i => i.ProductOrServiceCode == "66500-00-00");
            lab.InformationSequence = string.IsNullOrWhiteSpace(lab.InformationSequence) ? $"{sequence}" : $"{lab.InformationSequence},{sequence}";
        });
        _sandbox.DecideAs = "rejected";

        Assert.AreEqual(RcmClaimStatus.Queued, (await SubmitAsync(consult.Id)).ClaimStatus);

        var claim = ClaimOf(Sent());
        Assert.AreEqual(check.ResponseIdentifier, (claim.GetExtension(NphiesExtensions.EligibilityResponse)?.Value as ResourceReference)?.Identifier?.Value);
        foreach (var category in new[] { "vital-sign-systolic", "vital-sign-diastolic", "onset", "temperature", "pulse", "lab-test" })
            CollectionAssert.Contains(Categories(claim), category);
        Assert.IsTrue(claim.Item.Select(i => i.ProductOrService.Coding.First().System).Distinct().Count() > 1, "Items from different code systems.");

        // Unsolicited communication about the lab service (extension-ClaimItemSequence, ClaimItem guide V1.1).
        var labSequence = await _host.QueryAsync(db => db.ClaimItems.Where(i => i.ClaimId == consult.Id && i.ProductOrServiceCode == "66500-00-00").Select(i => i.Sequence).SingleAsync());
        await _host.RunAsync<IRcmCommunicationService, CommunicationDto>(s => s.SendAsync(new SendCommunicationDto
        {
            ClaimId = consult.Id,
            Items = { new CommunicationItemNoteDto { Sequence = labSequence, Text = "The lipid profile was ordered because of a family history of hyperlipidaemia." } },
        }));
        var payload = Sent("communication").Entry.Select(e => e.Resource).OfType<Communication>().Single().Payload.Single();
        Assert.AreEqual(labSequence, (payload.GetExtension(NphiesExtensions.ClaimItemSequence)?.Value as PositiveInt)?.Value);
        await PollAsync();
        Assert.AreEqual("rejected", await DecisionAsync(consult.Id));
    }

    [TestMethod]
    public async Task Case_3_institutional_claim_with_offline_eligibility_is_partly_approved_and_its_status_is_complete()
    {
        var claimEntity = await PickAsync("institutional", preAuth: false);
        await _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.RecordOfflineAsync(new OfflineEligibilityRequestDto
        {
            PatientId = claimEntity.PatientId, CoverageId = claimEntity.CoverageId, ProviderId = claimEntity.ProviderId, Reference = "OFF-ELIG-C3", Date = DateTime.UtcNow.AddHours(-2),
        }));
        await ChangeAsync(claimEntity.Id, (db, c) =>
        {
            c.Priority = "deferred";
            db.Add(new ClaimSupportingInfoEntity
            {
                ClaimId = c.Id, Sequence = c.SupportingInfo.Max(s => s.Sequence) + 1, Category = "attachment", CategorySystem = InfoCategories,
                AttachmentTitle = "Discharge summary", AttachmentContentType = "application/pdf", AttachmentData = Convert.ToBase64String("%PDF-1.4 discharge summary"u8.ToArray()),
            });
        });
        _sandbox.DecideAs = "partial";

        Assert.AreEqual(RcmClaimStatus.Queued, (await SubmitAsync(claimEntity.Id)).ClaimStatus);

        var bundle = Sent();
        var claim = ClaimOf(bundle);
        Assert.AreEqual("OFF-ELIG-C3", (claim.GetExtension(NphiesExtensions.EligibilityOfflineReference)?.Value as FhirString)?.Value);
        foreach (var category in new[] { "vital-sign-diastolic", "vital-sign-height", "vital-sign-systolic", "vital-sign-weight", "attachment", "icu-hours", "chief-complaint" })
            CollectionAssert.Contains(Categories(claim), category);
        var encounter = bundle.Entry.Select(e => e.Resource).OfType<Encounter>().Single();
        Assert.IsNotNull(encounter.Period?.Start);
        Assert.IsNotNull(encounter.Period?.End, "An in-patient claim has the end of the stay.");
        Assert.IsTrue(claim.Item.Any(i => (i.GetExtension(NphiesExtensions.Package)?.Value as FhirBoolean)?.Value == true && i.Detail.Count >= 2),
            "A package item with several details.");

        await PollAsync();
        Assert.AreEqual("partial", await DecisionAsync(claimEntity.Id));

        var status = await _host.RunAsync<IRcmClaimWorkflowService, TaskResultDto>(s => s.StatusCheckAsync(claimEntity.Id));
        Assert.AreEqual("complete", status.ProcessingStatus);
    }

    [TestMethod]
    public async Task Case_4_dental_claim_with_an_offline_authorization_is_approved_then_cancelled()
    {
        var dental = await PickAsync("oral", preAuth: false);
        await ChangeAsync(dental.Id, (_, c) =>
        {
            c.PreAuthorizationRef = "OFF-PA-0004"; // given by the payer by phone
            c.AuthorizationOfflineDate = DateTime.UtcNow.Date.AddDays(-1);
        });
        _sandbox.DecideAs = "approved";

        Assert.AreEqual(RcmClaimStatus.Approved, (await SubmitAsync(dental.Id)).ClaimStatus);

        var claim = ClaimOf(Sent());
        Assert.AreEqual("normal", claim.Priority.Coding.Single().Code);
        CollectionAssert.AreEqual(new[] { "OFF-PA-0004" }, claim.Insurance.Single().PreAuthRef.ToList());
        Assert.IsNotNull(claim.GetExtension(NphiesExtensions.AuthorizationOfflineDate));
        Assert.IsTrue(claim.Item.Any(i => i.BodySite?.Coding.Any(c => c.System!.EndsWith("fdi-oral-region")) == true), "The tooth as an FDI oral region.");

        var cancel = await _host.RunAsync<IRcmClaimWorkflowService, TaskResultDto>(s => s.CancelAsync(dental.Id, new CancelClaimRequestDto { Reason = "WI" }));
        CollectionAssert.AreEqual(Array.Empty<string>(), cancel.Errors);
        Assert.AreEqual(RcmClaimStatus.Cancelled, await StatusAsync(dental.Id));
    }

    [TestMethod]
    public async Task Case_5_a_claim_for_an_unknown_member_without_references_gets_an_error_response()
    {
        var consult = await PickAsync("professional", preAuth: false, withItemCode: "66500-00-00");
        await MemberAsync(consult.PatientId, NphiesSandboxTransport.UnknownMemberId);

        var submit = await SubmitAsync(consult.Id);

        Assert.AreEqual(RcmClaimStatus.Error, submit.ClaimStatus);
        var claim = ClaimOf(Sent());
        Assert.IsNull(claim.GetExtension(NphiesExtensions.EligibilityResponse));
        Assert.IsNull(claim.GetExtension(NphiesExtensions.EligibilityOfflineReference));
        Assert.AreEqual(0, claim.Insurance.Single().PreAuthRef.Count(), "No authorization reference.");
        var transaction = await _host.QueryAsync(db => db.NphiesTransactions.AsNoTracking().SingleAsync(t => t.Direction == "outbound" && t.EntityId == consult.Id));
        Assert.AreEqual("PayerBusinessError", transaction.ErrorKind);
        StringAssert.Contains(transaction.ErrorSummary, "1658");
    }

    [TestMethod]
    public async Task Case_6_newborn_claim_pended_then_approved()
    {
        var consult = await PickAsync("professional", preAuth: false, withItemCode: "66500-00-00");
        var newborn = new PatientEntity
        {
            MRN = "MRN-NB-C006", NationalIdType = "MRN", FirstName = "Baby", LastName = "Saleh", FullName = "Baby Saleh", Gender = "M",
            DateOfBirth = DateTime.UtcNow.Date.AddDays(-15),
        };
        await _host.QueryAsync(async db => { db.Patients.Add(newborn); return await db.SaveChangesAsync(); });
        await MemberAsync(consult.PatientId, NphiesSandboxTransport.ActiveMemberId); // the mother
        await ChangeAsync(consult.Id, (db, c) =>
        {
            c.Priority = "deferred";
            c.PatientId = newborn.Id;
            c.IsNewborn = true;
            db.RemoveRange(c.SupportingInfo.Where(s => s.Category == "last-menstrual-period"));
            db.Add(new ClaimSupportingInfoEntity
            {
                ClaimId = c.Id, Sequence = c.SupportingInfo.Max(s => s.Sequence) + 1, Category = "birth-weight", CategorySystem = InfoCategories,
                QuantityValue = 3.4m, QuantityUnit = "kg", DateValue = newborn.DateOfBirth,
            });
            db.Add(new ClaimDiagnosisEntity
            {
                ClaimId = c.Id, Sequence = c.Diagnoses.Max(d => d.Sequence) + 1, DiagnosisCode = "Z38.0", DiagnosisDisplay = "Singleton, born in hospital",
                DiagnosisSystem = c.Diagnoses.First().DiagnosisSystem, DiagnosisType = "secondary", DiagnosisTypeSystem = c.Diagnoses.First().DiagnosisTypeSystem,
            });
        });
        _sandbox.DecideAs = "approved";

        Assert.AreEqual(RcmClaimStatus.Queued, (await SubmitAsync(consult.Id)).ClaimStatus);

        var bundle = Sent();
        var claim = ClaimOf(bundle);
        Assert.AreEqual(true, (claim.GetExtension(NphiesExtensions.Newborn)?.Value as FhirBoolean)?.Value);
        CollectionAssert.Contains(Categories(claim), "birth-weight");
        var patients = bundle.Entry.Select(e => e.Resource).OfType<Patient>().ToList();
        Assert.AreEqual(2, patients.Count, "The mother and the newborn.");
        Assert.IsTrue(patients.Any(p => p.Identifier.Any(i => i.Value == NphiesSandboxTransport.ActiveMemberId)), "The mother by Iqama.");
        Assert.IsTrue(patients.Any(p => p.Identifier.Any(i => i.Value!.Contains("MRN-NB-C006"))), "The newborn by MRN.");
        await PollAsync();
        Assert.AreEqual("approved", await DecisionAsync(consult.Id));
    }

    [TestMethod]
    public async Task Case_7_batch_of_claims_is_queued_then_decided_and_matched_by_the_batch_extensions()
    {
        var pair = await _host.QueryAsync(db => db.Claims.AsNoTracking()
            .Where(c => c.ClaimNumber.StartsWith(SampleDataSeeder.ClaimPrefix) && c.Use == "claim")
            .GroupBy(c => new { c.InsurerId, c.ProviderId }).Where(g => g.Count() >= 2)
            .Select(g => g.OrderBy(c => c.ClaimNumber).Select(c => c.Id).Take(2).ToList()).FirstAsync());
        _sandbox.DecideAs = "approved";

        var batch = await _host.RunAsync<IRcmClaimWorkflowService, BatchSubmitResultDto>(s => s.SubmitBatchAsync(new BatchSubmitRequestDto { ClaimIds = pair }));

        CollectionAssert.AreEqual(Array.Empty<string>(), batch.Errors);
        Assert.AreEqual(2, batch.ClaimCount);
        var inner = Sent("batch-request").Entry.Select(e => e.Resource).OfType<Bundle>().Select(ClaimOf).ToList();
        Assert.AreEqual(2, inner.Count);
        foreach (var claim in inner)
            foreach (var url in new[] { NphiesExtensions.BatchIdentifier, NphiesExtensions.BatchNumber, NphiesExtensions.BatchPeriod })
                Assert.IsNotNull(claim.GetExtension(url), $"{url} on every claim.");

        var poll = await PollAsync();
        foreach (var id in pair)
        {
            CollectionAssert.Contains(poll.ClaimIds, id);
            AssertAdjudicated(await StatusAsync(id));
        }

        // A deferred response without the request identifier still finds its claim by batch-identifier and batch-number.
        var first = await _host.GetClaimAsync(pair[0]);
        var response = JsonNode.Parse(DocsExamples.ReadJson("7b - Claim Response.json"))!;
        response["id"] = Guid.NewGuid().ToString();
        response["entry"]![0]!["resource"]!["id"] = Guid.NewGuid().ToString();
        var claimResponse = response["entry"]!.AsArray().Select(e => e!["resource"]!).First(r => (string?)r["resourceType"] == "ClaimResponse").AsObject();
        claimResponse.Remove("request");
        claimResponse["identifier"]![0]!["value"] = "resp_batch_match";
        var extensions = claimResponse["extension"] as JsonArray ?? new JsonArray();
        extensions.Add(new JsonObject { ["url"] = NphiesExtensions.BatchIdentifier, ["valueIdentifier"] = new JsonObject { ["system"] = "http://provider.com/batch", ["value"] = first.BatchIdentifier } });
        extensions.Add(new JsonObject { ["url"] = NphiesExtensions.BatchNumber, ["valuePositiveInt"] = first.BatchNumber });
        claimResponse["extension"] = extensions;
        var inbound = await _host.RunAsync<IRcmInboundService, RcmInboundResultDto>(s => s.IngestRawAsync(response.ToJsonString()));
        CollectionAssert.Contains(inbound.ClaimIds, pair[0]);
    }

    [TestMethod]
    public async Task Case_8_optical_claim_with_eligibility_and_authorization_references_is_rejected()
    {
        var pa = await PickAsync("vision", preAuth: true);
        await MemberAsync(pa.PatientId, NphiesSandboxTransport.ActiveMemberId);
        var check = await EligibilityAsync(pa);
        await ChangeAsync(pa.Id, (db, c) =>
        {
            var lens = c.Items.Single();
            db.AddRange(
                new VisionPrescriptionEntity { ClaimItemId = lens.Id, LensSpecification = "OD -2.00 / -0.50 x 90", Product = "lens", Eye = "right", Sphere = -2.00m, Cylinder = -0.50m, Axis = 90, Brand = "Essilor" },
                new VisionPrescriptionEntity { ClaimItemId = lens.Id, LensSpecification = "OS -1.75 / -0.25 x 85", Product = "lens", Eye = "left", Sphere = -1.75m, Cylinder = -0.25m, Axis = 85, Brand = "Essilor" });
        });
        _sandbox.DecideAs = "approved";
        Assert.AreEqual(RcmClaimStatus.Approved, (await SubmitAsync(pa.Id)).ClaimStatus);

        var claimId = (await ClaimFromAsync(pa.Id)).ClaimId;
        await ChangeAsync(claimId, (_, c) => c.Priority = "deferred");
        _sandbox.DecideAs = "rejected";
        Assert.AreEqual(RcmClaimStatus.Queued, (await SubmitAsync(claimId)).ClaimStatus);

        var bundle = Sent();
        var claim = ClaimOf(bundle);
        Assert.AreEqual(check.ResponseIdentifier, (claim.GetExtension(NphiesExtensions.EligibilityResponse)?.Value as ResourceReference)?.Identifier?.Value);
        Assert.IsNotNull(claim.GetExtension(NphiesExtensions.PriorAuthResponse));
        Assert.IsTrue(bundle.Entry.Any(e => e.Resource is VisionPrescription), "The vision prescription is sent.");
        await PollAsync();
        Assert.AreEqual("rejected", await DecisionAsync(claimId));
    }

    [TestMethod]
    public async Task Case_9_a_300_item_inpatient_claim_is_a_large_claim_answered_by_poll()
    {
        _sandbox.LargeClaimItemThreshold = 299; // the test case sends 300 items as a large claim
        var claimEntity = await PickAsync("institutional", preAuth: false);
        await ChangeAsync(claimEntity.Id, (db, c) =>
        {
            var day = c.Items.Single(i => i.ProductOrServiceCode == "83600-00-00");
            var sequence = c.Items.Max(i => i.Sequence);
            while (sequence < 300)
            {
                var extra = CopyItem(db, day, ++sequence, day.ProductOrServiceCode);
                extra.Quantity = 1;
                extra.Net = extra.UnitPrice;
                c.Total += extra.Net ?? 0m;
            }
        });
        _sandbox.DecideAs = "approved";

        var submit = await SubmitAsync(claimEntity.Id);

        Assert.AreEqual(RcmClaimStatus.Queued, submit.ClaimStatus);
        Assert.AreEqual(300, ClaimOf(Sent()).Item.Count);
        var transaction = await _host.QueryAsync(db => db.NphiesTransactions.AsNoTracking().SingleAsync(t => t.Direction == "outbound" && t.EntityId == claimEntity.Id));
        StringAssert.Contains(transaction.MetaTags, NphiesMetaTags.LargeClaim);
        await PollAsync();
        AssertAdjudicated(await StatusAsync(claimEntity.Id));
    }
}
