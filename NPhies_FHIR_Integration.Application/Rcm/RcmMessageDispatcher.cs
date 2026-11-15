using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Application.Exceptions;
using NPhies_FHIR_Integration.Application.Nphies.Messaging;
using NPhies_FHIR_Integration.Application.Nphies.Parsing;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Application.Rcm;

/// <summary>Outcome of sending one message through the messaging core.</summary>
public sealed class RcmDispatchResult
{
    public required NphiesTransaction Transaction { get; init; }
    public NphiesMessageResult? Result { get; init; }
    public NphiesResponse? Response => Result?.Response;

    /// <summary>Pending / Completed / Queued / Error / ValidationFailed / Timeout.</summary>
    public string Status => Transaction.Status;
    public bool Sent => Status is not RcmTransactionStatus.ValidationFailed;
    public bool IsTimeout => Status == RcmTransactionStatus.Timeout;
    public List<string> Errors { get; } = new();
}

public sealed class RcmDispatchRequest
{
    public required Bundle Message { get; init; }
    public required string Operation { get; init; }
    public string? EntityType { get; init; }
    public string? EntityId { get; init; }
    public string? RequestIdentifierSystem { get; init; }
    public string? RequestIdentifierValue { get; init; }
    public string? ProviderId { get; init; }
    public string? CorrelationId { get; init; }
}

/// <summary>
/// Sends a message via <see cref="INphiesMessagingClient"/> and tracks it in <c>NphiesTransactions</c>.
/// The transaction row is persisted <b>before</b> sending so a crash/timeout can be recovered by status-check/poll.
/// </summary>
public interface IRcmMessageDispatcher
{
    Task<RcmDispatchResult> DispatchAsync(RcmDispatchRequest request, CancellationToken ct = default);

    /// <summary>
    /// Sends the identical message of an earlier outbound transaction again: same Bundle.id, MessageHeader.id and body,
    /// replayed from the message audit. NPHIES answers a resent message with its stored response (or "queued"), so this
    /// is the safe recovery after a timeout or transport failure. Allowed only for prior authorizations, claims,
    /// communications and cancel/nullify requests; eligibility and status-check are never queued, so they must be sent
    /// as new requests instead.
    /// </summary>
    /// <param name="message">The bundle exactly as it was first sent.</param>
    Task<RcmDispatchResult> ResendAsync(NphiesTransaction transaction, Bundle message, CancellationToken ct = default);
}

public sealed class RcmMessageDispatcher : IRcmMessageDispatcher
{
    private readonly INphiesMessagingClient _client;
    private readonly ApplicationDbContext _db;
    private readonly IRcmPollSignal _pollSignal;
    private readonly ILogger<RcmMessageDispatcher> _logger;

    public RcmMessageDispatcher(INphiesMessagingClient client, ApplicationDbContext db, IRcmPollSignal pollSignal, ILogger<RcmMessageDispatcher> logger)
    {
        _client = client;
        _db = db;
        _pollSignal = pollSignal;
        _logger = logger;
    }

    public async Task<RcmDispatchResult> DispatchAsync(RcmDispatchRequest request, CancellationToken ct = default)
    {
        var header = request.Message.Entry.Select(e => e.Resource).OfType<Hl7.Fhir.Model.MessageHeader>().FirstOrDefault();
        var tx = new NphiesTransaction
        {
            Direction = "outbound",
            Operation = request.Operation,
            EventCode = (header?.Event as Coding)?.Code ?? string.Empty,
            MessageId = header?.Id ?? request.Message.Id ?? Guid.NewGuid().ToString(),
            BundleId = request.Message.Id,
            CorrelationId = request.CorrelationId,
            EntityType = request.EntityType,
            EntityId = request.EntityId,
            RequestIdentifierSystem = request.RequestIdentifierSystem,
            RequestIdentifierValue = request.RequestIdentifierValue,
            ProviderId = request.ProviderId,
            Status = RcmTransactionStatus.Pending,
            SentAt = DateTime.UtcNow,
        };
        _db.NphiesTransactions.Add(tx);
        await _db.SaveChangesAsync(ct);

        return await SendAndRecordAsync(tx, request.Message, new NphiesSendOptions { CorrelationId = request.CorrelationId ?? tx.Id }, ct);
    }

