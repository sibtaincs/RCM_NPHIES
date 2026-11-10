using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.ApiService.MasterData;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

[TestClass]
public class MasterCrudTests
{
    private static Dictionary<string, JsonElement> Values(object anonymous) =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(JsonSerializer.Serialize(anonymous))!;

    private static MasterDefinition Def(string table) => MasterCatalog.Find(table)!;

    [TestMethod]
    public async Task Catalog_tables_exist_describe_and_lookup()
    {
        await using var host = await RcmTestHost.CreateAsync();
        var seed = await host.SeedPartiesAsync();

        var failures = await host.QueryAsync(async db =>
        {
            var crud = new MasterCrudService(db);
            var errors = new List<string>();
            foreach (var def in MasterCatalog.All)
            {
                try
                {
                    var info = crud.Describe(def);
                    if (info.Fields.Count == 0) errors.Add($"{def.Table}: no fields");
                    foreach (var key in def.Summary.Concat(def.Display).Concat(def.NaturalKey).Concat(def.Options.Keys).Concat(def.Lookups.Keys))
                        if (info.Fields.All(f => !string.Equals(f.Key, key, StringComparison.OrdinalIgnoreCase)))
                            errors.Add($"{def.Table}: unknown column '{key}'");
                    foreach (var f in info.Fields.Where(f => f.Lookup is not null))
                        if (crud.Registry.Find(f.Lookup!) is null) errors.Add($"{def.Table}.{f.Key}: unknown lookup table {f.Lookup}");
                    await crud.LookupAsync(crud.Table(def), def, "a", seed.Provider.Id, new(), 5, CancellationToken.None);
                    await crud.CountAsync(def, seed.Provider.Id, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    errors.Add($"{def.Table}: {ex.GetBaseException().Message}");
                }
            }
            return errors;
        });
        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));

        var coverage = await host.QueryAsync(db => Task.FromResult(new MasterCrudService(db).Describe(Def("Coverages"))));
        var insurer = coverage.Fields.Single(f => f.Key == "insurerId");
        Assert.AreEqual("Organizations", insurer.Lookup);
        Assert.AreEqual("organizationType=ins", insurer.LookupFilter);
        Assert.IsTrue(coverage.Fields.Single(f => f.Key == "patientId").Required);

