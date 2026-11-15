using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Application.Extensions;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using NPhies_FHIR_Integration.Infrastructure.Configuration;
using NphiesTransaction = NPhies_FHIR_Integration.Domain.Entities.NphiesTransaction;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

/// <summary>The poll lease (one instance polls at a time) and the stuck-request scan (flag, escalate at 30 days, resolve).</summary>
[TestClass]
public class RcmWorkerLeaseAndStuckRequestTests
{
    private RcmTestHost _host = null!;

    [TestInitialize]
    public async Task Init() => _host = await RcmTestHost.CreateAsync();

    [TestCleanup]
    public async Task Cleanup() => await (_host?.DisposeAsync() ?? ValueTask.CompletedTask);

    private Task<bool> AcquireAsync(string owner, TimeSpan duration) =>
        _host.RunAsync<IRcmWorkerLeaseService, bool>(s => s.TryAcquireAsync(RcmWorkerLeaseService.PollLease, owner, duration));

    [TestMethod]
    public async Task Only_one_instance_holds_the_poll_lease_until_it_expires_or_is_released()
    {
        Assert.IsTrue(await AcquireAsync("instance-a", TimeSpan.FromMinutes(10)), "The first instance takes the lease.");
        Assert.IsFalse(await AcquireAsync("instance-b", TimeSpan.FromMinutes(10)), "A second instance must stand by.");
        Assert.IsTrue(await AcquireAsync("instance-a", TimeSpan.FromMinutes(10)), "The holder renews its lease.");

        await _host.RunAsync<IRcmWorkerLeaseService, int>(async s =>
        {
            await s.ReleaseAsync(RcmWorkerLeaseService.PollLease, "instance-b");
            return 0;
        });
        Assert.IsFalse(await AcquireAsync("instance-b", TimeSpan.FromMinutes(10)), "Only the holder can release the lease.");

        await _host.RunAsync<IRcmWorkerLeaseService, int>(async s =>
        {
            await s.ReleaseAsync(RcmWorkerLeaseService.PollLease, "instance-a");
            return 0;
        });
        Assert.IsTrue(await AcquireAsync("instance-b", TimeSpan.FromMinutes(10)), "After a release another instance takes over.");
        Assert.IsFalse(await AcquireAsync("instance-a", TimeSpan.FromMinutes(10)));
    }

    [TestMethod]
    public async Task An_expired_lease_is_taken_over()
    {
        Assert.IsTrue(await AcquireAsync("instance-a", TimeSpan.FromSeconds(-5)), "A lease that is already expired.");
        Assert.IsTrue(await AcquireAsync("instance-b", TimeSpan.FromMinutes(10)));
        Assert.AreEqual("instance-b", await _host.QueryAsync(db => db.RcmWorkerLeases.Select(l => l.Owner).SingleAsync()));
    }

    [TestMethod]
    public async Task Concurrent_instances_cannot_both_take_the_lease()
    {
        var attempts = await Task.WhenAll(Enumerable.Range(0, 8).Select(i => AcquireAsync($"instance-{i}", TimeSpan.FromMinutes(10))));
        Assert.AreEqual(1, attempts.Count(won => won));
    }

    private async Task<string> SeedWaitingClaimAsync(RcmTestHost.Seed seed, string identifier, string status, int daysAgo, bool heldByNphies = false)
    {
        var claim = await _host.SeedClaimAsync(seed, identifier);
        await _host.QueryAsync(async db =>
        {
            var c = await db.Claims.SingleAsync(x => x.Id == claim.Id);
            c.NphiesStatus = status;
            c.SubmittedAt = DateTime.UtcNow.AddDays(-daysAgo);
            db.NphiesTransactions.Add(new NphiesTransaction
            {
                Direction = "outbound",
                Operation = RcmOperations.Claim,
                EventCode = "claim-request",
                MessageId = "msg-" + identifier,
                BundleId = "bundle-" + identifier,
                EntityType = RcmEntityTypes.Claim,
                EntityId = claim.Id,
                ProviderId = seed.Provider.Id,
                Status = RcmTransactionStatus.Queued,
                NphiesGenerated = heldByNphies,
                SentAt = c.SubmittedAt.Value,
            });
            return await db.SaveChangesAsync();
        });
        return claim.Id;
    }

    private Task<StuckScanResultDto> ScanAsync() => _host.RunAsync<IRcmStuckRequestService, StuckScanResultDto>(s => s.ScanAsync());

    private Task<RcmPagedResult<StuckRequestDto>> ListAsync(RcmQuery? query = null) =>
        _host.RunAsync<IRcmStuckRequestService, RcmPagedResult<StuckRequestDto>>(s => s.ListAsync(query ?? new RcmQuery()));

