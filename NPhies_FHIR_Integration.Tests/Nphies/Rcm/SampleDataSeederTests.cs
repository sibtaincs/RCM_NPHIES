using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NPhies_FHIR_Integration.Application.Nphies.Builders;
using NPhies_FHIR_Integration.Application.Nphies.Validation;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;
using NPhies_FHIR_Integration.Infrastructure.Data.Seeders;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

[TestClass]
public class SampleDataSeederTests
{
    private RcmTestHost _host = null!;

    [TestInitialize]
    public async Task Init() => _host = await RcmTestHost.CreateAsync();

    [TestCleanup]
    public async Task Cleanup() => await _host.DisposeAsync();

    private Task SeedAsync() => _host.QueryAsync(async db =>
    {
        await SampleDataSeeder.SeedAsync(db, NullLogger.Instance);
        return true;
    });

    [TestMethod]
    public async Task Every_seeded_claim_builds_a_valid_nphies_bundle()
    {
        await SeedAsync();

        var ids = await _host.QueryAsync(db => db.Claims.Where(c => c.ClaimNumber.StartsWith(SampleDataSeeder.ClaimPrefix))
            .Select(c => c.Id).ToListAsync());
        Assert.AreEqual(36, ids.Count);

        var failures = new List<string>();
        using var scope = _host.Services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IRcmInputMapper>();
        var builder = scope.ServiceProvider.GetRequiredService<INphiesBundleBuilder>();
        var validator = scope.ServiceProvider.GetRequiredService<INphiesBundleValidator>();
        foreach (var id in ids)
        {
            var claim = await mapper.LoadClaimGraphAsync(id);
            Assert.IsNotNull(claim);
            var input = await mapper.MapClaimAsync(claim);
            var bundle = RcmInputMapper.IsPreAuth(claim) ? builder.BuildPriorAuthRequest(input) : builder.BuildClaimRequest(input);
            var result = validator.Validate(bundle);
            if (!result.IsValid)
                failures.Add($"{claim.ClaimNumber}: {string.Join("; ", result.Errors.Select(e => e.ToString()))}");
        }

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public async Task Every_seeded_claim_conforms_to_the_nphies_ig_profiles()
    {
        await SeedAsync();

        var ids = await _host.QueryAsync(db => db.Claims.Where(c => c.ClaimNumber.StartsWith(SampleDataSeeder.ClaimPrefix))
            .Select(c => c.Id).ToListAsync());
        var profiles = new NphiesProfileValidator(NphiesConformancePackage.Embedded);
        var failures = new List<string>();
        using var scope = _host.Services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IRcmInputMapper>();
        var builder = scope.ServiceProvider.GetRequiredService<INphiesBundleBuilder>();
        foreach (var id in ids)
        {
            var claim = (await mapper.LoadClaimGraphAsync(id))!;
            var input = await mapper.MapClaimAsync(claim);
            var bundle = RcmInputMapper.IsPreAuth(claim) ? builder.BuildPriorAuthRequest(input) : builder.BuildClaimRequest(input);
            foreach (var e in profiles.Validate(bundle).Errors)
                failures.Add($"{claim.ClaimNumber}: {e.RuleId} {e.Expression}: {e.Message}");
        }

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures.Take(40)));
    }

    [TestMethod]
    public async Task Seeder_covers_every_claim_table_and_is_idempotent()
    {
        await SeedAsync();
        var first = await CountsAsync();

        Assert.AreEqual(5, first["types"]);
        Assert.IsTrue(first["payers"] >= 6);
        Assert.IsTrue(first["hospitals"] >= 3);
        foreach (var table in new[] { "policies", "coverages", "patients", "items", "diagnoses", "careTeam", "supportingInfo",
                     "related", "vision", "oral", "accidents", "encounters", "shadow" })
            Assert.IsTrue(first[table] > 0, $"{table} not seeded");

        await SeedAsync();
        var second = await CountsAsync();
        CollectionAssert.AreEquivalent(first.ToList(), second.ToList());
    }

    private Task<Dictionary<string, int>> CountsAsync() => _host.QueryAsync(async (ApplicationDbContext db) => new Dictionary<string, int>
    {
        ["types"] = await db.Claims.Where(c => c.ClaimNumber.StartsWith(SampleDataSeeder.ClaimPrefix)).Select(c => c.ClaimType).Distinct().CountAsync(),
        ["claims"] = await db.Claims.CountAsync(),
        ["payers"] = await db.PayerMasters.CountAsync(),
        ["policies"] = await db.PayerPolicyMasters.CountAsync(),
        ["hospitals"] = await db.Organizations.CountAsync(o => o.OrganizationType == "prov"),
        ["coverages"] = await db.Coverages.CountAsync(),
        ["patients"] = await db.Patients.CountAsync(),
        ["items"] = await db.ClaimItems.CountAsync(),
        ["diagnoses"] = await db.ClaimDiagnoses.CountAsync(),
        ["careTeam"] = await db.ClaimCareTeams.CountAsync(),
        ["supportingInfo"] = await db.ClaimSupportingInfos.CountAsync(),
        ["related"] = await db.ClaimRelatedClaims.CountAsync(),
        ["vision"] = await db.VisionPrescriptions.CountAsync(),
        ["oral"] = await db.OralDetails.CountAsync(),
        ["accidents"] = await db.ClaimAccidents.CountAsync(),
        ["encounters"] = await db.Encounters.CountAsync(),
        ["shadow"] = await db.ClaimItems.CountAsync(i => i.AltProductOrServiceCode != null),
    });
}
