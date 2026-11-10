using Microsoft.VisualStudio.TestTools.UnitTesting;
using NPhies_FHIR_Integration.ApiService.DataExplorer;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

[TestClass]
public class DataExplorerTests
{
    [TestMethod]
    public async Task Every_table_translates_with_hospital_date_and_search_filters()
    {
        await using var host = await RcmTestHost.CreateAsync();
        var seed = await host.SeedPartiesAsync();
        await host.SeedClaimAsync(seed, "explorer-1");

        var failures = await host.QueryAsync(async db =>
        {
            var explorer = new DataExplorerService(db);
            var errors = new List<string>();
            foreach (var table in explorer.Registry.Tables)
            {
                try
                {
                    await explorer.QueryAsync(table, new ExplorerQuery
                    {
                        HospitalId = seed.Provider.Id,
                        From = DateTime.UtcNow.AddYears(-1),
                        To = DateTime.UtcNow,
                        Search = "x",
                        PageSize = 5
                    }, null, CancellationToken.None);
                }
                catch (Exception ex)
                {
                    errors.Add($"{table.Name}: {ex.GetBaseException().Message}");
                }
            }
            return errors;
        });

        Assert.AreEqual(0, failures.Count, string.Join(Environment.NewLine, failures));
    }

    [TestMethod]
    public async Task Claims_are_scoped_by_hospital_and_expose_children()
    {
        await using var host = await RcmTestHost.CreateAsync();
        var seed = await host.SeedPartiesAsync();
        var claim = await host.SeedClaimAsync(seed, "explorer-2", false, ("70000-00-00", 230m), ("70000-00-01", 100m));

        await host.QueryAsync(async db =>
        {
            var explorer = new DataExplorerService(db);
            var registry = explorer.Registry;
            Assert.IsNull(registry.Tables.FirstOrDefault(t => t.Name is "Users" or "RefreshTokens"), "Security tables must be hidden.");
            Assert.IsFalse(registry.Tables.SelectMany(t => t.Columns).Any(c => c.Key.Contains("password", StringComparison.OrdinalIgnoreCase)));

            var claims = registry.Find("Claims")!;
            Assert.IsTrue(claims.HospitalScoped);

            var mine = await explorer.QueryAsync(claims, new ExplorerQuery { HospitalId = seed.Provider.Id }, null, default);
            Assert.AreEqual(1, mine.Total);
            Assert.AreEqual(claim.Id, mine.Items[0]["id"]);

            var other = await explorer.QueryAsync(claims, new ExplorerQuery { HospitalId = seed.Insurer.Id }, null, default);
            Assert.AreEqual(0, other.Total);

            var future = await explorer.QueryAsync(claims, new ExplorerQuery { From = DateTime.UtcNow.AddDays(10) }, null, default);
            Assert.AreEqual(0, future.Total);

            var record = await explorer.GetAsync(claims, claim.Id, seed.Provider.Id, default);
            Assert.IsNotNull(record);
            var items = record!.Children.FirstOrDefault(c => c.Table == "ClaimItems");
            Assert.IsNotNull(items, "Claim items must be a child of claims: " + string.Join(",", record.Children.Select(c => c.Table)));
            Assert.AreEqual(2, items!.Count);

            // Child table inherits hospital scope through its parent claim.
            var itemTable = registry.Find("ClaimItems")!;
            Assert.IsTrue(itemTable.HospitalScoped, itemTable.HospitalPath);
            var scopedItems = await explorer.QueryAsync(itemTable, new ExplorerQuery { HospitalId = seed.Insurer.Id }, null, default);
            Assert.AreEqual(0, scopedItems.Total);

            var csv = DataExplorerService.ToCsv(itemTable, (await explorer.QueryAsync(itemTable, new ExplorerQuery(), 100, default)).Items);
            Assert.AreEqual(3, csv.Trim().Split('\n').Length);

            Assert.ThrowsException<ArgumentException>(() =>
                explorer.QueryAsync(claims, new ExplorerQuery { Filters = { ["nope"] = "1" } }, null, default).GetAwaiter().GetResult());
            return 0;
        });
    }
}
