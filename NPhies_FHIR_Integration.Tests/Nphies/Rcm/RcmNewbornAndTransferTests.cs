using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.Extensions.DependencyInjection;
using NPhies_FHIR_Integration.Application.Nphies.Builders;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Validation;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using PatientEntity = NPhies_FHIR_Integration.Domain.Entities.Patient;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

/// <summary>
/// IG 6.5 (newborn on the mother's coverage) and 7.3 (transfer flag on eligibility). Certification test cases
/// Eligibility #2 and #3, Authorization #8 and Claims #6 need these.
/// </summary>
[TestClass]
public class RcmNewbornAndTransferTests
{
    private RcmTestHost _host = null!;

    [TestInitialize]
    public async Task Init() => _host = await RcmTestHost.CreateAsync();

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    [TestMethod]
    public async Task Newborn_eligibility_uses_the_mothers_coverage_and_sends_the_newborn_extension()
    {
        var seed = await _host.SeedPartiesAsync();
        var newborn = await SeedNewbornAsync();
        _host.ReplyWith("14b - Eligibility Response Ben-Val.json");

        var result = await _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.CheckAsync(new EligibilityCheckRequestDto
        {
            PatientId = newborn.Id,
            CoverageId = seed.Coverage.Id, // the mother's coverage
            ProviderId = seed.Provider.Id,
            Purposes = { "validation", "benefits" },
            IsNewborn = true,
        }));

        Assert.IsTrue(result.IsNewborn);
        var bundle = SentBundle(0, "eligibility-request");
        var request = bundle.Entry.Select(e => e.Resource).OfType<CoverageEligibilityRequest>().Single();
        Assert.AreEqual(true, (request.Extension.SingleOrDefault(e => e.Url == NphiesExtensions.Newborn)?.Value as FhirBoolean)?.Value);

        var patients = bundle.Entry.Where(e => e.Resource is Patient).ToList();
        Assert.AreEqual(2, patients.Count, "Newborn and mother are both sent.");
        var newbornEntry = patients.Single(e => e.FullUrl == request.Patient.Reference);
        StringAssert.Contains(((Patient)newbornEntry.Resource).Identifier.Single().Value, "MRN-NB-0001");

        var coverage = bundle.Entry.Select(e => e.Resource).OfType<Coverage>().Single();
        Assert.AreEqual(request.Patient.Reference, coverage.Beneficiary.Reference, "The newborn is the beneficiary.");
        var mother = (Patient)patients.Single(e => e.FullUrl == coverage.Subscriber.Reference).Resource;
        Assert.AreEqual("2234567890", mother.Identifier.Single().Value, "The mother is the subscriber.");
        Assert.AreEqual("child", coverage.Relationship.Coding.Single().Code);

