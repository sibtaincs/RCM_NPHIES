using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Rcm;

/// <summary>
/// A lease in the database that lets one API instance at a time run a background job. Polling from several
/// instances at once would split the provider's NPHIES queue between them and send duplicate payment notices.
/// </summary>
public interface IRcmWorkerLeaseService
{
    /// <summary>
    /// Takes or renews the lease <paramref name="name"/> for <paramref name="owner"/>. Returns false while another
    /// owner holds an unexpired lease.
    /// </summary>
    Task<bool> TryAcquireAsync(string name, string owner, TimeSpan duration, CancellationToken ct = default);

    /// <summary>Gives the lease up (on shutdown) so another instance can take over at once.</summary>
    Task ReleaseAsync(string name, string owner, CancellationToken ct = default);
}

public sealed class RcmWorkerLeaseService : IRcmWorkerLeaseService
{
    public const string PollLease = "nphies-poll";

    private readonly ApplicationDbContext _db;

    public RcmWorkerLeaseService(ApplicationDbContext db) => _db = db;

    public async Task<bool> TryAcquireAsync(string name, string owner, TimeSpan duration, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var expires = now.Add(duration);

        // One UPDATE decides the winner: SQL Server locks the row, so two instances cannot both match it.
        var renewed = await _db.RcmWorkerLeases
            .Where(l => l.Name == name && l.Owner == owner)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.ExpiresAt, expires), ct);
        if (renewed == 1) return true;

        var taken = await _db.RcmWorkerLeases
            .Where(l => l.Name == name && l.ExpiresAt < now)
            .ExecuteUpdateAsync(s => s
                .SetProperty(l => l.Owner, owner)
                .SetProperty(l => l.AcquiredAt, now)
                .SetProperty(l => l.ExpiresAt, expires), ct);
        if (taken == 1) return true;

        if (await _db.RcmWorkerLeases.AnyAsync(l => l.Name == name, ct)) return false;

        // First use of this lease: the primary key lets only one instance insert it.
        var lease = new RcmWorkerLease { Name = name, Owner = owner, AcquiredAt = now, ExpiresAt = expires };
        _db.RcmWorkerLeases.Add(lease);
        try
        {
            await _db.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException)
        {
            _db.Entry(lease).State = EntityState.Detached;
            return false;
        }
    }

    public async Task ReleaseAsync(string name, string owner, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        await _db.RcmWorkerLeases
            .Where(l => l.Name == name && l.Owner == owner)
            .ExecuteUpdateAsync(s => s.SetProperty(l => l.ExpiresAt, now.AddSeconds(-1)), ct);
    }
}
