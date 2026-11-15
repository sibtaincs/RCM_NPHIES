using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Rcm.Documents;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Infrastructure.Configuration;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;
using NPhies_FHIR_Integration.Infrastructure.Nphies;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Application.Rcm.Services;

/// <summary>
/// Recovers a request whose answer never arrived (timeout, connection failure, crash before the reply) by sending the
/// identical message again. NPHIES detects it as a duplicate and returns its stored response, or "queued" when there is
/// none yet (Claim Submission Guideline). Sending it again with new ids instead would create a business duplicate.
/// </summary>
public interface IRcmResendService
{
    Task<ResendResultDto> ResendAsync(string transactionId, string? providerId, CancellationToken ct = default);
}

public sealed class RcmResendService : IRcmResendService
{
    /// <summary>Operations NPHIES can queue, so an identical resend returns the stored response.</summary>
    public static readonly string[] ResendableOperations =
        { RcmOperations.PriorAuth, RcmOperations.Claim, RcmOperations.Communication, RcmOperations.Cancel, RcmOperations.Nullify };

    /// <summary>Real-time only operations: NPHIES never queues them, so a lost reply is recovered with a new request.</summary>
    public static readonly string[] RealTimeOnlyOperations = { RcmOperations.Eligibility, RcmOperations.StatusCheck };

#pragma warning disable CS0618 // ParserSettings is the supported way to relax NPHIES-specific deviations in Firely 5.x
    private static readonly FhirJsonParser Parser = new(new ParserSettings { AcceptUnknownMembers = true, AllowUnrecognizedEnums = true, PermissiveParsing = true });
#pragma warning restore CS0618

    private readonly ApplicationDbContext _db;
    private readonly IRcmMessageDispatcher _dispatcher;
    private readonly IRcmResponseIngestor _ingestor;
    private readonly IClaimDocumentStorage _documents;
    private readonly NphiesConfiguration _nphies;

    public RcmResendService(
        ApplicationDbContext db,
        IRcmMessageDispatcher dispatcher,
        IRcmResponseIngestor ingestor,
        IClaimDocumentStorage documents,
        IOptions<NphiesConfiguration> nphies)
    {
        _db = db;
        _dispatcher = dispatcher;
        _ingestor = ingestor;
        _documents = documents;
        _nphies = nphies.Value;
    }

    public async Task<ResendResultDto> ResendAsync(string transactionId, string? providerId, CancellationToken ct = default)
    {
        var tx = await _db.NphiesTransactions.FirstOrDefaultAsync(t => t.Id == transactionId && t.Direction == "outbound", ct);
        if (tx is null || (!string.IsNullOrWhiteSpace(providerId) && tx.ProviderId != null && tx.ProviderId != providerId))
            throw RcmWorkflowException.NotFound("Transaction", transactionId);

        EnsureResendable(tx);
        var message = await LoadOriginalMessageAsync(tx, ct);

        var dispatch = await _dispatcher.ResendAsync(tx, message, ct);

        string? claimStatus = null;
        var claim = tx.EntityType == RcmEntityTypes.Claim && tx.EntityId != null
            ? await _db.Claims.FirstOrDefaultAsync(c => c.Id == tx.EntityId, ct)
            : null;
        if (claim != null && tx.Operation is RcmOperations.Claim or RcmOperations.PriorAuth)
        {
            // Same mapping as the first send; a synchronous adjudication ingested below refines it.
            claim.NphiesStatus = dispatch.Status switch
            {
                RcmTransactionStatus.Timeout => RcmClaimStatus.Timeout,
                RcmTransactionStatus.Queued => RcmClaimStatus.Queued,
                RcmTransactionStatus.Error => RcmClaimStatus.Error,
                _ => RcmClaimStatus.Submitted,
            };
            claim.UpdatedAt = DateTime.UtcNow;
        }

        if (dispatch.Response != null && dispatch.Status is RcmTransactionStatus.Completed or RcmTransactionStatus.Queued or RcmTransactionStatus.Error)
        {
            await _db.SaveChangesAsync(ct);
            await _ingestor.IngestAsync(dispatch.Response, dispatch.Transaction, ct);
            if (claim != null && dispatch.Status == RcmTransactionStatus.Error) claim.NphiesStatus = RcmClaimStatus.Error;
        }
        await _db.SaveChangesAsync(ct);
        claimStatus = claim?.NphiesStatus;

        return new ResendResultDto
        {
            TransactionId = tx.Id,
            Operation = tx.Operation,
            MessageId = tx.MessageId,
            BundleId = tx.BundleId,
            ResendCount = tx.ResendCount,
            TransactionStatus = dispatch.Status,
            ErrorKind = tx.ErrorKind,
            EntityType = tx.EntityType,
            EntityId = tx.EntityId,
            ClaimStatus = claimStatus,
            Errors = dispatch.Errors,
        };
    }

