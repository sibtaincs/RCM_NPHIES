using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Infrastructure.Nphies;

/// <summary>Persists raw NPHIES request/response bundles for audit.</summary>
public interface INphiesMessageAuditStore
{
    Task SaveAsync(NphiesMessageLog log, CancellationToken cancellationToken = default);
}

/// <summary>EF Core audit store (table <c>NphiesMessageLogs</c>).</summary>
public sealed class EfNphiesMessageAuditStore : INphiesMessageAuditStore
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<EfNphiesMessageAuditStore> _logger;
    private readonly bool _stripStoredAttachments;

    public EfNphiesMessageAuditStore(ApplicationDbContext db, ILogger<EfNphiesMessageAuditStore> logger, bool stripStoredAttachments = true)
    {
        _db = db;
        _logger = logger;
        _stripStoredAttachments = stripStoredAttachments;
    }

    public async Task SaveAsync(NphiesMessageLog log, CancellationToken cancellationToken = default)
    {
        if (_stripStoredAttachments && !string.IsNullOrEmpty(log.RequestJson))
            log.RequestJson = await NphiesAuditAttachments.StripAsync(log.RequestJson, _db, cancellationToken).ConfigureAwait(false);
        _db.NphiesMessageLogs.Add(log);
        await _db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        _logger.LogDebug("Audited NPHIES message {MessageId} ({EventCode})", log.MessageId, log.EventCode);
    }
}

/// <summary>No-op audit store (used when auditing is disabled or no database is configured).</summary>
public sealed class NullNphiesMessageAuditStore : INphiesMessageAuditStore
{
    public Task SaveAsync(NphiesMessageLog log, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
