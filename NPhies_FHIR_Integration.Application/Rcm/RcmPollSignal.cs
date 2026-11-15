namespace NPhies_FHIR_Integration.Application.Rcm;

/// <summary>
/// Wakes the background poll worker before its next scheduled cycle, for example when a response carries the
/// <c>queued-messages</c> meta-tag (more messages are waiting in the provider's NPHIES queue).
/// </summary>
public interface IRcmPollSignal
{
    /// <summary>Ask for a poll as soon as possible. Repeated requests before the worker wakes collapse into one.</summary>
    void Request(string reason);

    /// <summary>
    /// Waits until a poll is requested or <paramref name="timeout"/> elapses. Returns the reason of the request that
    /// woke it, or null on timeout.
    /// </summary>
    Task<string?> WaitAsync(TimeSpan timeout, CancellationToken ct);
}

public sealed class RcmPollSignal : IRcmPollSignal
{
    private readonly SemaphoreSlim _signal = new(0, 1);
    private string? _reason;

    public void Request(string reason)
    {
        Interlocked.Exchange(ref _reason, reason);
        try { _signal.Release(); }
        catch (SemaphoreFullException) { /* a request is already pending */ }
    }

    public async Task<string?> WaitAsync(TimeSpan timeout, CancellationToken ct)
    {
        if (!await _signal.WaitAsync(timeout, ct).ConfigureAwait(false)) return null;
        return Interlocked.Exchange(ref _reason, null) ?? "poll requested";
    }
}