    private void EnsureResendable(Domain.Entities.NphiesTransaction tx)
    {
        if (RealTimeOnlyOperations.Contains(tx.Operation))
            throw RcmWorkflowException.Conflict(
                $"A {tx.Operation} is real time only and NPHIES never queues it, so the identical message cannot be recovered. Send a new {tx.Operation} request instead.");
        if (!ResendableOperations.Contains(tx.Operation))
            throw RcmWorkflowException.Conflict($"A {tx.Operation} message cannot be resent. Only prior authorizations, claims, communications and cancel requests can.");

        var stalePending = tx.Status == RcmTransactionStatus.Pending
                           && tx.SentAt < DateTime.UtcNow.AddSeconds(-(Math.Max(_nphies.Timeout, 1) + 30));
        var transportError = tx.Status == RcmTransactionStatus.Error && string.IsNullOrEmpty(tx.ResponseMessageId);
        if (tx.Status != RcmTransactionStatus.Timeout && !stalePending && !transportError)
            throw RcmWorkflowException.Conflict(tx.Status == RcmTransactionStatus.Pending
                ? "The message is still being sent. Wait for it to finish before resending."
                : $"The message is {tx.Status} and already has an answer from NPHIES, so there is nothing to recover. " +
                  "Use status-check or poll to follow it, or send a corrected request with a new identifier.");
    }

    /// <summary>Rebuilds the exact bundle that was sent from the message audit, restoring any attachments that were stripped.</summary>
    private async Task<Bundle> LoadOriginalMessageAsync(Domain.Entities.NphiesTransaction tx, CancellationToken ct)
    {
        var log = await _db.NphiesMessageLogs.AsNoTracking()
            .Where(l => l.MessageId == tx.MessageId && l.RequestJson != null && l.RequestJson != "")
            .OrderBy(l => l.CreatedAt)
            .FirstOrDefaultAsync(ct);
        if (log is null)
            throw RcmWorkflowException.Conflict(
                "There is no stored copy of this message (is Nphies:EnableMessageAudit off?), so it cannot be resent identically. Use status-check or poll.");

        var json = await NphiesAuditAttachments.RehydrateAsync(log.RequestJson, LoadDocumentContentAsync, ct);
        if (string.IsNullOrEmpty(json) || json.Contains("\"" + NphiesAuditAttachments.MarkerPrefix, StringComparison.Ordinal))
            throw RcmWorkflowException.Conflict(
                "An attachment of the original message is no longer stored, so it cannot be resent identically. Use status-check or poll.");

        try
        {
            return Parser.Parse<Bundle>(json);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or InvalidOperationException)
        {
            throw RcmWorkflowException.Conflict($"The stored copy of message {tx.MessageId} could not be read: {ex.Message}");
        }
    }

    private async Task<byte[]?> LoadDocumentContentAsync(string documentId, CancellationToken ct)
    {
        var doc = await _db.ClaimDocuments.AsNoTracking().FirstOrDefaultAsync(d => d.Id == documentId, ct);
        return doc is null ? null : await _documents.ReadAsync(doc.StorageProvider, doc.StorageKey, ct);
    }
}