        var errors = new NphiesProfileValidator(NphiesConformancePackage.Embedded).Validate(bundle).Errors.ToList();
        Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors.Select(e => $"{e.RuleId} {e.Expression}: {e.Message}")));
    }

    [TestMethod]
    public async Task Another_patients_coverage_is_rejected_without_the_newborn_flag()
    {
        var seed = await _host.SeedPartiesAsync();
        var newborn = await SeedNewbornAsync();

        var ex = await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.CheckAsync(new EligibilityCheckRequestDto
            {
                PatientId = newborn.Id,
                CoverageId = seed.Coverage.Id,
                ProviderId = seed.Provider.Id,
                Purposes = { "validation" },
            })));
        StringAssert.Contains(ex.Message, "isNewborn");
        Assert.AreEqual(0, _host.Transport.Requests.Count);
    }

    [TestMethod]
    public async Task Transfer_flag_is_sent_on_the_eligibility_request()
    {
        var seed = await _host.SeedPartiesAsync();
        _host.ReplyWith("14b - Eligibility Response Ben-Val.json");

        var result = await _host.RunAsync<IRcmEligibilityService, EligibilityCheckDto>(s => s.CheckAsync(new EligibilityCheckRequestDto
        {
            PatientId = seed.Patient.Id,
            CoverageId = seed.Coverage.Id,
            ProviderId = seed.Provider.Id,
            Purposes = { "benefits" },
            IsTransfer = true,
        }));

        Assert.IsTrue(result.IsTransfer);
        Assert.IsFalse(result.IsNewborn);
        var request = SentBundle(0, "eligibility-request").Entry.Select(e => e.Resource).OfType<CoverageEligibilityRequest>().Single();
        Assert.AreEqual(true, (request.Extension.SingleOrDefault(e => e.Url == NphiesExtensions.Transfer)?.Value as FhirBoolean)?.Value);
        Assert.IsFalse(request.Extension.Any(e => e.Url == NphiesExtensions.Newborn));
    }

    [TestMethod]
    public async Task Newborn_claim_on_the_mothers_coverage_passes_the_editor_and_conforms_to_the_ig()
    {
        var seed = await _host.SeedPartiesAsync();
        var newborn = await SeedNewbornAsync();
        var claim = await _host.SeedClaimAsync(seed, "req_newborn");
        await _host.QueryAsync(async db =>
        {
            var c = db.Claims.Single(x => x.Id == claim.Id);
            c.PatientId = newborn.Id;
            c.IsNewborn = true;
            // The shared fixture claim has no chief complaint, which the oral profile requires; add one so the
            // strict IG check below only reports problems introduced by the newborn mapping.
            db.Add(new NPhies_FHIR_Integration.Domain.Entities.ClaimSupportingInfo
            {
                ClaimId = c.Id,
                Sequence = 1,
                Category = "chief-complaint",
                CategorySystem = "http://nphies.sa/terminology/CodeSystem/claim-information-category",
                CodeValue = "K02.1",
                CodeSystem = "http://hl7.org/fhir/sid/icd-10-am",
                StringValue = "Toothache",
            });
            await db.SaveChangesAsync();
            return 0;
        });

        var health = await _host.RunAsync<IRcmClaimEditorService, ClaimHealthDto>(s => s.CheckHealthAsync(claim.Id, null));
        Assert.IsFalse(health.Issues.Any(i => i.Code == "COVERAGE_PATIENT"), string.Join(", ", health.Issues.Select(i => i.Code)));
        Assert.AreEqual("Warning", health.Issues.Single(i => i.Code == "NEWBORN_BIRTH_WEIGHT").Severity);

        using var scope = _host.Services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IRcmInputMapper>();
        var builder = scope.ServiceProvider.GetRequiredService<INphiesBundleBuilder>();
        var bundle = builder.BuildClaimRequest(await mapper.MapClaimAsync((await mapper.LoadClaimGraphAsync(claim.Id))!));

        var sent = bundle.Entry.Select(e => e.Resource).OfType<Claim>().Single();
        Assert.AreEqual(true, (sent.Extension.SingleOrDefault(e => e.Url == NphiesExtensions.Newborn)?.Value as FhirBoolean)?.Value);
        var coverage = bundle.Entry.Select(e => e.Resource).OfType<Coverage>().Single();
        Assert.AreEqual(sent.Patient.Reference, coverage.Beneficiary.Reference);
        Assert.AreNotEqual(coverage.Beneficiary.Reference, coverage.Subscriber.Reference);
        Assert.AreEqual(2, bundle.Entry.Count(e => e.Resource is Patient));

        var errors = new NphiesProfileValidator(NphiesConformancePackage.Embedded).Validate(bundle).Errors.ToList();
        Assert.AreEqual(0, errors.Count, string.Join(Environment.NewLine, errors.Select(e => $"{e.RuleId} {e.Expression}: {e.Message}")));
    }

    /// <summary>A newborn identified by MRN only (no national ID yet), as in the certification test cases.</summary>
    private async Task<PatientEntity> SeedNewbornAsync()
    {
        var newborn = new PatientEntity
        {
            MRN = "MRN-NB-0001",
            NationalIdType = "MRN",
            FirstName = "Baby",
            LastName = "Khaled",
            FullName = "Baby Khaled",
            Gender = "F",
            DateOfBirth = DateTime.UtcNow.Date.AddDays(-10),
        };
        await _host.QueryAsync(async db =>
        {
            db.Patients.Add(newborn);
            await db.SaveChangesAsync();
            return 0;
        });
        return newborn;
    }

    private Bundle SentBundle(int index, string eventCode)
    {
        var request = _host.Transport.Requests[index];
        Assert.AreEqual(eventCode, request.EventCode);
#pragma warning disable CS0618 // relaxed parsing, as in DocsExamples
        return new FhirJsonParser(new ParserSettings { AcceptUnknownMembers = true, AllowUnrecognizedEnums = true, PermissiveParsing = true })
            .Parse<Bundle>(request.Json);
#pragma warning restore CS0618
    }
}
