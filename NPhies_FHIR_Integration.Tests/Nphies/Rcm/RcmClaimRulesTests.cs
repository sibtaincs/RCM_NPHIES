using Hl7.Fhir.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NPhies_FHIR_Integration.Application.Nphies.Builders;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;
using ClaimEntity = NPhies_FHIR_Integration.Domain.Entities.Claim;
using ClaimItemEntity = NPhies_FHIR_Integration.Domain.Entities.ClaimItem;
using ClaimDiagnosisEntity = NPhies_FHIR_Integration.Domain.Entities.ClaimDiagnosis;
using ClaimSupportingInfoEntity = NPhies_FHIR_Integration.Domain.Entities.ClaimSupportingInfo;
using PatientEntity = NPhies_FHIR_Integration.Domain.Entities.Patient;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

/// <summary>
/// Phase 5 claim rules checked before sending (A3): accidents, newborns, vital signs not measured, pharmacy
/// days-supply, large claims and duplicates; and "Add services" in the same episode.
/// </summary>
[TestClass]
public class RcmClaimRulesTests
{
    private const string InfoCategories = "http://nphies.sa/terminology/CodeSystem/claim-information-category";
    private RcmTestHost _host = null!;
    private RcmTestHost.Seed _seed = null!;

    [TestInitialize]
    public async Task Init()
    {
        _host = await RcmTestHost.CreateAsync();
        _seed = await _host.SeedPartiesAsync();
    }

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    private Task ChangeAsync(string claimId, Action<ApplicationDbContext, ClaimEntity> change) => _host.QueryAsync(async db =>
    {
        var claim = await db.Claims.Include(c => c.Items).Include(c => c.SupportingInfo).Include(c => c.Diagnoses).SingleAsync(c => c.Id == claimId);
        change(db, claim);
        return await db.SaveChangesAsync();
    });

    private async Task<Dictionary<string, string>> IssuesAsync(string claimId) =>
        (await _host.RunAsync<IRcmClaimEditorService, ClaimHealthDto>(s => s.CheckHealthAsync(claimId, null)))
        .Issues.GroupBy(i => i.Code).ToDictionary(g => g.Key, g => g.First().Severity);

    private static ClaimDiagnosisEntity Diagnosis(ClaimEntity c, string code, string type = "secondary") => new()
    {
        ClaimId = c.Id, Sequence = c.Diagnoses.Max(d => d.Sequence) + 1, DiagnosisCode = code, DiagnosisType = type,
        DiagnosisSystem = c.Diagnoses.First().DiagnosisSystem, DiagnosisTypeSystem = c.Diagnoses.First().DiagnosisTypeSystem,
    };

    [TestMethod]
    public async Task Work_related_and_road_traffic_diagnoses_need_the_matching_accident_type()
    {
        var claim = await _host.SeedClaimAsync(_seed, "req_accident");
        await ChangeAsync(claim.Id, (db, c) => db.Add(Diagnosis(c, "Y96")));
        Assert.AreEqual("Error", (await IssuesAsync(claim.Id))["ACCIDENT_WPA_REQUIRED"]);

        await ChangeAsync(claim.Id, (_, c) => { c.AccidentType = "WPA"; c.AccidentDate = DateTime.UtcNow.Date; });
        Assert.IsFalse((await IssuesAsync(claim.Id)).Keys.Any(k => k.StartsWith("ACCIDENT_")));

        await ChangeAsync(claim.Id, (db, c) => db.Add(Diagnosis(c, "V40.60")));
        CollectionAssert.Contains((await IssuesAsync(claim.Id)).Keys.ToList(), "ACCIDENT_MVA_REQUIRED");

        await ChangeAsync(claim.Id, (db, c) =>
        {
            c.AccidentType = "MVA";
            db.RemoveRange(c.Diagnoses.Where(d => d.DiagnosisCode is "Y96" or "V40.60"));
        });
        CollectionAssert.Contains((await IssuesAsync(claim.Id)).Keys.ToList(), "ACCIDENT_MVA_DIAGNOSIS");
    }