        var items = await host.QueryAsync(db =>
        {
            var crud = new MasterCrudService(db);
            return crud.LookupAsync(crud.Registry.Find("Organizations")!, Def("Organizations"), null, null,
                new Dictionary<string, string> { ["organizationType"] = "ins" }, 50, CancellationToken.None);
        });
        Assert.IsTrue(items.Any(i => i.Value == seed.Insurer.Id));
        Assert.IsFalse(items.Any(i => i.Value == seed.Provider.Id));
    }

    [TestMethod]
    public async Task Location_crud_validates_and_scopes_to_hospital()
    {
        await using var host = await RcmTestHost.CreateAsync();
        var seed = await host.SeedPartiesAsync();
        var def = Def("Locations");

        var invalid = await host.QueryAsync(async db =>
        {
            try { await new MasterCrudService(db).CreateAsync(def, Values(new { locationName = "", organizationId = "missing" }), null, "tester", default); return null; }
            catch (MasterValidationException ex) { return ex.Errors; }
        });
        Assert.IsNotNull(invalid);
        CollectionAssert.IsSubsetOf(new[] { "locationName", "locationLicense", "organizationId" }, invalid.Keys.ToList());

        var created = await host.QueryAsync(db => new MasterCrudService(db).CreateAsync(def,
            Values(new { locationName = "Main OPD", locationLicense = "LOC-T-1", organizationId = seed.Provider.Id, status = "active", city = "Riyadh" }),
            null, "tester", default));
        var id = (string)created["id"]!;
        Assert.AreEqual("Main OPD", created["locationName"]);

        var duplicate = await host.QueryAsync(async db =>
        {
            try { await new MasterCrudService(db).CreateAsync(def, Values(new { locationName = "Copy", locationLicense = "LOC-T-1", organizationId = seed.Provider.Id }), null, null, default); return false; }
            catch (MasterValidationException ex) { return ex.Errors.ContainsKey("locationLicense"); }
        });
        Assert.IsTrue(duplicate, "duplicate license rejected");

        var badStatus = await host.QueryAsync(async db =>
        {
            try { await new MasterCrudService(db).UpdateAsync(def, id, Values(new { status = "closed" }), null, null, default); return false; }
            catch (MasterValidationException ex) { return ex.Errors.ContainsKey("status"); }
        });
        Assert.IsTrue(badStatus, "coded value checked");

        var updated = await host.QueryAsync(db => new MasterCrudService(db).UpdateAsync(def, id, Values(new { city = "Jeddah", locationLicense = "LOC-T-1" }), seed.Provider.Id, "tester", default));
        Assert.AreEqual("Jeddah", updated!["city"]);
        Assert.IsNotNull(updated["updatedAt"]);

        await Assert.ThrowsExceptionAsync<MasterForbiddenException>(() =>
            host.QueryAsync(db => new MasterCrudService(db).UpdateAsync(def, id, Values(new { city = "Dammam" }), "other-hospital", null, default)));

        var forced = await host.QueryAsync(db => new MasterCrudService(db).CreateAsync(def,
            Values(new { locationName = "Branch", locationLicense = "LOC-T-2", organizationId = seed.Insurer.Id }), seed.Provider.Id, null, default));
        Assert.AreEqual(seed.Provider.Id, forced["organizationId"], "non-admin rows are forced to their hospital");

        var deleted = await host.QueryAsync(db => new MasterCrudService(db).DeleteAsync(def, id, null, null, default));
        Assert.IsTrue(deleted!.Deleted);
        Assert.IsFalse(await host.QueryAsync(db => db.Locations.AnyAsync(l => l.Id == id)));
    }

    [TestMethod]
    public async Task Referenced_rows_are_deactivated_and_patients_validate_codes()
    {
        await using var host = await RcmTestHost.CreateAsync();
        var seed = await host.SeedPartiesAsync();
        await host.SeedClaimAsync(seed, "master-ref-1");

        var result = await host.QueryAsync(db => new MasterCrudService(db).DeleteAsync(Def("Organizations"), seed.Insurer.Id, null, "tester", default));
        Assert.IsTrue(result!.Deactivated);
        Assert.IsTrue(result.ReferencedBy.Count > 0);
        var insurer = await host.QueryAsync(db => db.Organizations.AsNoTracking().SingleAsync(o => o.Id == seed.Insurer.Id));
        Assert.IsFalse(insurer.IsActive);
        Assert.AreEqual("inactive", insurer.Status);

        var errors = await host.QueryAsync(async db =>
        {
            try
            {
                await new MasterCrudService(db).CreateAsync(Def("Patients"), Values(new
                {
                    mrn = "MRN-T", firstName = "Sara", lastName = "Ali", nationalId = seed.Patient.NationalId, nationalIdType = "XX",
                    gender = "F", dateOfBirth = "1990-01-01", status = "active",
                }), null, null, default);
                return null;
            }
            catch (MasterValidationException ex) { return ex.Errors; }
        });
        Assert.IsNotNull(errors);
        Assert.IsTrue(errors.ContainsKey("nationalIdType"), string.Join(",", errors.Keys));
        Assert.IsTrue(errors.ContainsKey("nationalId"), "duplicate national id");

        var patient = await host.QueryAsync(db => new MasterCrudService(db).CreateAsync(Def("Patients"), Values(new
        {
            mrn = "MRN-T2", firstName = "Sara", lastName = "Ali", firstNameArabic = "Ø³Ø§Ø±Ø©", nationalId = "2000000001", nationalIdType = "PRC",
            gender = "F", dateOfBirth = "1990-01-01", status = "active",
        }), null, null, default));
        Assert.AreEqual(new DateTime(1990, 1, 1), patient["dateOfBirth"]);

        var idErrors = await host.QueryAsync(async db =>
        {
            try
            {
                await new MasterCrudService(db).CreateAsync(Def("Patients"), Values(new
                {
                    mrn = "MRN-T3", firstName = "Raj", lastName = "Kumar", nationalId = "P1234567", nationalIdType = "PPN",
                    gender = "M", dateOfBirth = "1990-01-01", status = "active",
                }), null, null, default);
                return null;
            }
            catch (MasterValidationException ex) { return ex.Errors; }
        });
        Assert.IsTrue(idErrors!.ContainsKey("nationalIdCountry"), string.Join(",", idErrors.Keys));

        var coverage = await host.QueryAsync(db => new MasterCrudService(db).CreateAsync(Def("Coverages"), Values(new
        {
            memberID = "MEM-T2", policyNumber = "POL-1", patientId = patient["id"], insurerId = seed.Insurer.Id, coverageType = "EHCPOL",
            relationToSubscriber = "self", subscriberRelationship = "self", status = "active",
            coverageStartDate = "2025-01-01", coverageEndDate = "2026-12-31",
        }), null, null, default));
        Assert.AreEqual(patient["id"], coverage["patientId"]);
    }
}

