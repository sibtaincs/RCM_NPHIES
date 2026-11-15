using System.Text.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Sandbox;
using NPhies_FHIR_Integration.Application.Nphies.Validation;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using PatientEntity = NPhies_FHIR_Integration.Domain.Entities.Patient;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

/// <summary>
/// The five NPHIES onboarding test cases for eligibility (Eligibility Test cases V4.0, against the pseudo-payer
/// INS-FHIR), run end to end against the sandbox simulator.
/// </summary>
[TestClass]
public class EligibilityOnboardingCasesTests
{
    private RcmTestHost _host = null!;
    private RcmTestHost.Seed _seed = null!;

    [TestInitialize]
    public async Task Init()
    {
        _host = await RcmTestHost.CreateAsync();
        var sandbox = _host.Services.GetRequiredService<NphiesSandboxTransport>();
        _host.ReplyWith(r => sandbox.SendAsync(r).GetAwaiter().GetResult().Body);
        _seed = await _host.SeedPartiesAsync();
    }

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    private Task SetIqamaAsync(string iqama) => _host.QueryAsync(async db =>
    {
        (await db.Patients.SingleAsync(p => p.Id == _seed.Patient.Id)).NationalId = iqama;
        return await db.SaveChangesAsync();
    });

    private Task<EligibilityCheckDto> CheckAsync(EligibilityCheckRequestDto request) =>
        _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.CheckAsync(request));

    private Bundle Sent()
    {
        var bundle = new FhirJsonParser().Parse<Bundle>(_host.Transport.Requests.Single().Json);
        var errors = new NphiesProfileValidator(NphiesConformancePackage.Embedded).Validate(bundle).Errors.ToList();
        Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors.Select(e => $"{e.RuleId} {e.Expression}: {e.Message}")));
        return bundle;
    }

    [TestMethod]
    public async Task Case_1_validation_with_a_member_id_is_eligible_and_in_force()
    {
        await SetIqamaAsync(NphiesSandboxTransport.ActiveMemberId);

        var result = await CheckAsync(new EligibilityCheckRequestDto
        {
            PatientId = _seed.Patient.Id, CoverageId = _seed.Coverage.Id, ProviderId = _seed.Provider.Id, Purposes = { "validation" }, IsTransfer = true,
        });

        Assert.AreEqual("Completed", result.Status);
        Assert.AreEqual("eligible", result.SiteEligibility);
        Assert.AreEqual(true, result.InForce);
        Assert.AreEqual(true, result.IsEligible);
        var bundle = Sent();
        var coverage = bundle.Entry.Select(e => e.Resource).OfType<Coverage>().Single();
        Assert.AreEqual("0000000001", coverage.Identifier.Single().Value, "The member id is sent on the coverage.");
        var request = bundle.Entry.Select(e => e.Resource).OfType<CoverageEligibilityRequest>().Single();
        Assert.AreEqual(true, (request.GetExtension(NphiesExtensions.Transfer)?.Value as FhirBoolean)?.Value, "The transfer extension is included.");
    }

    [TestMethod]
    public async Task Case_2_discovery_and_benefits_without_a_coverage_returns_multiple_active_policies()
    {
        await SetIqamaAsync(NphiesSandboxTransport.ActiveMemberId);

        var result = await CheckAsync(new EligibilityCheckRequestDto
        {
            PatientId = _seed.Patient.Id, ProviderId = _seed.Provider.Id, InsurerId = _seed.Insurer.Id, Purposes = { "discovery", "benefits" },
        });

        Assert.AreEqual("Completed", result.Status, JsonSerializer.Serialize(result.Errors));
        Assert.AreEqual("eligible", result.SiteEligibility);
        Assert.AreEqual(true, result.InForce);
        var discovered = JsonSerializer.SerializeToElement(result.DiscoveredCoverages);
        Assert.AreEqual(2, discovered.GetArrayLength(), "Both active policies are returned.");
        Assert.AreEqual(2, JsonSerializer.SerializeToElement(result.Benefits).GetArrayLength(), "One insurance entry per policy.");
        Assert.IsFalse(Sent().Entry.Any(e => e.Resource is Coverage), "Discovery is sent without a coverage resource.");
    }

    [TestMethod]
    public async Task Case_3_newborn_by_mrn_with_the_mothers_coverage_is_eligible()
    {
        await SetIqamaAsync(NphiesSandboxTransport.ActiveMemberId); // the mother
        var newborn = new PatientEntity
        {
            MRN = "MRN-NB-0003", NationalIdType = "MRN", FirstName = "Baby", LastName = "Khaled", FullName = "Baby Khaled", Gender = "F",
            DateOfBirth = DateTime.UtcNow.Date.AddDays(-5),
        };
        await _host.QueryAsync(async db => { db.Patients.Add(newborn); return await db.SaveChangesAsync(); });

        var result = await CheckAsync(new EligibilityCheckRequestDto
        {
            PatientId = newborn.Id, CoverageId = _seed.Coverage.Id, ProviderId = _seed.Provider.Id, Purposes = { "validation", "benefits" }, IsNewborn = true,
        });

        Assert.AreEqual("eligible", result.SiteEligibility);
        Assert.AreEqual(true, result.InForce);
        var bundle = Sent();
        var request = bundle.Entry.Select(e => e.Resource).OfType<CoverageEligibilityRequest>().Single();
        Assert.AreEqual(true, (request.GetExtension(NphiesExtensions.Newborn)?.Value as FhirBoolean)?.Value);
        var patients = bundle.Entry.Select(e => e.Resource).OfType<Patient>().ToList();
        Assert.AreEqual(2, patients.Count, "The mother and the newborn are both sent.");
        Assert.IsTrue(patients.Any(p => p.Identifier.Any(i => i.Value == NphiesSandboxTransport.ActiveMemberId)), "The mother carries her Iqama number.");
        Assert.IsTrue(patients.Any(p => p.Identifier.Any(i => i.Value!.Contains("MRN-NB-0003"))), "The newborn carries an MRN.");
    }

    [TestMethod]
    public async Task Case_4_an_inactive_member_is_not_active_and_not_in_force()
    {
        await SetIqamaAsync(NphiesSandboxTransport.InactiveMemberId);

        var result = await CheckAsync(new EligibilityCheckRequestDto
        {
            PatientId = _seed.Patient.Id, CoverageId = _seed.Coverage.Id, ProviderId = _seed.Provider.Id, Purposes = { "validation", "benefits" },
        });

        Assert.AreEqual("Completed", result.Status);
        Assert.AreEqual("not-active", result.SiteEligibility);
        Assert.AreEqual(false, result.InForce);
        Assert.AreEqual(false, result.IsEligible);
        StringAssert.Contains(result.SiteEligibilityReason, "not active");
    }

    [TestMethod]
    public async Task Case_5_an_unknown_member_gets_outcome_error_with_code_1658()
    {
        await SetIqamaAsync(NphiesSandboxTransport.UnknownMemberId);

        var result = await CheckAsync(new EligibilityCheckRequestDto
        {
            PatientId = _seed.Patient.Id, CoverageId = _seed.Coverage.Id, ProviderId = _seed.Provider.Id, Purposes = { "validation" },
        });

        Assert.AreEqual("Error", result.Status);
        Assert.AreEqual("error", result.Outcome);
        var error = result.ErrorExplanations.Single(e => e.Code == "1658");
        Assert.IsNotNull(error.Explanation, "The front desk is told what to do about it.");
    }
}
