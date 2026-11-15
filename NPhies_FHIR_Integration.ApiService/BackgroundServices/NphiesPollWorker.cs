using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Services;

namespace NPhies_FHIR_Integration.ApiService.BackgroundServices;

/// <summary>
/// Periodically polls NPHIES for deferred messages (claim/prior-auth responses, communication requests,
/// payment reconciliations) and ingests them, then scans for requests that have waited too long.
/// <list type="bullet">
/// <item>On unless <c>Rcm:PollWorkerEnabled</c> says otherwise; off by default only in Development.</item>
/// <item>Polls every <c>Rcm:PollIntervalSeconds</c> in working hours and every <c>Rcm:OffHoursPollIntervalSeconds</c>
/// outside them, never less often than daily.</item>
/// <item>Safe on several instances: only the holder of the database lease polls.</item>
/// </list>
/// </summary>
public sealed class NphiesPollWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IOptionsMonitor<RcmOptions> _options;
    private readonly IRcmPollSignal _signal;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<NphiesPollWorker> _logger;
    private readonly string _owner = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid():N}";
    private DateTime _lastStuckScan = DateTime.MinValue;
    private bool _holdsLease;

    public NphiesPollWorker(IServiceScopeFactory scopes, IOptionsMonitor<RcmOptions> options, IRcmPollSignal signal,
        IHostEnvironment environment, ILogger<NphiesPollWorker> logger)
    {
        _scopes = scopes;
        _options = options;
        _signal = signal;
        _environment = environment;
        _logger = logger;
    }

    /// <summary>The explicit setting wins; without one the worker runs everywhere except Development.</summary>
    public static bool IsEnabled(RcmOptions options, IHostEnvironment environment) =>
        options.PollWorkerEnabled ?? !environment.IsDevelopment();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the host finish starting (migrations, seeding) before the first cycle.
        await DelaySafe(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.CurrentValue;
            var interval = options.PollIntervalAt(DateTime.UtcNow);
            if (IsEnabled(options, _environment))
            {
                try
                {
                    await RunCycleAsync(options, interval, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "NPHIES poll cycle failed");
                }
            }

            // Sleep until the next cycle, or until a response tagged queued-messages asks for an early poll.
            try
            {
                var reason = await _signal.WaitAsync(interval, stoppingToken);
                if (reason is not null)
                    _logger.LogInformation("NPHIES poll requested early: {Reason}", reason);
            }
            catch (OperationCanceledException) { }
        }

        await ReleaseLeaseAsync();
    }

    private async Task RunCycleAsync(RcmOptions options, TimeSpan interval, CancellationToken ct)
    {
        using var scope = _scopes.CreateScope();
        var leases = scope.ServiceProvider.GetRequiredService<IRcmWorkerLeaseService>();

        // The lease must outlive the sleep between cycles, or another instance would take it while this one waits.
        var leaseFor = TimeSpan.FromSeconds(Math.Max(options.PollLeaseSeconds, interval.TotalSeconds * 2));
        var held = await leases.TryAcquireAsync(RcmWorkerLeaseService.PollLease, _owner, leaseFor, ct);
        if (held != _holdsLease)
            _logger.LogInformation(held
                ? "This instance ({Owner}) now runs the NPHIES poll."
                : "Another instance runs the NPHIES poll; this one ({Owner}) is on standby.", _owner);
        _holdsLease = held;
        if (!held) return;

        var poll = scope.ServiceProvider.GetRequiredService<IRcmPollService>();
        var result = await poll.PollOnceAsync(null, ct);
        if (result.MessagesReceived > 0 || result.Errors.Count > 0)
            _logger.LogInformation("NPHIES poll cycle: {Polls} poll(s), {Messages} message(s), {Processed} processed, {Errors} error(s)",
                result.PollRequests, result.MessagesReceived, result.Processed, result.Errors.Count);

        if (DateTime.UtcNow - _lastStuckScan >= TimeSpan.FromMinutes(Math.Max(1, options.StuckScanIntervalMinutes)))
        {
            var stuck = await scope.ServiceProvider.GetRequiredService<IRcmStuckRequestService>().ScanAsync(ct);
            _lastStuckScan = DateTime.UtcNow;
            if (stuck.Flagged > 0 || stuck.Escalated > 0 || stuck.Resolved > 0)
                _logger.LogInformation("NPHIES stuck-request scan: {Flagged} flagged, {Escalated} escalated, {Resolved} resolved, {Open} still waiting",
                    stuck.Flagged, stuck.Escalated, stuck.Resolved, stuck.Open);
        }
    }

    /// <summary>On shutdown, hand the poll to another instance at once instead of making it wait for the lease to expire.</summary>
    private async Task ReleaseLeaseAsync()
    {
        if (!_holdsLease) return;
        try
        {
            using var scope = _scopes.CreateScope();
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await scope.ServiceProvider.GetRequiredService<IRcmWorkerLeaseService>()
                .ReleaseAsync(RcmWorkerLeaseService.PollLease, _owner, timeout.Token);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not release the NPHIES poll lease; it expires on its own.");
        }
    }

    private static async Task DelaySafe(TimeSpan delay, CancellationToken ct)
    {
        try { await Task.Delay(delay, ct); }
        catch (OperationCanceledException) { }
    }
}