    [TestMethod]
    public async Task A_newborn_request_needs_a_Z38_diagnosis_birth_weight_in_kg_and_services_within_90_days()
    {
        var newborn = new PatientEntity
        {
            MRN = "MRN-NB-0100", NationalIdType = "MRN", FirstName = "Baby", LastName = "Khaled", FullName = "Baby Khaled", Gender = "F",
            DateOfBirth = DateTime.UtcNow.Date.AddDays(-100),
        };
        await _host.QueryAsync(async db => { db.Patients.Add(newborn); return await db.SaveChangesAsync(); });
        var claim = await _host.SeedClaimAsync(_seed, "req_newborn_rules");
        await ChangeAsync(claim.Id, (_, c) => { c.PatientId = newborn.Id; c.IsNewborn = true; c.ClaimType = "professional"; });

        var issues = await IssuesAsync(claim.Id);
        Assert.AreEqual("Error", issues["NEWBORN_Z38"]);
        Assert.AreEqual("Error", issues["NEWBORN_BIRTH_WEIGHT"], "Required on professional requests.");
        Assert.AreEqual("Error", issues["NEWBORN_WINDOW"]);

        await ChangeAsync(claim.Id, (db, c) =>
        {
            db.Add(Diagnosis(c, "Z38.0"));
            db.Add(new ClaimSupportingInfoEntity
            {
                ClaimId = c.Id, Sequence = 9, Category = "birth-weight", CategorySystem = InfoCategories, QuantityValue = 3200, QuantityUnit = "g", DateValue = newborn.DateOfBirth,
            });
        });
        issues = await IssuesAsync(claim.Id);
        Assert.IsFalse(issues.ContainsKey("NEWBORN_Z38"));
        Assert.AreEqual("Error", issues["NEWBORN_BIRTH_WEIGHT_UNIT"]);

        await ChangeAsync(claim.Id, (_, c) =>
        {
            var weight = c.SupportingInfo.Single(s => s.Category == "birth-weight");
            weight.QuantityValue = 3.2m;
            weight.QuantityUnit = "kg";
        });
        await _host.QueryAsync(async db =>
        {
            (await db.Patients.SingleAsync(p => p.Id == newborn.Id)).DateOfBirth = DateTime.UtcNow.Date.AddDays(-10);
            return await db.SaveChangesAsync();
        });
        Assert.IsFalse((await IssuesAsync(claim.Id)).Keys.Any(k => k.StartsWith("NEWBORN_")));
    }

    [TestMethod]
    public async Task A_vital_sign_not_measured_is_sent_with_its_absence_reason()
    {
        var claim = await _host.SeedClaimAsync(_seed, "req_vitals");
        await ChangeAsync(claim.Id, (db, c) => db.Add(new ClaimSupportingInfoEntity
        {
            ClaimId = c.Id, Sequence = 9, Category = "vital-sign-systolic", CategorySystem = InfoCategories, DateValue = DateTime.UtcNow.Date,
        }));
        Assert.AreEqual("Error", (await IssuesAsync(claim.Id))["VITAL_VALUE_MISSING"]);

        await ChangeAsync(claim.Id, (_, c) => c.SupportingInfo.Single(s => s.Sequence == 9).ReasonCode = "123");
        Assert.AreEqual("Error", (await IssuesAsync(claim.Id))["VITAL_REASON_INVALID"]);

        await ChangeAsync(claim.Id, (_, c) => c.SupportingInfo.Single(s => s.Sequence == 9).ReasonCode = "888");
        Assert.IsFalse((await IssuesAsync(claim.Id)).Keys.Any(k => k.StartsWith("VITAL_")));

        using var scope = _host.Services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IRcmInputMapper>();
        var builder = scope.ServiceProvider.GetRequiredService<INphiesBundleBuilder>();
        var bundle = builder.BuildClaimRequest(await mapper.MapClaimAsync((await mapper.LoadClaimGraphAsync(claim.Id))!));
        var systolic = bundle.Entry.Select(e => e.Resource).OfType<Claim>().Single().SupportingInfo.Single(s => s.Sequence == 9);
        Assert.AreEqual("888", systolic.Reason?.Coding.Single().Code);
        StringAssert.EndsWith(systolic.Reason!.Coding.Single().System, "blood-pressure-absence-reason");
        Assert.IsNull(systolic.Value, "No value is sent with a reason.");
    }

