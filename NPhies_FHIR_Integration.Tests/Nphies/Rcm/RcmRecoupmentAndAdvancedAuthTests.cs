using System.IO.Compression;
using System.Text.Json.Nodes;
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
    public async Task Cleanup() => await _host.DisposeAsync();

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
        await _host.DisposeAsync();
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
