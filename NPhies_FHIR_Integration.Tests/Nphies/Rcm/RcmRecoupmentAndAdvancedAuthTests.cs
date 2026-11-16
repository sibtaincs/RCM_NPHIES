using System.IO.Compression;
using System.Text.Json.Nodes;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

/// <summary>Recoupment / refund payment lines, insurer-initiated (advanced) authorizations and live-test readiness tooling.</summary>
[TestClass]
public class RcmRecoupmentAndAdvancedAuthTests
{
    private const string Ext = "http://nphies.sa/fhir/ksa/nphies-fs/StructureDefinition/extension-";
    private RcmTestHost _host = null!;

    [TestInitialize]
    public async Task Init() => _host = await RcmTestHost.CreateAsync();

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    [TestMethod]
    public async Task Recoupment_referencing_predecessor_reverses_claim_payment()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        _host.ReplyWith("7b - Claim Response.json");
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));

        var payment = JsonNode.Parse(DocsExamples.ReadJson("13a - Payment Reconciliation.json"))!;
        Recon(payment)["detail"]![0]!["identifier"] = new JsonObject { ["system"] = "http://sni.com.sa/paymentdetail", ["value"] = "pd-1" };
        await _host.RunAsync<IRcmInboundService, RcmInboundResultDto>(s => s.IngestRawAsync(payment.ToJsonString()));
        Assert.AreEqual(RcmClaimStatus.Paid, (await _host.GetClaimAsync(claim.Id)).NphiesStatus);

        var recoup = JsonNode.Parse(DocsExamples.ReadJson("13a - Payment Reconciliation.json"))!;
        recoup["id"] = Guid.NewGuid().ToString();
        recoup["entry"]![0]!["resource"]!["id"] = Guid.NewGuid().ToString();
        var r = Recon(recoup);
        r["id"] = "99061";
        r["identifier"]![0]!["value"] = "929061";
        r["paymentAmount"]!["value"] = -193.55m;
        var d = r["detail"]![0]!.AsObject();
        d.Remove("request");
        d["identifier"] = new JsonObject { ["system"] = "http://sni.com.sa/paymentdetail", ["value"] = "pd-2" };
        d["predecessor"] = new JsonObject { ["system"] = "http://sni.com.sa/paymentdetail", ["value"] = "pd-1" };
        d["type"]!["coding"]![0]!["code"] = "adjustment";
        d["amount"]!["value"] = -193.55m;
        d["extension"]![0]!["valueMoney"]!["value"] = -195.5m;
        var inbound = await _host.RunAsync<IRcmInboundService, RcmInboundResultDto>(s => s.IngestRawAsync(recoup.ToJsonString()));

        var dto = await _host.RunAsync<IRcmPaymentService, PaymentReconciliationDto?>(s => s.GetAsync(XAssert.Single(inbound.PaymentReconciliationIds)));
        var line = XAssert.Single(dto!.Details);
        Assert.AreEqual(RcmPaymentMatching.Recoupment, line.MatchStatus);
        Assert.AreEqual(claim.Id, line.MatchedClaimId, "matched through the predecessor payment line");
        Assert.AreEqual("pd-1", line.PredecessorIdentifier);
        Assert.IsNull(line.ExpectedAmount);
        Assert.AreEqual(-195.5m, dto.TotalRecouped);
        Assert.AreEqual(RcmPaymentMatching.Recoupment, dto.MatchStatus);

        var stored = await _host.GetClaimAsync(claim.Id);
        Assert.AreEqual(0m, stored.PaidAmount);
        Assert.AreEqual(RcmClaimStatus.Recouped, stored.NphiesStatus);

        var discrepancies = await _host.RunAsync<IRcmPaymentService, List<PaymentDetailDto>>(s => s.GetDiscrepanciesAsync(new RcmQuery()));
        Assert.AreEqual(line.Id, XAssert.Single(discrepancies).Id);
    }

    [TestMethod]
    public async Task Advanced_authorization_is_stored_and_quoted_on_a_claim()
    {
        var seed = await _host.SeedPartiesAsync();
        var json = BuildAdvancedAuthorization();

        var inbound = await _host.RunAsync<IRcmInboundService, RcmInboundResultDto>(s => s.IngestRawAsync(json));
        var id = XAssert.Single(inbound.AdvancedAuthorizationIds);
        var again = await _host.RunAsync<IRcmInboundService, RcmInboundResultDto>(s => s.IngestRawAsync(json));
        Assert.AreEqual(0, again.AdvancedAuthorizationIds.Count, "duplicate message is ignored");

        var auth = await _host.RunAsync<IRcmAdvancedAuthorizationService, AdvancedAuthorizationDto?>(s => s.GetAsync(id, null));
        Assert.IsNotNull(auth);
        Assert.AreEqual("referral", auth.Reason);
        Assert.AreEqual("AA-12345", auth.PreAuthRef);
        Assert.AreEqual(seed.Patient.Id, auth.PatientId);
        Assert.AreEqual(seed.Insurer.Id, auth.InsurerId);
        Assert.AreEqual(seed.Provider.Id, auth.ServiceProviderId);
        Assert.AreEqual("Active", auth.Status);
        Assert.AreEqual(1, auth.Items!.Value.GetArrayLength());
        Assert.AreEqual("J45.909", auth.Diagnoses!.Value[0].GetProperty("Code").GetString());

        var list = await _host.RunAsync<IRcmAdvancedAuthorizationService, RcmPagedResult<AdvancedAuthorizationDto>>(s =>
            s.ListAsync(new RcmQuery { ProviderId = seed.Provider.Id, Status = "Active" }));
        Assert.AreEqual(1, list.Total);

        var claim = await _host.SeedClaimAsync(seed, "req_aa_1");
        var used = await _host.RunAsync<IRcmAdvancedAuthorizationService, AdvancedAuthorizationDto>(s =>
            s.UseInClaimAsync(id, new UseAdvancedAuthorizationDto { ClaimId = claim.Id }, seed.Provider.Id));
        Assert.AreEqual("Used", used.Status);
        StringAssert.Contains((await _host.GetClaimAsync(claim.Id)).PreAuthorizationRef, "AA-12345");

        var other = await _host.SeedClaimAsync(seed, "req_aa_2");
        await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() => _host.RunAsync<IRcmAdvancedAuthorizationService, AdvancedAuthorizationDto>(s =>
            s.UseInClaimAsync(id, new UseAdvancedAuthorizationDto { ClaimId = other.Id }, seed.Provider.Id)));
    }

    [TestMethod]
    public async Task Evidence_pack_contains_exchanged_bundles_and_summary()
    {
        await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);
        _host = await RcmTestHost.CreateAsync(new Dictionary<string, string?> { ["Nphies:EnableMessageAudit"] = "true" });
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        _host.ReplyWith("7b - Claim Response.json");
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));
        await _host.QueryAsync(async db =>
        {
            db.NphiesMessageLogs.AddRange(_host.Audit.Logs);
            return await db.SaveChangesAsync();
        });

        var file = await _host.RunAsync<IRcmEvidenceService, RcmEvidenceFile>(s => s.BuildAsync(new[] { claim.Id }, seed.Provider.Id));
        using var zip = new ZipArchive(new MemoryStream(file.Content));
        var names = zip.Entries.Select(e => e.FullName).ToList();
        CollectionAssert.Contains(names, "README.md");
        Assert.IsTrue(names.Any(n => n.EndsWith("/timeline.json")));
        Assert.IsTrue(names.Any(n => n.EndsWith("-claim-request-request.json")), string.Join(", ", names));
        var response = zip.Entries.First(e => e.FullName.EndsWith("-claim-request-response.json"));
        using (var r = new StreamReader(response.Open())) StringAssert.Contains(r.ReadToEnd(), "claim-response");

        await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmEvidenceService, RcmEvidenceFile>(s => s.BuildAsync(new[] { claim.Id }, "another-provider")));
    }

    [TestMethod]
    public async Task Connectivity_report_flags_missing_certificate_and_unreachable_endpoint()
    {
        await using var host = await RcmTestHost.CreateAsync(new Dictionary<string, string?> { ["Nphies:BaseUrl"] = "https://localhost:1/" });
        var report = await host.RunAsync<IRcmConnectivityService, ConnectivityReportDto>(s => s.CheckAsync(includePoll: false));

        Assert.AreEqual(ConnectivityCheckDto.Fail, report.Status);
        Assert.AreEqual("https://localhost:1/$process-message", report.Endpoint);
        var byName = report.Checks.ToDictionary(c => c.Name);
        Assert.AreEqual(ConnectivityCheckDto.Warn, byName["configuration"].Status, "profile validation is not Enforce");
        Assert.AreEqual(ConnectivityCheckDto.Fail, byName["client-certificate"].Status);
        Assert.AreEqual(ConnectivityCheckDto.Pass, byName["dns"].Status);
        Assert.AreEqual(ConnectivityCheckDto.Fail, byName["tls-handshake"].Status);
        Assert.AreEqual(ConnectivityCheckDto.Skipped, byName["poll"].Status);
    }

    private static JsonObject Recon(JsonNode bundle) =>
        bundle["entry"]!.AsArray().Select(e => e!["resource"]!).First(r => (string?)r["resourceType"] == "PaymentReconciliation").AsObject();

    /// <summary>Synthetic advanced-authorization message derived from example 7b (structure per the IG profile).</summary>
    /// <summary>The same authorization sent again by the payer as a new message, with a reissue reason.</summary>
    private static string Reissue(string json, string reason, bool newResponse)
    {
        var b = JsonNode.Parse(json)!;
        b["id"] = Guid.NewGuid().ToString();
        var entries = b["entry"]!.AsArray();
        entries[0]!["resource"]!["id"] = Guid.NewGuid().ToString();
        var cr = entries.Select(e => e!["resource"]!).First(r => (string?)r["resourceType"] == "ClaimResponse").AsObject();
        if (newResponse)
        {
            cr["id"] = "AA-2";
            cr["identifier"]![0]!["value"] = "aa_resp_2";
        }
        cr["extension"]!.AsArray().Add(new JsonObject
        {
            ["url"] = Ext + "adjudication-reissue",
            ["valueCodeableConcept"] = new JsonObject { ["coding"] = new JsonArray(new JsonObject { ["system"] = "http://nphies.sa/terminology/CodeSystem/reissue-reason", ["code"] = reason }) },
        });
        return b.ToJsonString();
    }

    private async Task<string> IngestAsync(string json) =>
        XAssert.Single((await _host.RunAsync<IRcmInboundService, RcmInboundResultDto>(s => s.IngestRawAsync(json))).AdvancedAuthorizationIds);

    private Task<AdvancedAuthorizationDto?> AuthAsync(string id) =>
        _host.RunAsync<IRcmAdvancedAuthorizationService, AdvancedAuthorizationDto?>(s => s.GetAsync(id, null));

    [TestMethod]
    public async Task Payer_cancels_an_advanced_authorization_by_reissuing_it_with_reason_cancel()
    {
        await _host.SeedPartiesAsync();
        var json = BuildAdvancedAuthorization();
        var id = await IngestAsync(json);

        // Same preAuthRef, sent as a new response.
        var noticeId = await IngestAsync(Reissue(json, "cancel", newResponse: true));

        Assert.AreEqual("Cancelled", (await AuthAsync(id))!.Status, "The authorization we held is withdrawn.");
        Assert.AreEqual("cancel", (await AuthAsync(id))!.ReissueReason);
        Assert.AreEqual("Cancelled", (await AuthAsync(noticeId))!.Status, "The cancellation notice is not a usable authorization.");
        var claim = await _host.SeedClaimAsync(await _host.QueryAsync(async db => new RcmTestHost.Seed
        {
            Provider = await db.Organizations.SingleAsync(o => o.OrganizationType == "prov"),
            Insurer = await db.Organizations.SingleAsync(o => o.OrganizationType == "ins"),
            Patient = await db.Patients.SingleAsync(),
            Coverage = await db.Coverages.SingleAsync(),
            Practitioner = await db.Practitioners.SingleAsync(),
        }), "req_aa_cancelled");
        await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() => _host.RunAsync<IRcmAdvancedAuthorizationService, AdvancedAuthorizationDto>(s =>
            s.UseInClaimAsync(id, new UseAdvancedAuthorizationDto { ClaimId = claim.Id }, null)), "A cancelled authorization cannot be billed.");
    }

    [TestMethod]
    public async Task A_reissue_with_another_reason_replaces_the_authorization()
    {
        await _host.SeedPartiesAsync();
        var json = BuildAdvancedAuthorization();
        var id = await IngestAsync(json);

        // The same response sent again with a correction changes it in place.
        var sameId = await IngestAsync(Reissue(json, "correction", newResponse: false));
        Assert.AreEqual(id, sameId);
        Assert.AreEqual("Active", (await AuthAsync(id))!.Status);
        Assert.AreEqual("correction", (await AuthAsync(id))!.ReissueReason);
        Assert.AreEqual(1, await _host.QueryAsync(db => db.AdvancedAuthorizations.CountAsync()));

        // A new response with the same preAuthRef takes its place.
        var newId = await IngestAsync(Reissue(json, "adjudication", newResponse: true));
        Assert.AreEqual("Superseded", (await AuthAsync(id))!.Status);
        Assert.AreEqual("Active", (await AuthAsync(newId))!.Status);
    }

    [TestMethod]
    public async Task Provider_cancels_an_advanced_authorization_with_a_cancel_request()
    {
        await _host.SeedPartiesAsync();
        var id = await IngestAsync(BuildAdvancedAuthorization());
        _host.ReplyWith("17b - Cancel Response (Do not process).json");

        var result = await _host.RunAsync<IRcmAdvancedAuthorizationService, TaskResultDto>(s => s.CancelAsync(id, new CancelClaimRequestDto { Reason = "NP" }, null));

        CollectionAssert.AreEqual(Array.Empty<string>(), result.Errors);
        Assert.AreEqual("cancel-request", _host.Transport.EventCodes.Last());
        StringAssert.Contains(_host.Transport.Requests.Last().Json, "aa_resp_1", "The Task focus is the authorization's identifier.");
        Assert.AreEqual("Cancelled", (await AuthAsync(id))!.Status);
        await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmAdvancedAuthorizationService, TaskResultDto>(s => s.CancelAsync(id, new CancelClaimRequestDto { Reason = "NP" }, null)), "Already cancelled.");
    }

    [TestMethod]
    public async Task Amending_an_advanced_authorization_starts_a_prior_authorization_with_all_items_and_its_preauth_ref()
    {
        var seed = await _host.SeedPartiesAsync();
        var id = await IngestAsync(BuildAdvancedAuthorization());

        var draft = await _host.RunAsync<IRcmAdvancedAuthorizationService, SubmissionResultDto>(s => s.AmendAsync(id, null));

        Assert.AreEqual(0, _host.Transport.Requests.Count, "The draft is not sent.");
        var claim = await _host.QueryAsync(db => db.Claims.AsNoTracking().Include(c => c.Items).Include(c => c.Diagnoses).SingleAsync(c => c.Id == draft.ClaimId));
        Assert.AreEqual("preauthorization", claim.Use);
        Assert.AreEqual("AA-12345", claim.PreAuthorizationRef, "The same preAuthRef makes the payer treat it as a change to the authorization.");
        Assert.AreEqual(RcmClaimStatus.Draft, claim.NphiesStatus);
        Assert.AreEqual(seed.Patient.Id, claim.PatientId);
        Assert.AreEqual(seed.Coverage.Id, claim.CoverageId);
        Assert.AreEqual((await AuthAsync(id))!.Items!.Value.GetArrayLength(), claim.Items.Count, "Every item is carried over.");
        Assert.AreEqual("J45.909", claim.Diagnoses.First().DiagnosisCode);
    }

    [TestMethod]
    public async Task A_communication_can_ask_the_payer_to_change_an_advanced_authorization()
    {
        await _host.SeedPartiesAsync();
        var id = await IngestAsync(BuildAdvancedAuthorization());
        _host.ReplyWith(RcmFixtures.Acknowledgement);

        var sent = await _host.RunAsync<IRcmCommunicationService, CommunicationDto>(s => s.SendAsync(new SendCommunicationDto
        {
            AdvancedAuthorizationId = id,
            Texts = { "Please extend the authorization period by 30 days." },
        }));

        CollectionAssert.AreEqual(Array.Empty<string>(), sent.Errors);
        Assert.AreEqual("communication", _host.Transport.EventCodes.Last());
        var about = JsonNode.Parse(_host.Transport.Requests.Last().Json)!["entry"]!.AsArray()
            .Select(e => e!["resource"]!).First(r => (string?)r["resourceType"] == "Communication")["about"]![0]!;
        Assert.AreEqual("ClaimResponse", (string?)about["type"], "The communication is about the authorization, not a claim.");
        Assert.AreEqual("aa_resp_1", (string?)about["identifier"]!["value"]);
        Assert.IsNull(sent.ClaimId);

        await _host.QueryAsync(async db =>
        {
            (await db.AdvancedAuthorizations.SingleAsync(a => a.Id == id)).Status = "Cancelled";
            return await db.SaveChangesAsync();
        });
        await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() => _host.RunAsync<IRcmCommunicationService, CommunicationDto>(s =>
            s.SendAsync(new SendCommunicationDto { AdvancedAuthorizationId = id, Texts = { "Again" } })), "Nothing to change on a cancelled authorization.");
    }

    private static string BuildAdvancedAuthorization()
    {
        var b = JsonNode.Parse(DocsExamples.ReadJson("7b - Claim Response.json"))!;
        b["id"] = Guid.NewGuid().ToString();
        var entries = b["entry"]!.AsArray();
        var header = entries[0]!["resource"]!;
        header["id"] = Guid.NewGuid().ToString();
        header["eventCoding"]!["code"] = "advanced-authorization";
        header.AsObject().Remove("response");

        string? providerUrl = null;
        foreach (var e in entries)
        {
            var r = e!["resource"]!;
            switch ((string?)r["resourceType"])
            {
                case "Organization":
                    var lic = r["identifier"]![0]!;
                    if (((string?)lic["system"])!.Contains("provider")) { lic["value"] = RcmTestHost.ProviderLicense; providerUrl = (string?)e["fullUrl"]; }
                    else lic["value"] = RcmTestHost.PayerLicense;
                    break;
                case "Patient":
                    r["identifier"]![0]!["value"] = "2234567890";
                    break;
            }
        }

        var cr = entries.Select(e => e!["resource"]!).First(r => (string?)r["resourceType"] == "ClaimResponse").AsObject();
        cr["id"] = "AA-1";
        cr["identifier"]![0]!["value"] = "aa_resp_1";
        cr.Remove("request");
        cr.Remove("requestor");
        cr.Remove("item");
        cr["preAuthRef"] = "AA-12345";
        cr["preAuthPeriod"] = new JsonObject { ["start"] = DateTime.UtcNow.ToString("yyyy-MM-dd"), ["end"] = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd") };
        cr["subType"] = new JsonObject { ["coding"] = new JsonArray(new JsonObject { ["system"] = "http://nphies.sa/terminology/CodeSystem/claim-subtype", ["code"] = "op" }) };
        cr["extension"] = new JsonArray(
            new JsonObject
            {
                ["url"] = Ext + "advancedAuth-reason",
                ["valueCodeableConcept"] = new JsonObject { ["coding"] = new JsonArray(new JsonObject { ["system"] = "http://nphies.sa/terminology/CodeSystem/advancedAuth-reason", ["code"] = "referral" }) },
            },
            new JsonObject { ["url"] = Ext + "serviceProvider", ["valueReference"] = new JsonObject { ["reference"] = providerUrl } },
            new JsonObject
            {
                ["url"] = Ext + "diagnosis",
                ["extension"] = new JsonArray(new JsonObject
                {
                    ["url"] = "diagnosisCode",
                    ["valueCodeableConcept"] = new JsonObject { ["coding"] = new JsonArray(new JsonObject { ["system"] = "http://hl7.org/fhir/sid/icd-10-am", ["code"] = "J45.909" }) },
                }),
            });
        cr["addItem"] = new JsonArray(new JsonObject
        {
            ["extension"] = new JsonArray(
                new JsonObject { ["url"] = Ext + "sequence", ["valuePositiveInt"] = 1 },
                new JsonObject
                {
                    ["url"] = Ext + "adjudication-outcome",
                    ["valueCodeableConcept"] = new JsonObject { ["coding"] = new JsonArray(new JsonObject { ["system"] = "http://nphies.sa/terminology/CodeSystem/adjudication-outcome", ["code"] = "approved" }) },
                }),
            ["productOrService"] = new JsonObject { ["coding"] = new JsonArray(new JsonObject { ["system"] = "http://nphies.sa/terminology/CodeSystem/services", ["code"] = "83600-00-10" }) },
            ["quantity"] = new JsonObject { ["value"] = 1 },
            ["adjudication"] = new JsonArray(new JsonObject
            {
                ["category"] = new JsonObject { ["coding"] = new JsonArray(new JsonObject { ["system"] = "http://terminology.hl7.org/CodeSystem/adjudication", ["code"] = "benefit" }) },
                ["amount"] = new JsonObject { ["value"] = 100, ["currency"] = "SAR" },
            }),
        });
        return b.ToJsonString();
    }
}
