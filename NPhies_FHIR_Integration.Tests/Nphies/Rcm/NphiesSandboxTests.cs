using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Nphies.Sandbox;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Infrastructure.Data.Seeders;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

[TestClass]
public class NphiesSandboxTests
{
    private RcmTestHost _host = null!;
    private NphiesSandboxTransport _sandbox = null!;

    [TestInitialize]
    public async Task Init()
    {
        _host = await RcmTestHost.CreateAsync();
        _sandbox = _host.Services.GetRequiredService<NphiesSandboxTransport>();
        _host.ReplyWith(r => _sandbox.SendAsync(r).GetAwaiter().GetResult().Body);
    }

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    [TestMethod]
    public async Task Router_uses_sandbox_when_enabled_or_activated()
    {
        var empty = new ServiceCollection().BuildServiceProvider();
        var bundle = new Hl7.Fhir.Model.Bundle
        {
            Id = "b-1",
            Type = Hl7.Fhir.Model.Bundle.BundleType.Message,
            Entry =
            {
                new Hl7.Fhir.Model.Bundle.EntryComponent
                {
                    FullUrl = "urn:uuid:m-1",
                    Resource = new Hl7.Fhir.Model.MessageHeader
                    {
                        Id = "m-1",
                        Event = new Hl7.Fhir.Model.Coding("http://nphies.sa/terminology/CodeSystem/ksa-message-events", "status-check"),
                        Sender = new Hl7.Fhir.Model.ResourceReference { Identifier = new Hl7.Fhir.Model.Identifier("http://nphies.sa/license/provider-license", "PR-X") },
                        Source = new Hl7.Fhir.Model.MessageHeader.MessageSourceComponent { Endpoint = "http://provider.local" },
                    },
                },
            },
        };
        var json = new Hl7.Fhir.Serialization.FhirJsonSerializer().SerializeToString(bundle);
        var request = new Infrastructure.Nphies.NphiesTransportRequest(json, "m-1", "b-1", "communication", true);

        var enabled = new NphiesTransportRouter(empty, _sandbox, Options.Create(new NphiesSandboxOptions { Enabled = true }));
        Assert.AreEqual("sandbox://nphies/$process-message", (await enabled.SendAsync(request)).Endpoint);

        var disabled = new NphiesTransportRouter(empty, _sandbox, Options.Create(new NphiesSandboxOptions()));
        await Assert.ThrowsExceptionAsync<InvalidOperationException>(() => disabled.SendAsync(request));
        using (NphiesSandboxTransport.Activate())
            Assert.AreEqual("sandbox://nphies/$process-message", (await disabled.SendAsync(request)).Endpoint);
    }

    [TestMethod]
    public async Task Activity_seeder_produces_full_rcm_lifecycle_and_is_idempotent()
    {
        await _host.QueryAsync(async db => { await SampleDataSeeder.SeedAsync(db, NullLogger.Instance); return true; });

        var result = await RcmSampleActivitySeeder.SeedAsync(_host.Services, NullLogger.Instance);
        Assert.IsFalse(result.Skipped);
        Assert.IsTrue(result.EligibilityChecks > 0);
        Assert.IsTrue(result.Submitted > 20, $"Submitted {result.Submitted}: {string.Join(" | ", result.Notes)}");

        var statuses = await _host.QueryAsync(db => db.Claims.Where(c => c.ClaimNumber.StartsWith(SampleDataSeeder.ClaimPrefix))
            .GroupBy(c => c.NphiesStatus).Select(g => g.Key).ToListAsync());
        foreach (var expected in new[] { RcmClaimStatus.Approved, RcmClaimStatus.Rejected, RcmClaimStatus.Paid })
            CollectionAssert.Contains(statuses, expected, $"Statuses: {string.Join(",", statuses)}");
        Assert.IsTrue(statuses.Contains(RcmClaimStatus.PartiallyApproved) || statuses.Contains(RcmClaimStatus.PartiallyPaid),
            $"Statuses: {string.Join(",", statuses)}");

        var counts = await _host.QueryAsync(async db => new
        {
            Eligibility = await db.EligibilityChecks.CountAsync(e => e.Outcome != null),
            Responses = await db.ClaimResponses.CountAsync(),
            Denials = await db.ClaimDenials.CountAsync(),
            Payments = await db.PaymentReconciliations.CountAsync(),
            Acknowledged = await db.PaymentReconciliations.CountAsync(p => p.AcknowledgedAt != null),
            Unmatched = await db.PaymentReconciliationDetails.CountAsync(d => d.MatchedClaimId == null),
            Requests = await db.CommunicationRequests.CountAsync(),
            Communications = await db.Communications.CountAsync(),
            Appeals = await db.AppealRequests.CountAsync(),
            Errors = await db.NphiesTransactions.CountAsync(t => t.Status == "Error"),
            Future = await db.Claims.CountAsync(c => c.SubmittedAt > DateTime.UtcNow.AddMinutes(1)),
        });
        Assert.IsTrue(counts.Eligibility > 0, "eligibility");
        Assert.IsTrue(counts.Responses > 20, "responses");
        Assert.IsTrue(counts.Denials > 0, "denials");
        Assert.IsTrue(counts.Payments > 0, "payments");
        Assert.AreEqual(counts.Payments, counts.Acknowledged, "every payment reconciliation is acknowledged");
        Assert.AreEqual(0, counts.Unmatched, "payment details match claims");
        Assert.IsTrue(counts.Requests > 0, "communication requests");
        Assert.IsTrue(counts.Communications > 0, "communications");
        Assert.IsTrue(counts.Appeals > 0, $"appeals: {string.Join(" | ", result.Notes)}");
        Assert.AreEqual(0, counts.Errors, "no transaction errors");
        Assert.AreEqual(0, counts.Future, "no future timestamps");

        var again = await RcmSampleActivitySeeder.SeedAsync(_host.Services, NullLogger.Instance);
        Assert.IsTrue(again.Skipped);
    }
}