    [TestMethod]
    public async Task Each_pharmacy_medication_item_points_at_exactly_one_days_supply_in_days()
    {
        var claim = await _host.SeedClaimAsync(_seed, "req_pharmacy_rules");
        await ChangeAsync(claim.Id, (_, c) =>
        {
            c.ClaimType = "pharmacy";
            c.Items.Single().ProductOrServiceSystem = RcmClaimRules.MedicationCodes;
        });
        Assert.AreEqual("Error", (await IssuesAsync(claim.Id))["DAYS_SUPPLY_LINK"]);

        await ChangeAsync(claim.Id, (db, c) =>
        {
            db.Add(new ClaimSupportingInfoEntity { ClaimId = c.Id, Sequence = 9, Category = "days-supply", CategorySystem = InfoCategories, QuantityValue = 7, QuantityUnit = "day" });
            c.Items.Single().InformationSequence = "9";
        });
        var issues = await IssuesAsync(claim.Id);
        Assert.IsFalse(issues.ContainsKey("DAYS_SUPPLY_LINK"));
        Assert.AreEqual("Error", issues["DAYS_SUPPLY_UNIT"]);

        await ChangeAsync(claim.Id, (_, c) => c.SupportingInfo.Single(s => s.Sequence == 9).QuantityUnit = "d");
        Assert.IsFalse((await IssuesAsync(claim.Id)).Keys.Any(k => k.StartsWith("DAYS_SUPPLY")));
    }

    [TestMethod]
    public async Task More_than_300_items_is_flagged_as_a_large_claim()
    {
        var claim = await _host.SeedClaimAsync(_seed, "req_large");
        await ChangeAsync(claim.Id, (db, c) =>
        {
            var template = c.Items.Single();
            for (var sequence = 2; sequence <= 301; sequence++)
            {
                var item = (ClaimItemEntity)db.Entry(template).CurrentValues.ToObject();
                item.Id = Guid.NewGuid().ToString();
                item.Sequence = sequence;
                db.Add(item);
            }
        });

        Assert.AreEqual("Warning", (await IssuesAsync(claim.Id))["LARGE_CLAIM"]);
    }

    [TestMethod]
    public async Task The_same_services_on_the_same_dates_for_the_patient_are_flagged_as_a_duplicate()
    {
        var first = await _host.SeedClaimAsync(_seed, "req_299060");
        _host.ReplyWith("7b - Claim Response.json");
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(first.Id));
        var second = await _host.SeedClaimAsync(_seed, "req_twin");
        var other = await _host.SeedClaimAsync(_seed, "req_other", false, ("70000-00-05", 120m));

        Assert.AreEqual("Warning", (await IssuesAsync(second.Id))["DUPLICATE_CLAIM"]);
        Assert.IsFalse((await IssuesAsync(other.Id)).ContainsKey("DUPLICATE_CLAIM"), "Different services are not a duplicate.");

        // A linked resubmission is the same claim corrected, not a duplicate.
        var resubmit = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.ResubmitClaimAsync(first.Id, new DerivedClaimRequestDto { Submit = false }));
        await _host.QueryAsync(async db =>
        {
            db.Claims.Remove(await db.Claims.SingleAsync(c => c.Id == second.Id));
            return await db.SaveChangesAsync();
        });
        Assert.IsFalse((await IssuesAsync(resubmit.ClaimId)).ContainsKey("DUPLICATE_CLAIM"));

        Assert.AreEqual("Duplicate", RcmResponseIngestor.DenialCategory("1677"));
        Assert.AreEqual("Duplicate", RcmResponseIngestor.DenialCategory("N-DC-069"));
    }

    [TestMethod]
    public async Task Add_services_starts_a_new_claim_in_the_same_episode()
    {
        var claim = await _host.SeedClaimAsync(_seed, "req_299060");
        var notSent = await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() =>
            _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.AddServicesAsync(claim.Id)));
        Assert.AreEqual(RcmErrorKind.Conflict, notSent.Kind);

        _host.ReplyWith("7b - Claim Response.json");
        await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.SubmitAsync(claim.Id));
        var added = await _host.RunAsync<IRcmClaimWorkflowService, SubmissionResultDto>(s => s.AddServicesAsync(claim.Id));

        var (original, extra) = await _host.QueryAsync(async db => (
            await db.Claims.AsNoTracking().SingleAsync(c => c.Id == claim.Id),
            await db.Claims.AsNoTracking().Include(c => c.Items).Include(c => c.RelatedClaims).SingleAsync(c => c.Id == added.ClaimId)));
        Assert.AreEqual(RcmClaimStatus.Draft, extra.NphiesStatus);
        Assert.AreEqual(original.EpisodeIdentifierValue, extra.EpisodeIdentifierValue, "Same episode of care.");
        Assert.IsNotNull(extra.EpisodeIdentifierValue);
        Assert.AreEqual(0, extra.Items.Count, "The new services are added in the editor.");
        Assert.AreEqual(0, extra.RelatedClaims.Count, "A new claim, not a resubmission.");
        Assert.IsNull(extra.ClaimIdentifierValue, "It gets its own identifier when sent.");
        Assert.AreEqual(RcmClaimStatus.Approved, original.NphiesStatus, "The sent claim is untouched.");
    }
}
