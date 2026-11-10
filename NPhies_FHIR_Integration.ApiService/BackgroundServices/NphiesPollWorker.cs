using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Services;

namespace NPhies_FHIR_Integration.ApiService.BackgroundServices;

/// <summary>
/// Periodically polls NPHIES for deferred messages (claim/prior-auth responses, communication requests,
/// payment reconciliations) and ingests them. Enabled with <c>Rcm:PollWorkerEnabled</c>.
/// </summary>
public sealed class NphiesPollWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopes;
    private readonly IOptionsMonitor<RcmOptions> _options;
    private readonly ILogger<NphiesPollWorker> _logger;

    public NphiesPollWorker(IServiceScopeFactory scopes, IOptionsMonitor<RcmOptions> options, ILogger<NphiesPollWorker> logger)
    {
        _scopes = scopes;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Let the host finish starting (migrations, seeding) before the first cycle.
        await DelaySafe(TimeSpan.FromSeconds(15), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var options = _options.CurrentValue;
            if (options.PollWorkerEnabled)
            {
                try
                {
                    using var scope = _scopes.CreateScope();
                    var poll = scope.ServiceProvider.GetRequiredService<IRcmPollService>();
                    var result = await poll.PollOnceAsync(null, stoppingToken);
                    if (result.MessagesReceived > 0 || result.Errors.Count > 0)
                        _logger.LogInformation("NPHIES poll cycle: {Polls} poll(s), {Messages} message(s), {Processed} processed, {Errors} error(s)",
                            result.PollRequests, result.MessagesReceived, result.Processed, result.Errors.Count);
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

            await DelaySafe(TimeSpan.FromSeconds(Math.Max(10, options.PollIntervalSeconds)), stoppingToken);
        }
    }

    private static async Task DelaySafe(TimeSpan delay, CancellationToken ct)
    {
        try { await Task.Delay(delay, ct); }
        catch (OperationCanceledException) { }
    }
}