    [TestMethod]
    public async Task Requests_waiting_too_long_are_flagged_then_escalated_with_their_bundle_ids()
    {
        var seed = await _host.SeedPartiesAsync();
        await SeedWaitingClaimAsync(seed, "req_recent", RcmClaimStatus.Queued, daysAgo: 1);
        var flagged = await SeedWaitingClaimAsync(seed, "req_flagged", RcmClaimStatus.Queued, daysAgo: 5, heldByNphies: true);
        var escalated = await SeedWaitingClaimAsync(seed, "req_escalated", RcmClaimStatus.Timeout, daysAgo: 31);
        var done = await SeedWaitingClaimAsync(seed, "req_done", RcmClaimStatus.Approved, daysAgo: 40);

        var scan = await ScanAsync();

        Assert.AreEqual(2, scan.Flagged);
        Assert.AreEqual(1, scan.Escalated);
        Assert.AreEqual(2, scan.Open);
        var rows = (await ListAsync()).Items;
        CollectionAssert.AreEquivalent(new[] { flagged, escalated }, rows.Select(r => r.ClaimId).ToList());
        Assert.IsFalse(rows.Any(r => r.ClaimId == done));

        var f = rows.Single(r => r.ClaimId == flagged);
        Assert.AreEqual(RcmStuckLevel.Flagged, f.Level);
        Assert.IsTrue(f.HeldByNphies);
        Assert.AreEqual(5, f.DaysWaiting);

        var e = rows.Single(r => r.ClaimId == escalated);
        Assert.AreEqual(RcmStuckLevel.Escalated, e.Level);
        Assert.IsNotNull(e.EscalatedAt);
        Assert.AreEqual("bundle-req_escalated", e.BundleId, "NPHIES support asks for the bundle id.");
        Assert.AreEqual("msg-req_escalated", e.MessageId);
        StringAssert.Contains(e.Guidance, "NPHIES support");

        var again = await ScanAsync();
        Assert.AreEqual(0, again.Flagged + again.Escalated + again.Resolved, "A second scan changes nothing.");
        Assert.AreEqual(2, await _host.QueryAsync(db => db.RcmStuckRequests.CountAsync()));
    }

    [TestMethod]
    public async Task A_stuck_request_is_resolved_when_its_final_response_arrives()
    {
        var seed = await _host.SeedPartiesAsync();
        var claimId = await SeedWaitingClaimAsync(seed, "req_waiting", RcmClaimStatus.Queued, daysAgo: 6);
        await ScanAsync();

        await _host.QueryAsync(async db =>
        {
            (await db.Claims.SingleAsync(c => c.Id == claimId)).NphiesStatus = RcmClaimStatus.Approved;
            return await db.SaveChangesAsync();
        });
        var scan = await ScanAsync();

        Assert.AreEqual(1, scan.Resolved);
        Assert.AreEqual(0, scan.Open);
        Assert.AreEqual(0, (await ListAsync()).Total, "Open requests only by default.");
        var resolved = (await ListAsync(new RcmQuery { Status = RcmStuckLevel.Resolved })).Items.Single();
        Assert.IsNotNull(resolved.ResolvedAt);
    }

    [TestMethod]
    public async Task Stuck_requests_are_scoped_to_the_provider()
    {
        var seed = await _host.SeedPartiesAsync();
        await SeedWaitingClaimAsync(seed, "req_waiting", RcmClaimStatus.Pended, daysAgo: 6);
        await ScanAsync();

        Assert.AreEqual(1, (await ListAsync(new RcmQuery { ProviderId = seed.Provider.Id })).Total);
        Assert.AreEqual(0, (await ListAsync(new RcmQuery { ProviderId = "another-hospital" })).Total);
    }

    // ----- rules that need no database -----

    [TestMethod]
    public void Poll_runs_more_often_in_working_hours_and_at_least_daily()
    {
        var options = new RcmOptions { PollIntervalSeconds = 300, OffHoursPollIntervalSeconds = 3600, WorkingHoursStart = 7, WorkingHoursEnd = 20 };
        // 09:00 UTC is 12:00 in Riyadh; 22:00 UTC is 01:00 in Riyadh.
        Assert.AreEqual(TimeSpan.FromSeconds(300), options.PollIntervalAt(new DateTime(2026, 11, 15, 9, 0, 0, DateTimeKind.Utc)));
        Assert.AreEqual(TimeSpan.FromSeconds(3600), options.PollIntervalAt(new DateTime(2026, 11, 15, 22, 0, 0, DateTimeKind.Utc)));

        options.OffHoursPollIntervalSeconds = 7 * 86400;
        Assert.AreEqual(TimeSpan.FromDays(1), options.PollIntervalAt(new DateTime(2026, 11, 15, 22, 0, 0, DateTimeKind.Utc)), "Never less often than daily.");
    }

    [TestMethod]
    public void Not_required_prior_authorization_gets_its_own_status()
    {
        Assert.AreEqual(RcmClaimStatus.NotRequired, RcmResponseIngestor.MapAdjudicationStatus("complete", "not-required"));
        Assert.AreEqual(RcmClaimStatus.Pended, RcmResponseIngestor.MapAdjudicationStatus("partial", "pended"));
        Assert.AreEqual(RcmClaimStatus.Queued, RcmResponseIngestor.MapAdjudicationStatus("queued", "pended"));
        CollectionAssert.Contains(RcmClaimStatus.Adjudicated, RcmClaimStatus.NotRequired);
    }

    [TestMethod]
    public void Configuration_cannot_exceed_the_nphies_limits()
    {
        var validator = new NphiesConfigurationValidator();
        Assert.IsTrue(validator.Validate(null, new NphiesConfiguration()).Succeeded, "The defaults are within the limits.");

        var failures = validator.Validate(null, new NphiesConfiguration { Timeout = 120, MaxMessageSizeBytes = 50_000_000, BatchMaxClaims = 500 }).Failures.ToList();
        Assert.IsTrue(failures.Any(f => f.Contains("Nphies:Timeout")), string.Join(" | ", failures));
        Assert.IsTrue(failures.Any(f => f.Contains("Nphies:MaxMessageSizeBytes")));
        Assert.IsTrue(failures.Any(f => f.Contains("Nphies:BatchMaxClaims")));
    }
}
