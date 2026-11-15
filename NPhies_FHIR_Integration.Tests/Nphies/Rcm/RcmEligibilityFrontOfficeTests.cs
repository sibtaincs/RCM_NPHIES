using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using PatientEntity = NPhies_FHIR_Integration.Domain.Entities.Patient;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

/// <summary>
/// Phase 3 front-office rules: site eligibility in plain language, advice for payer error codes, the 90-day newborn
/// window, eligibility recorded offline and carried on claims, and the warning before billing a patient who is not eligible.
/// </summary>
[TestClass]
public class RcmEligibilityFrontOfficeTests
{
    private RcmTestHost _host = null!;

    [TestInitialize]
    public async Task Init() => _host = await RcmTestHost.CreateAsync();

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    private Task<EligibilityCheckDto> CheckAsync(EligibilityCheckRequestDto request) =>
        _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.CheckAsync(request));

    private Task<EligibilityCheckDto> DiscoveryAsync(RcmTestHost.Seed seed) => CheckAsync(new EligibilityCheckRequestDto
    {
        PatientId = seed.Patient.Id,
        ProviderId = seed.Provider.Id,
        InsurerId = seed.Insurer.Id,
        Purposes = { "discovery" },
    });

    [TestMethod]
    public async Task Site_eligibility_is_explained_in_plain_language()
    {
        var seed = await _host.SeedPartiesAsync();
        _host.ReplyWith("15b - Eligibility Response Discovery - Outcome_Complete_out-network.json");

        var result = await DiscoveryAsync(seed);

        Assert.AreEqual("out-network", result.SiteEligibility);
        Assert.AreEqual(false, result.IsEligible);
        StringAssert.Contains(result.SiteEligibilityReason, "outside the policy's network");
    }

    [TestMethod]
    public void Every_site_eligibility_code_has_a_reason()
    {
        foreach (var code in new[] { "eligible", "not-active", "not-covered", "not-direct-billing", "out-network", "limit-exhausted", "coverage-suspended", "provider-contract-suspended" })
            Assert.IsNotNull(RcmEligibilityGuidance.SiteEligibilityReason(code), code);
        Assert.IsNull(RcmEligibilityGuidance.SiteEligibilityReason("something-new"));
        foreach (var code in new[] { "1658", "1659", "1660", "1665", "1666", "1680", "1681", "1682" })
            Assert.IsNotNull(RcmEligibilityGuidance.PayerErrorExplanation(code), code);
    }

    [TestMethod]
    public async Task Payer_error_codes_come_with_advice()
    {
        var seed = await _host.SeedPartiesAsync();
        _host.ReplyWith("15c - Eligibility Response Discovery - Outcome_Error.json");

        var result = await DiscoveryAsync(seed);

        Assert.AreEqual("Error", result.Status);
        var error = result.ErrorExplanations.Single(e => e.Code == "1658");
        StringAssert.Contains(error.Explanation, "no member");
        var stored = await _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto?>(s => s.GetAsync(result.Id));
        Assert.IsTrue(stored!.ErrorExplanations.Any(e => e.Code == "1658" && e.Explanation != null), "The advice is also given when the check is read back.");
    }

    [TestMethod]
    public async Task Newborn_older_than_90_days_cannot_use_the_mothers_coverage()
    {
        var seed = await _host.SeedPartiesAsync();
        var newborn = new PatientEntity
        {
            MRN = "MRN-NB-0002", NationalIdType = "MRN", FirstName = "Baby", LastName = "Khaled", FullName = "Baby Khaled", Gender = "M",
            DateOfBirth = DateTime.UtcNow.Date.AddDays(-91),
        };
        await _host.QueryAsync(async db => { db.Patients.Add(newborn); return await db.SaveChangesAsync(); });
        _host.ReplyWith("14b - Eligibility Response Ben-Val.json");
        EligibilityCheckRequestDto Request(DateTime? serviced) => new()
        {
            PatientId = newborn.Id, CoverageId = seed.Coverage.Id, ProviderId = seed.Provider.Id, IsNewborn = true, ServicedDate = serviced,
        };

        var ex = await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() => CheckAsync(Request(null)));
        StringAssert.Contains(ex.Message, "90 days");
        Assert.AreEqual(0, _host.Transport.Requests.Count, "Nothing is sent.");

        // A service on day 90 is still inside the window.
        var ok = await CheckAsync(Request(newborn.DateOfBirth.AddDays(90)));
        Assert.IsTrue(ok.IsNewborn);
        Assert.AreEqual(1, _host.Transport.Requests.Count);
    }

    [TestMethod]
    public async Task Offline_eligibility_is_recorded_and_sent_on_the_claim()
    {
        var seed = await _host.SeedPartiesAsync();
        var confirmedAt = DateTime.UtcNow.Date.AddDays(-1).AddHours(10);

        var offline = await _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.RecordOfflineAsync(new OfflineEligibilityRequestDto
        {
            PatientId = seed.Patient.Id, ProviderId = seed.Provider.Id, Reference = "PORTAL-778899", Date = confirmedAt, Notes = "Confirmed on the payer portal.",
        }));

        Assert.IsTrue(offline.IsOffline);
        Assert.AreEqual("PORTAL-778899", offline.OfflineReference);
        Assert.AreEqual(seed.Insurer.Id, offline.InsurerId, "The insurer comes from the patient's active coverage.");
        Assert.AreEqual(true, offline.IsEligible);
        Assert.AreEqual(0, _host.Transport.Requests.Count, "An offline record sends nothing to NPHIES.");
        var latest = await _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto?>(s => s.GetLatestForPatientAsync(seed.Patient.Id, null));
        Assert.AreEqual(offline.Id, latest!.Id);

        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        await _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.ApplyOfflineToClaimAsync(offline.Id, claim.Id, null));
        _host.ReplyWith("7b - Claim Response.json");
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));

        var sent = new FhirJsonParser().Parse<Bundle>(_host.Transport.Requests.Single().Json);
        var fhirClaim = sent.Entry.Select(e => e.Resource).OfType<Claim>().Single();
        Assert.AreEqual("PORTAL-778899", (fhirClaim.GetExtension(NphiesExtensions.EligibilityOfflineReference)?.Value as FhirString)?.Value);
        Assert.IsNotNull(fhirClaim.GetExtension(NphiesExtensions.EligibilityOfflineDate)?.Value as FhirDateTime);

        // A sent claim can no longer be changed.
        var ex = await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.ApplyOfflineToClaimAsync(offline.Id, claim.Id, null)));
        Assert.AreEqual(RcmErrorKind.Conflict, ex.Kind);
    }

    [TestMethod]
    public async Task Offline_eligibility_needs_a_reference_and_a_past_date()
    {
        var seed = await _host.SeedPartiesAsync();
        Task<EligibilityCheckDto> Record(string reference, DateTime date) => _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s =>
            s.RecordOfflineAsync(new OfflineEligibilityRequestDto { PatientId = seed.Patient.Id, ProviderId = seed.Provider.Id, Reference = reference, Date = date }));

        await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() => Record(" ", DateTime.UtcNow));
        await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() => Record("REF-1", DateTime.UtcNow.AddDays(3)));
        Assert.AreEqual(0, await _host.QueryAsync(db => db.EligibilityChecks.CountAsync()));
    }

    [TestMethod]
    public async Task Only_an_offline_record_for_the_same_patient_can_be_put_on_a_claim()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        _host.ReplyWith("14b - Eligibility Response Ben-Val.json");
        var online = await CheckAsync(new EligibilityCheckRequestDto { PatientId = seed.Patient.Id, CoverageId = seed.Coverage.Id, ProviderId = seed.Provider.Id });

        var ex = await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.ApplyOfflineToClaimAsync(online.Id, claim.Id, null)));
        Assert.AreEqual(RcmErrorKind.Conflict, ex.Kind);

        var other = await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.ApplyOfflineToClaimAsync(online.Id, claim.Id, "another-hospital")));
        Assert.AreEqual(RcmErrorKind.NotFound, other.Kind, "Another hospital cannot see the record.");
    }

    [TestMethod]
    public async Task Claim_health_warns_when_the_last_check_said_not_eligible()
    {
        var seed = await _host.SeedPartiesAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_299060");
        Task<ClaimHealthDto> HealthAsync() => _host.RunAsync<IRcmClaimEditorService, ClaimHealthDto>(s => s.CheckHealthAsync(claim.Id, null));

        Assert.IsFalse((await HealthAsync()).Issues.Any(i => i.Code == "ELIGIBILITY_NOT_ELIGIBLE"), "No check yet, no warning.");

        _host.ReplyWith("15b - Eligibility Response Discovery - Outcome_Complete_out-network.json");
        await DiscoveryAsync(seed);
        var warning = (await HealthAsync()).Issues.Single(i => i.Code == "ELIGIBILITY_NOT_ELIGIBLE");
        Assert.AreEqual("Warning", warning.Severity);
        StringAssert.Contains(warning.Message, "out-network");

        // A later check that confirms eligibility clears the warning.
        await _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.RecordOfflineAsync(new OfflineEligibilityRequestDto
        {
            PatientId = seed.Patient.Id, ProviderId = seed.Provider.Id, Reference = "CALL-1", Date = DateTime.UtcNow,
        }));
        Assert.IsFalse((await HealthAsync()).Issues.Any(i => i.Code == "ELIGIBILITY_NOT_ELIGIBLE"));
    }
}