    public async Task<RcmDispatchResult> ResendAsync(NphiesTransaction transaction, Bundle message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        ArgumentNullException.ThrowIfNull(message);
        var header = message.Entry.FirstOrDefault()?.Resource as Hl7.Fhir.Model.MessageHeader;
        if (header?.Id != transaction.MessageId || (transaction.BundleId != null && message.Id != transaction.BundleId))
            throw new InvalidOperationException(
                $"The message to resend ({message.Id}/{header?.Id}) is not the one transaction {transaction.Id} sent ({transaction.BundleId}/{transaction.MessageId}).");

        transaction.ResendCount++;
        transaction.LastResentAt = DateTime.UtcNow;
        transaction.Status = RcmTransactionStatus.Pending;
        await _db.SaveChangesAsync(ct);

        // The body must be byte-for-byte what NPHIES may already hold, so it is sent even if today's rules would flag it.
        return await SendAndRecordAsync(transaction, message,
            new NphiesSendOptions { CorrelationId = transaction.CorrelationId ?? transaction.Id, SendWhenInvalid = true }, ct);
    }

    private async Task<RcmDispatchResult> SendAndRecordAsync(NphiesTransaction tx, Bundle message, NphiesSendOptions options, CancellationToken ct)
    {
        NphiesMessageResult? result = null;
        var errors = new List<string>();
        try
        {
            result = await _client.SendAsync(message, options, ct);
            var response = result.Response;
            tx.ResponseCode = response.ResponseCode;
            tx.ResponseMessageId = response.MessageId;
            tx.ResponseEventCode = response.EventCode;
            tx.Outcome = response.Adjudication?.Outcome ?? response.Eligibility?.Outcome ?? response.Task?.Status ?? response.PaymentReconciliation?.Outcome;
            RecordResponseFacts(tx, response);
            errors.AddRange(response.Errors.Select(e => string.IsNullOrWhiteSpace(e.Code) ? e.Message ?? "error" : $"{e.Code}: {e.Message}"));

            if (!response.IsSuccess)
                tx.Status = RcmTransactionStatus.Error;
            else if (response.IsLargeClaim
                     || string.Equals(tx.Outcome, "queued", StringComparison.OrdinalIgnoreCase)
                     || (response.Adjudication is null && response.Eligibility is null && response.Task is null
                         && response.PaymentReconciliation is null && response.InnerMessages.Count == 0
                         && tx.Operation is RcmOperations.Claim or RcmOperations.PriorAuth or RcmOperations.Batch))
                tx.Status = RcmTransactionStatus.Queued;
            else
                tx.Status = RcmTransactionStatus.Completed;
        }
        catch (NphiesValidationException ex)
        {
            tx.Status = RcmTransactionStatus.ValidationFailed;
            errors.AddRange(ex.ValidationErrors.Count > 0 ? ex.ValidationErrors : new List<string> { ex.Message });
        }
        catch (NphiesTimeoutException ex)
        {
            // The message may have been delivered: recover by resending the identical message, or by status-check/poll.
            // Never send it again with new ids.
            tx.Status = RcmTransactionStatus.Timeout;
            errors.Add(ex.Message);
        }
        catch (NphiesException ex)
        {
            tx.Status = RcmTransactionStatus.Error;
            errors.Add(ex.Message);
        }

        tx.ErrorSummary = errors.Count == 0 ? null : Truncate(string.Join(" | ", errors), 2000);
        tx.CompletedAt = DateTime.UtcNow;
        tx.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("NPHIES {Operation} message {MessageId} for {EntityType}/{EntityId} -> {Status}{Resend}",
            tx.Operation, tx.MessageId, tx.EntityType, tx.EntityId, tx.Status, tx.ResendCount > 0 ? $" (resend #{tx.ResendCount})" : "");

        var dispatch = new RcmDispatchResult { Transaction = tx, Result = result };
        dispatch.Errors.AddRange(errors);
        return dispatch;
    }

    /// <summary>
    /// Stores the error kind, adjudication outcome and meta-tags of a response on its transaction, and wakes the poll
    /// worker when NPHIES says more messages are queued. Used for synchronous replies and for messages ingested later.
    /// </summary>
    internal void RecordResponseFacts(NphiesTransaction tx, NphiesResponse response)
    {
        RecordFacts(tx, response);
        if (response.HasQueuedMessages)
            _pollSignal.Request($"queued-messages on {response.EventCode} {response.MessageId}");
    }

    /// <summary>Copies the classification facts of <paramref name="response"/> onto <paramref name="tx"/>.</summary>
    public static void RecordFacts(NphiesTransaction tx, NphiesResponse response)
    {
        tx.ErrorKind = response.ErrorKind?.ToString();
        tx.AdjudicationOutcome = response.Adjudication?.AdjudicationOutcome;
        tx.MetaTags = response.MetaTags.Count == 0 ? null : Truncate(string.Join(",", response.MetaTags), 200);
        tx.NphiesGenerated = response.IsNphiesGenerated;
    }

    private static string Truncate(string s, int max) => s.Length <= max ? s : s[..max];
}
