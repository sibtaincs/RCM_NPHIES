using System.Globalization;
using System.Text.Json;
using Hl7.Fhir.Model;
using Hl7.Fhir.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Parsing;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;
using ClaimEntity = NPhies_FHIR_Integration.Domain.Entities.Claim;
using ClaimResponseEntity = NPhies_FHIR_Integration.Domain.Entities.ClaimResponse;
using CommunicationRequestEntity = NPhies_FHIR_Integration.Domain.Entities.CommunicationRequest;
using PaymentReconciliationEntity = NPhies_FHIR_Integration.Domain.Entities.PaymentReconciliation;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Application.Rcm;

public sealed class RcmIngestResult
{
    public int Processed { get; set; }
    public int Duplicates { get; set; }
    public int Unmatched { get; set; }
    public HashSet<string> ClaimIds { get; } = new();
    public HashSet<string> EligibilityCheckIds { get; } = new();
    public HashSet<string> PaymentReconciliationIds { get; } = new();
    public HashSet<string> CommunicationRequestIds { get; } = new();
    public HashSet<string> AdvancedAuthorizationIds { get; } = new();
    public List<string> Notes { get; } = new();

    public void Merge(RcmIngestResult other)
    {
        Processed += other.Processed;
        Duplicates += other.Duplicates;
        Unmatched += other.Unmatched;
        ClaimIds.UnionWith(other.ClaimIds);
        EligibilityCheckIds.UnionWith(other.EligibilityCheckIds);
        PaymentReconciliationIds.UnionWith(other.PaymentReconciliationIds);
        CommunicationRequestIds.UnionWith(other.CommunicationRequestIds);
        AdvancedAuthorizationIds.UnionWith(other.AdvancedAuthorizationIds);
        Notes.AddRange(other.Notes);
    }
}

/// <summary>
/// Applies parsed NPHIES responses (synchronous replies or poll-delivered messages) to the RCM model.
/// Idempotent: each inbound message id is recorded once in <c>NphiesTransactions</c> (direction <c>inbound</c>).
/// </summary>
public interface IRcmResponseIngestor
{
    Task<RcmIngestResult> IngestAsync(NphiesResponse response, NphiesTransaction? requestTransaction = null, CancellationToken ct = default);
}

public sealed class RcmResponseIngestor : IRcmResponseIngestor
{
    private static readonly FhirJsonSerializer Serializer = new(new SerializerSettings { Pretty = false });

    private readonly ApplicationDbContext _db;
    private readonly RcmOptions _options;
    private readonly ILogger<RcmResponseIngestor> _logger;

    public RcmResponseIngestor(ApplicationDbContext db, IOptions<RcmOptions> options, ILogger<RcmResponseIngestor> logger)
    {
        _db = db;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<RcmIngestResult> IngestAsync(NphiesResponse response, NphiesTransaction? requestTransaction = null, CancellationToken ct = default)
    {
        var result = new RcmIngestResult();

        // Poll responses wrap the deferred messages; each inner message is ingested on its own.
        if (response.InnerMessages.Count > 0)
        {
            foreach (var inner in response.InnerMessages)
                result.Merge(await IngestAsync(inner, null, ct));
            return result;
        }

        var inboundId = response.MessageId ?? response.BundleId ?? Guid.NewGuid().ToString();
        if (await _db.NphiesTransactions.AnyAsync(t => t.Direction == "inbound" && t.MessageId == inboundId, ct))
        {
            result.Duplicates++;
            result.Notes.Add($"Message {inboundId} already processed.");
            return result;
        }

        requestTransaction ??= string.IsNullOrWhiteSpace(response.RequestMessageId) ? null
            : await _db.NphiesTransactions.FirstOrDefaultAsync(t => t.Direction == "outbound" && t.MessageId == response.RequestMessageId, ct);

        var inbound = new NphiesTransaction
        {
            Direction = "inbound",
            Operation = RcmOperations.Inbound,
            EventCode = response.EventCode ?? string.Empty,
            MessageId = inboundId,
            BundleId = response.BundleId,
            CorrelationId = requestTransaction?.Id,
            ResponseCode = response.ResponseCode,
            Status = RcmTransactionStatus.Completed,
            SentAt = DateTime.UtcNow,
            CompletedAt = DateTime.UtcNow,
            ErrorSummary = response.Errors.Count == 0 ? null : Truncate(string.Join(" | ", response.Errors.Select(e => $"{e.Code}: {e.Message}")), 4000),
        };

        (string? type, string? id) target = (null, null);
        if (string.Equals(response.EventCode, NphiesMessageEvents.AdvancedAuthorization, StringComparison.OrdinalIgnoreCase))
            target = await IngestAdvancedAuthorizationAsync(response, ct);
        else if (response.Adjudication != null)
            target = await IngestAdjudicationAsync(response, requestTransaction, ct);
        else if (response.Task != null)
            target = await IngestTaskAsync(response, requestTransaction, ct);
        else if (response.PaymentReconciliation != null)
            target = await IngestPaymentReconciliationAsync(response, ct);
        else if (response.CommunicationRequest != null)
            target = await IngestCommunicationRequestAsync(response, ct);
        else if (response.Eligibility != null)
            target = await IngestEligibilityAsync(response, requestTransaction, ct);
        else if (requestTransaction?.EntityType == RcmEntityTypes.Claim && !response.IsSuccess)
            target = await MarkClaimErrorAsync(requestTransaction.EntityId!, response, ct);

        inbound.EntityType = target.type;
        inbound.EntityId = target.id;
        inbound.Outcome = response.Adjudication?.Outcome ?? response.Eligibility?.Outcome ?? response.Task?.Status ?? response.PaymentReconciliation?.Outcome;
        RcmMessageDispatcher.RecordFacts(inbound, response);
        if (target.id is null)
        {
            inbound.Status = RcmTransactionStatus.Unmatched;
            result.Unmatched++;
            result.Notes.Add($"Message {inboundId} ({response.EventCode}) could not be correlated.");
        }
        else
        {
            result.Processed++;
            switch (target.type)
            {
                case RcmEntityTypes.Claim: result.ClaimIds.Add(target.id); break;
                case RcmEntityTypes.EligibilityCheck: result.EligibilityCheckIds.Add(target.id); break;
                case RcmEntityTypes.PaymentReconciliation: result.PaymentReconciliationIds.Add(target.id); break;
                case RcmEntityTypes.CommunicationRequest: result.CommunicationRequestIds.Add(target.id); break;
                case RcmEntityTypes.AdvancedAuthorization: result.AdvancedAuthorizationIds.Add(target.id); break;
            }
        }

        _db.NphiesTransactions.Add(inbound);
        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            // Concurrent ingestion of the same message: the other writer wins.
            _db.ChangeTracker.Clear();
            return new RcmIngestResult { Duplicates = 1 };
        }

        _logger.LogInformation("Ingested NPHIES {Event} {MessageId} -> {Type}/{Id}", response.EventCode, inboundId, target.type, target.id);
        return result;
    }

    // ===================== Adjudication (claim / prior-auth responses) =====================

    private async Task<(string?, string?)> IngestAdjudicationAsync(NphiesResponse response, NphiesTransaction? tx, CancellationToken ct)
    {
        var adj = response.Adjudication!;
        var claim = await FindClaimAsync(tx, adj.RequestIdentifierSystem, adj.RequestIdentifier, ct)
                    ?? await FindBatchClaimAsync(adj.BatchIdentifier, adj.BatchNumber, ct);
        if (claim is null) return (null, null);

        var claimResponse = await _db.ClaimResponses
            .Include(r => r.Items).Include(r => r.Totals)
            .FirstOrDefaultAsync(r => r.ClaimId == claim.Id && (
                (adj.Identifier != null && r.ResponseIdentifierValue == adj.Identifier) ||
                (response.MessageId != null && r.ResponseMessageId == response.MessageId)), ct);
        if (claimResponse is null)
        {
            claimResponse = new ClaimResponseEntity { ClaimId = claim.Id };
            _db.ClaimResponses.Add(claimResponse);
        }
        else
        {
            _db.ClaimResponseItems.RemoveRange(claimResponse.Items);
            _db.RemoveRange(claimResponse.Totals);
            claimResponse.Items.Clear();
            claimResponse.Totals.Clear();
            claimResponse.UpdatedAt = DateTime.UtcNow;
        }

        var resource = response.Bundle?.Entry.Select(e => e.Resource).OfType<Hl7.Fhir.Model.ClaimResponse>().FirstOrDefault();
        claimResponse.ResponseIdentifierSystem = resource?.Identifier.FirstOrDefault()?.System;
        claimResponse.ResponseIdentifierValue = adj.Identifier ?? adj.Id;
        claimResponse.ClaimResponseStatus = resource?.Status?.ToString().ToLowerInvariant() ?? "active";
        claimResponse.ClaimType = adj.ClaimType ?? claim.ClaimType;
        claimResponse.Use = adj.Use ?? claim.Use;
        claimResponse.PatientId = claim.PatientId;
        claimResponse.InsurerId = claim.InsurerId;
        claimResponse.RequestorId = claim.ProviderId;
        claimResponse.RequestIdentifierSystem = adj.RequestIdentifierSystem;
        claimResponse.RequestIdentifierValue = adj.RequestIdentifier;
        claimResponse.PreAuthRef = adj.PreAuthRef;
        claimResponse.PreAuthPeriodStart = ParseDate(adj.PreAuthPeriodStart);
        claimResponse.PreAuthPeriodEnd = ParseDate(adj.PreAuthPeriodEnd);
        claimResponse.TransferAuthorizationNumber = Truncate(adj.TransferAuthorizationNumber, 200);
        claimResponse.TransferAuthorizationPeriodStart = ParseDate(adj.TransferAuthorizationPeriodStart);
        claimResponse.TransferAuthorizationPeriodEnd = ParseDate(adj.TransferAuthorizationPeriodEnd);
        claimResponse.TransferAuthorizationProvider = Truncate(adj.TransferAuthorizationProvider, 500);
        claimResponse.Outcome = adj.Outcome;
        claimResponse.AdjudicationOutcome = adj.AdjudicationOutcome;
        claimResponse.Disposition = Truncate(adj.Disposition, 1000);
        claimResponse.ReissueReason = Truncate(adj.ReissueReason, 30);
        claimResponse.ResponseMessageId = response.MessageId;
        claimResponse.ReceivedAt = DateTime.UtcNow;
        claimResponse.TotalSubmitted = adj.TotalOf("submitted");
        claimResponse.TotalEligible = adj.TotalOf("eligible");
        claimResponse.TotalBenefit = adj.TotalOf("benefit");
        claimResponse.TotalCopay = adj.TotalOf("copay");
        claimResponse.TotalTax = adj.TotalOf("tax");
        claimResponse.TotalPatientShare = adj.TotalOf("patientShare");
        claimResponse.PaymentAmount = adj.Payment?.Value;
        claimResponse.PaymentDate = ParseDate(adj.PaymentDate);
        claimResponse.FhirClaimResponseJson = resource is null ? null : Serializer.SerializeToString(resource);

        foreach (var total in adj.Totals.Where(t => t.Amount.HasValue))
            claimResponse.Totals.Add(new ClaimResponseTotal { ClaimResponseId = claimResponse.Id, Category = total.Category, Amount = total.Amount!.Value, Currency = total.Currency ?? "SAR" });

        // Totals are optional on item-level responses; derive them from items when missing.
        if (adj.Items.Count > 0)
        {
            claimResponse.TotalSubmitted ??= SumItems(adj, "submitted");
            claimResponse.TotalEligible ??= SumItems(adj, "eligible");
            claimResponse.TotalBenefit ??= SumItems(adj, "benefit");
            claimResponse.TotalCopay ??= SumItems(adj, "copay");
            claimResponse.TotalTax ??= SumItems(adj, "tax");
            claimResponse.TotalPatientShare ??= SumItems(adj, "patientShare");
        }

        foreach (var item in adj.Items)
        {
            var reason = item.Adjudications.FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.Reason))?.Reason;
            claimResponse.Items.Add(new ClaimResponseItem
            {
                ClaimResponseId = claimResponse.Id,
                ClaimId = claim.Id,
                ItemSequence = item.ItemSequence,
                Outcome = item.Outcome,
                Submitted = item.AmountOf("submitted"),
                Eligible = item.AmountOf("eligible"),
                Benefit = item.AmountOf("benefit"),
                Copay = item.AmountOf("copay"),
                Deductible = item.AmountOf("deductible"),
                Tax = item.AmountOf("tax"),
                PatientShare = item.AmountOf("patientShare"),
                ApprovedQuantity = item.Adjudications.FirstOrDefault(a => a.Category == "approved-quantity")?.Value,
                ReasonCode = reason,
                ReasonDisplay = reason is null ? null : response.Errors.FirstOrDefault(e => e.Code == reason)?.Message,
                AdjudicationsJson = JsonSerializer.Serialize(item.Adjudications),
            });
        }

        // Claim status
        claim.NphiesOutcome = adj.Outcome;
        claim.AdjudicationOutcome = adj.AdjudicationOutcome;
        claim.NphiesStatus = MapAdjudicationStatus(adj.Outcome, adj.AdjudicationOutcome);
        if (claim.NphiesStatus is not (RcmClaimStatus.Queued or RcmClaimStatus.Pended or RcmClaimStatus.Error))
            claim.AdjudicatedAt = DateTime.UtcNow;
        claim.ApprovedAmount = claimResponse.TotalBenefit;
        claim.PatientShareAmount = claimResponse.TotalPatientShare;
        if (claimResponse.PreAuthPeriodEnd.HasValue) claim.PreAuthPeriodEnd = claimResponse.PreAuthPeriodEnd;
        claim.UpdatedAt = DateTime.UtcNow;

        await RebuildDenialsAsync(claim, claimResponse, adj, response, ct);
        await SyncResubmissionOutcomeAsync(claim, ct);
        return (RcmEntityTypes.Claim, claim.Id);
    }

    /// <summary>When a resubmission/appeal version is adjudicated, closes the original denials and the appeal record.</summary>
    private async Task SyncResubmissionOutcomeAsync(ClaimEntity claim, CancellationToken ct)
    {
        if (claim.NphiesStatus is not (RcmClaimStatus.Approved or RcmClaimStatus.PartiallyApproved or RcmClaimStatus.Rejected)) return;
        var originals = await _db.ClaimDenials.Where(d => d.ResubmissionClaimId == claim.Id && d.ClaimId != claim.Id).ToListAsync(ct);
        if (originals.Count == 0) return;

        var stillDenied = _db.ClaimDenials.Local.Where(d => d.ClaimId == claim.Id && _db.Entry(d).State == EntityState.Added)
            .Select(d => new { d.ItemSequence, d.ReasonCode }).ToList();
        foreach (var d in originals)
        {
            var upheld = claim.NphiesStatus == RcmClaimStatus.Rejected
                || stillDenied.Any(s => s.ItemSequence == d.ItemSequence || (d.ItemSequence == null && s.ItemSequence == null));
            d.Status = upheld ? "Upheld" : "Overturned";
            d.UpdatedAt = DateTime.UtcNow;
        }

        var appealIds = originals.Where(d => d.AppealRequestId != null).Select(d => d.AppealRequestId!).Distinct().ToList();
        foreach (var appeal in await _db.AppealRequests.Where(a => appealIds.Contains(a.Id)).ToListAsync(ct))
        {
            var mine = originals.Where(d => d.AppealRequestId == appeal.Id).ToList();
            var outcome = mine.All(d => d.Status == "Overturned") ? "Overturned" : mine.Any(d => d.Status == "Overturned") ? "PartiallyOverturned" : "Upheld";
            appeal.AppealOutcome = outcome;
            appeal.AppealStatus = "Decided";
            appeal.ApprovedAmount = claim.ApprovedAmount;
            appeal.ReviewCompletedDate = DateTime.UtcNow;
            appeal.LastStatusUpdateDate = DateTime.UtcNow;
            appeal.DecisionExplanation = $"Resubmission {claim.ClaimNumber} adjudicated as {claim.AdjudicationOutcome ?? claim.NphiesStatus}.";
            _db.AppealStatusHistories.Add(new AppealStatusHistory { AppealId = appeal.Id, Status = "Decided", ChangedBy = "nphies", ChangeReason = outcome });
        }
    }

    private static decimal? SumItems(NphiesClaimAdjudicationResult adj, string category)
    {
        var values = adj.Items.Select(i => i.AmountOf(category)).Where(v => v.HasValue).ToList();
        return values.Count == 0 ? null : values.Sum(v => v!.Value);
    }

    public static string MapAdjudicationStatus(string? outcome, string? adjudicationOutcome)
    {
        switch ((outcome ?? string.Empty).ToLowerInvariant())
        {
            case "queued": return RcmClaimStatus.Queued;
            case "error": return RcmClaimStatus.Error;
        }
        return (adjudicationOutcome ?? string.Empty).ToLowerInvariant() switch
        {
            "approved" => RcmClaimStatus.Approved,
            "rejected" => RcmClaimStatus.Rejected,
            "partial" => RcmClaimStatus.PartiallyApproved,
            "pended" => RcmClaimStatus.Pended,
            // Prior authorizations only: the payer says the services need no authorization (Transaction Statuses Guideline).
            "not-required" => RcmClaimStatus.NotRequired,
            _ => string.Equals(outcome, "partial", StringComparison.OrdinalIgnoreCase) ? RcmClaimStatus.Pended : RcmClaimStatus.Approved,
        };
    }

    private async Task RebuildDenialsAsync(ClaimEntity claim, ClaimResponseEntity claimResponse, NphiesClaimAdjudicationResult adj, NphiesResponse response, CancellationToken ct)
    {
        // A new/reissued adjudication supersedes denials nobody has acted on yet.
        var open = await _db.ClaimDenials.Where(d => d.ClaimId == claim.Id && d.Status == "Open").ToListAsync(ct);
        _db.ClaimDenials.RemoveRange(open);

        if (claim.NphiesStatus is RcmClaimStatus.Queued or RcmClaimStatus.Pended or RcmClaimStatus.Error) return;

        var deadline = DateTime.UtcNow.Date.AddDays(_options.AppealWindowDays);
        var items = await _db.ClaimItems.AsNoTracking().Where(i => i.ClaimId == claim.Id).ToListAsync(ct);
        var denials = new List<ClaimDenial>();

        foreach (var item in adj.Items)
        {
            var submitted = item.AmountOf("submitted") ?? items.FirstOrDefault(i => i.Sequence == item.ItemSequence)?.Net ?? 0m;
            var eligible = item.AmountOf("eligible");
            var reason = item.Adjudications.FirstOrDefault(a => !string.IsNullOrWhiteSpace(a.Reason))?.Reason;
            var rejected = string.Equals(item.Outcome, "rejected", StringComparison.OrdinalIgnoreCase)
                           || (eligible == 0m && submitted > 0m);
            var denied = rejected ? submitted : Math.Max(0m, submitted - (eligible ?? submitted));
            if (!rejected && (reason is null || denied <= 0m)) continue;

            denials.Add(NewDenial(claim, claimResponse, item.ItemSequence, reason ?? (rejected ? "rejected" : "reduced"),
                response.Errors.FirstOrDefault(e => e.Code == reason)?.Message, submitted, denied, deadline));
        }

        if (denials.Count == 0 && claim.NphiesStatus == RcmClaimStatus.Rejected)
        {
            var err = response.Errors.FirstOrDefault();
            denials.Add(NewDenial(claim, claimResponse, null, err?.Code ?? "rejected", err?.Message ?? adj.Disposition,
                claimResponse.TotalSubmitted ?? claim.Total, claimResponse.TotalSubmitted ?? claim.Total, deadline));
        }

        await DecideSameClaimAppealsAsync(claim, denials, ct);
        _db.ClaimDenials.AddRange(denials);
    }

    /// <summary>
    /// A final adjudication of a claim with services under a re-adjudication appeal (a communication about the same
    /// claim, Claim Re-submission guide) decides them: a service the payer no longer denies is Overturned; one it still
    /// denies is Upheld, and stays the record of that denial instead of a new Open one. When the payer now denies less,
    /// the row is split into the Overturned (recovered) part and an Upheld row for what is left.
    /// </summary>
    /// <param name="denials">The denials of the new adjudication; those that continue an appealed denial are taken out.</param>
    private async Task DecideSameClaimAppealsAsync(ClaimEntity claim, List<ClaimDenial> denials, CancellationToken ct)
    {
        var carried = await CarriedDenialsAsync(claim.Id, RcmDenialStatus.Appealed, RcmDenialStatus.Upheld, ct);
        if (carried.Count == 0) return;

        var consumed = new HashSet<ClaimDenial>();
        foreach (var d in carried)
        {
            var next = denials.FirstOrDefault(n => n.ItemSequence == d.ItemSequence && !consumed.Contains(n));
            var appealedNow = d.Status == RcmDenialStatus.Appealed;
            if (next is null)
            {
                // Taken by an earlier row of the same service, or no longer denied.
                d.Status = denials.Any(n => n.ItemSequence == d.ItemSequence) ? RcmDenialStatus.Upheld : RcmDenialStatus.Overturned;
            }
            else
            {
                consumed.Add(next);
                if (next.DeniedAmount < d.DeniedAmount)
                {
                    next.Status = RcmDenialStatus.Upheld;
                    next.AppealRequestId = d.AppealRequestId;
                    d.DeniedAmount -= next.DeniedAmount;
                    d.Status = RcmDenialStatus.Overturned;
                    _db.ClaimDenials.Add(next);
                }
                else
                {
                    d.Status = RcmDenialStatus.Upheld;
                    d.ClaimResponseId = next.ClaimResponseId;
                    d.ReasonCode = next.ReasonCode;
                    d.ReasonDisplay = next.ReasonDisplay;
                    d.Category = next.Category;
                    d.SubmittedAmount = next.SubmittedAmount;
                    d.DeniedAmount = next.DeniedAmount;
                    // A further appeal after the payer's answer gets a fresh window.
                    if (appealedNow) d.AppealDeadline = next.AppealDeadline;
                }
            }
            d.UpdatedAt = DateTime.UtcNow;
        }
        denials.RemoveAll(consumed.Contains);
        await DecideAppealsAsync(carried.Select(d => d.AppealRequestId!).Distinct().ToList(),
            $"Reissued response for claim {claim.ClaimNumber}: {claim.AdjudicationOutcome ?? claim.NphiesStatus}.", claim.ApprovedAmount, ct);
    }

    /// <summary>
    /// Denials of <paramref name="claimId"/> in <paramref name="statuses"/> that belong to a re-adjudication appeal in
    /// the same claim (not to a resubmission).
    /// </summary>
    private async Task<List<ClaimDenial>> CarriedDenialsAsync(string claimId, string status1, string? status2, CancellationToken ct)
    {
        var appealIds = await _db.AppealRequests.Where(a => a.ClaimId == claimId && a.AppealMethod == RcmAppealMethod.Communication && !a.IsWithdrawn)
            .Select(a => a.Id).ToListAsync(ct);
        if (appealIds.Count == 0) return new List<ClaimDenial>();
        return await _db.ClaimDenials
            .Where(d => d.ClaimId == claimId && d.ResubmissionClaimId == null && d.AppealRequestId != null && appealIds.Contains(d.AppealRequestId)
                        && (d.Status == status1 || d.Status == status2))
            .OrderBy(d => d.ItemSequence).ThenBy(d => d.CreatedAt)
            .ToListAsync(ct);
    }

    /// <summary>Sets the outcome of each appeal from its denials: all Overturned, some recovered, or Upheld.</summary>
    private async Task DecideAppealsAsync(List<string> appealIds, string explanation, decimal? approvedAmount, CancellationToken ct)
    {
        if (appealIds.Count == 0) return;
        // Queries return the tracked instances, so the changes made above count; rows split off above are not stored yet.
        var stored = await _db.ClaimDenials.Where(d => d.AppealRequestId != null && appealIds.Contains(d.AppealRequestId)).ToListAsync(ct);
        var added = _db.ClaimDenials.Local.Where(d => _db.Entry(d).State == EntityState.Added && d.AppealRequestId != null && appealIds.Contains(d.AppealRequestId));
        var rows = stored.Where(d => _db.Entry(d).State != EntityState.Deleted).Concat(added).Distinct().ToList();
        foreach (var appeal in await _db.AppealRequests.Where(a => appealIds.Contains(a.Id)).ToListAsync(ct))
        {
            var mine = rows.Where(d => d.AppealRequestId == appeal.Id).ToList();
            if (mine.Any(d => d.Status == RcmDenialStatus.Appealed)) continue; // services still waiting for the payer.
            var recovered = mine.Where(d => d.Status == RcmDenialStatus.Overturned).Sum(d => d.DeniedAmount);
            var upheld = mine.Where(d => d.Status == RcmDenialStatus.Upheld).Sum(d => d.DeniedAmount);
            var outcome = upheld == 0m && recovered > 0m ? "Overturned" : recovered > 0m ? "PartiallyOverturned" : "Upheld";
            if (appeal.AppealStatus == "Decided" && appeal.AppealOutcome == outcome) continue;
            appeal.AppealOutcome = outcome;
            appeal.AppealStatus = "Decided";
            appeal.ApprovedAmount = approvedAmount;
            appeal.ReviewCompletedDate = DateTime.UtcNow;
            appeal.LastStatusUpdateDate = DateTime.UtcNow;
            appeal.DecisionExplanation = Truncate(explanation, 2000);
            _db.AppealStatusHistories.Add(new AppealStatusHistory { AppealId = appeal.Id, Status = "Decided", ChangedBy = "nphies", ChangeReason = outcome });
        }
    }

    private static ClaimDenial NewDenial(ClaimEntity claim, ClaimResponseEntity response, int? seq, string code, string? display, decimal submitted, decimal denied, DateTime deadline) => new()
    {
        ClaimId = claim.Id,
        ClaimResponseId = response.Id,
        ItemSequence = seq,
        ReasonCode = Truncate(code, 50)!,
        ReasonDisplay = Truncate(display, 500),
        Category = DenialCategory(code),
        SubmittedAmount = submitted,
        DeniedAmount = denied,
        Status = "Open",
        DenialDate = DateTime.UtcNow,
        AppealDeadline = deadline,
        ProviderId = claim.ProviderId,
        InsurerId = claim.InsurerId,
    };

    /// <summary>
    /// Groups NPHIES adjudication reason codes into denial categories. The five NPHIES rejection categories
    /// (IG V2.6 section 6.3.10.2) are Administrative (AD), Coverage (CV), Medical Necessity (MN),
    /// Supporting Evidence (SE) and Billing Errors (BE); the remaining buckets cover older or non-NPHIES codes.
    /// </summary>
    public static string DenialCategory(string? code)
    {
        var c = (code ?? string.Empty).ToUpperInvariant();
        if (RcmClaimRules.DuplicateCodes.Contains(c)) return "Duplicate";
        if (c.StartsWith("MN")) return "MedicalNecessity";
        if (c.StartsWith("BE")) return "BillingError";
        if (c.StartsWith("CV")) return "Coverage";
        if (c.StartsWith("AD")) return "Administrative";
        if (c.StartsWith("SE")) return "SupportingEvidence";
        if (c.StartsWith("PA") || c.Contains("AUTH")) return "Authorization";
        if (c.StartsWith("DU") || c.Contains("DUP")) return "Duplicate";
        if (c.StartsWith("N-") || c.StartsWith("CO")) return "Contractual";
        if (c == "REDUCED") return "Contractual";
        if (c.StartsWith("GE") || c.StartsWith("BV") || c.StartsWith("RE")) return "Technical";
        return "Other";
    }

    // ===================== Task (status-check / cancel responses) =====================

    private async Task<(string?, string?)> IngestTaskAsync(NphiesResponse response, NphiesTransaction? tx, CancellationToken ct)
    {
        var task = response.Task!;
        var claim = await FindClaimAsync(tx, task.FocusIdentifierSystem, task.FocusIdentifier, ct);
        if (claim is null) return (null, null);

        var completed = string.Equals(task.Status, "completed", StringComparison.OrdinalIgnoreCase);
        switch ((task.Code ?? tx?.Operation ?? string.Empty).ToLowerInvariant())
        {
            case "cancel":
            case RcmOperations.Nullify:
                if (completed && response.IsSuccess)
                {
                    var nullify = tx?.Operation == RcmOperations.Nullify;
                    claim.NphiesStatus = nullify ? RcmClaimStatus.Nullified : RcmClaimStatus.Cancelled;
                    claim.Status = nullify ? "entered-in-error" : "cancelled";
                    await _db.ClaimDenials.Where(d => d.ClaimId == claim.Id && d.Status == "Open")
                        .ForEachAsync(d => { d.Status = "WrittenOff"; d.Notes = "Claim cancelled"; }, ct);
                }
                break;
            default: // status-check
                switch ((task.ProcessingStatus ?? string.Empty).ToLowerInvariant())
                {
                    case "queued": claim.NphiesStatus = RcmClaimStatus.Queued; break;
                    case "pended": claim.NphiesStatus = RcmClaimStatus.Pended; break;
                    case "error": claim.NphiesStatus = RcmClaimStatus.Error; break;
                    case "complete" when claim.NphiesStatus is RcmClaimStatus.Timeout or RcmClaimStatus.Submitted:
                        // Payer finished adjudication; the ClaimResponse is delivered via poll.
                        claim.NphiesStatus = RcmClaimStatus.Queued;
                        break;
                }
                break;
        }
        claim.UpdatedAt = DateTime.UtcNow;
        return (RcmEntityTypes.Claim, claim.Id);
    }

    private async Task<(string?, string?)> MarkClaimErrorAsync(string claimId, NphiesResponse response, CancellationToken ct)
    {
        var claim = await _db.Claims.FirstOrDefaultAsync(c => c.Id == claimId, ct);
        if (claim is null) return (null, null);
        claim.NphiesStatus = RcmClaimStatus.Error;
        claim.UpdatedAt = DateTime.UtcNow;
        return (RcmEntityTypes.Claim, claim.Id);
    }

    // ===================== Payment reconciliation =====================

    private async Task<(string?, string?)> IngestPaymentReconciliationAsync(NphiesResponse response, CancellationToken ct)
    {
        var pr = response.PaymentReconciliation!;
        var key = pr.Id ?? pr.Identifier ?? response.MessageId ?? Guid.NewGuid().ToString();
        var existing = await _db.PaymentReconciliations.FirstOrDefaultAsync(p =>
            p.PaymentReconciliationId == key || (pr.Identifier != null && p.IdentifierValue == pr.Identifier), ct);
        if (existing != null) return (RcmEntityTypes.PaymentReconciliation, existing.Id);

        var resource = response.Bundle?.Entry.Select(e => e.Resource).OfType<Hl7.Fhir.Model.PaymentReconciliation>().FirstOrDefault();
        var paymentDate = ParseDate(pr.PaymentDate) ?? DateTime.UtcNow.Date;
        var recon = new PaymentReconciliationEntity
        {
            PaymentReconciliationId = Truncate(key, 100)!,
            IdentifierSystem = pr.IdentifierSystem,
            IdentifierValue = pr.Identifier,
            Status = resource?.Status?.ToString().ToLowerInvariant() ?? "active",
            Outcome = pr.Outcome,
            Disposition = Truncate(pr.Disposition, 1000),
            PeriodStart = ParseDate(pr.PeriodStart) ?? paymentDate,
            PeriodEnd = ParseDate(pr.PeriodEnd) ?? paymentDate,
            CreatedDate = ParseDate(resource?.Created) ?? DateTime.UtcNow,
            PaymentDate = paymentDate,
            PaymentAmount = pr.PaymentAmount?.Value ?? 0m,
            PaymentCurrency = pr.PaymentAmount?.Currency ?? "SAR",
            PaymentMethodType = pr.PaymentMethod,
            PaymentIdentifierSystem = resource?.PaymentIdentifier?.System,
            PaymentIdentifierValue = pr.PaymentIdentifier,
            FhirPaymentReconciliationJson = resource is null ? null : Serializer.SerializeToString(resource),
            MessageId = response.MessageId,
            ReceivedAt = DateTime.UtcNow,
            AcknowledgementStatus = "Pending",
        };

        var claimsTouched = new Dictionary<string, ClaimEntity>();
        foreach (var d in pr.Details)
        {
            var detail = new PaymentReconciliationDetail
            {
                PaymentReconciliationId = recon.Id,
                DetailType = d.Type ?? "payment",
                DetailIdentifier = Truncate(d.Identifier, 200),
                PredecessorIdentifier = Truncate(d.Predecessor, 200),
                RequestIdentifierValue = d.RequestIdentifier,
                ResponseIdentifierValue = d.ResponseIdentifier,
                DetailDate = ParseDate(d.Date),
                Amount = d.Amount ?? 0m,
                ComponentPayment = d.ComponentPayment,
                EarlyFee = d.ComponentEarlyFee,
                NphiesFee = d.ComponentNphiesFee,
            };

            var claim = string.IsNullOrWhiteSpace(d.RequestIdentifier) ? null
                : await _db.Claims.Where(c => c.ClaimIdentifierValue == d.RequestIdentifier && c.Use == "claim")
                    .OrderByDescending(c => c.CreatedDate).FirstOrDefaultAsync(ct);
            if (claim is null && !string.IsNullOrWhiteSpace(detail.PredecessorIdentifier))
            {
                // Recoupment/refund lines may only reference the original payment line.
                var predecessorClaimId = recon.Details.FirstOrDefault(x => x.DetailIdentifier == detail.PredecessorIdentifier)?.MatchedClaimId
                    ?? await _db.PaymentReconciliationDetails.Where(x => x.DetailIdentifier == detail.PredecessorIdentifier && x.MatchedClaimId != null)
                        .Select(x => x.MatchedClaimId).FirstOrDefaultAsync(ct);
                if (predecessorClaimId != null)
                    claim = claimsTouched.GetValueOrDefault(predecessorClaimId) ?? await _db.Claims.FirstOrDefaultAsync(c => c.Id == predecessorClaimId, ct);
            }
            if (claim != null)
            {
                // Component payment is the insurer's gross payable (benefit + VAT on benefit) before NPHIES/early fees (see example 13a vs 7b).
                var latest = await _db.ClaimResponses.Where(r => r.ClaimId == claim.Id).OrderByDescending(r => r.ReceivedAt)
                    .Select(r => new { r.TotalBenefit, r.TotalTax }).FirstOrDefaultAsync(ct);
                var expected = latest?.TotalBenefit is decimal benefit ? benefit + (latest.TotalTax ?? 0m) : claim.ApprovedAmount;
                RcmPaymentMatching.Classify(detail, claim.Id, expected, _options.PaymentMatchTolerance);
                claimsTouched[claim.Id] = claim;
                recon.PaymentIssuerId ??= claim.InsurerId;
                recon.RequestorId ??= claim.ProviderId;
            }
            else RcmPaymentMatching.ClassifyUnmatched(detail);
            recon.Details.Add(detail);
        }

        RcmPaymentMatching.RecomputeHeader(recon);

        _db.PaymentReconciliations.Add(recon);

        foreach (var claim in claimsTouched.Values)
        {
            var prior = await _db.PaymentReconciliationDetails.Where(d => d.MatchedClaimId == claim.Id)
                .Select(d => new { Paid = d.ComponentPayment ?? d.Amount, d.MatchStatus }).ToListAsync(ct);
            var current = recon.Details.Where(d => d.MatchedClaimId == claim.Id).ToList();
            claim.PaidAmount = prior.Sum(x => x.Paid) + current.Sum(RcmPaymentMatching.Paid);
            claim.PaidAt = paymentDate;
            var hasRecoupment = prior.Any(x => x.MatchStatus == RcmPaymentMatching.Recoupment) || current.Any(x => x.MatchStatus == RcmPaymentMatching.Recoupment);
            claim.NphiesStatus = RcmPaymentMatching.ClaimStatus(claim.PaidAmount.Value, claim.ApprovedAmount ?? 0m, hasRecoupment, _options.PaymentMatchTolerance);
            claim.UpdatedAt = DateTime.UtcNow;
        }

        return (RcmEntityTypes.PaymentReconciliation, recon.Id);
    }

    // ===================== Advanced (insurer-initiated) authorization =====================

    private async Task<(string?, string?)> IngestAdvancedAuthorizationAsync(NphiesResponse response, CancellationToken ct)
    {
        var bundle = response.Bundle;
        var cr = bundle?.Entry.Select(e => e.Resource).OfType<Hl7.Fhir.Model.ClaimResponse>().FirstOrDefault();
        if (cr is null) return (null, null);

        var identifier = cr.Identifier.FirstOrDefault();
        var key = cr.Id ?? identifier?.Value ?? response.MessageId ?? Guid.NewGuid().ToString();
        var existing = await _db.AdvancedAuthorizations.FirstOrDefaultAsync(a =>
            a.ClaimResponseId == key || (identifier != null && identifier.Value != null && a.IdentifierValue == identifier.Value), ct);


        Extension? Ext(string name) => cr.Extension.FirstOrDefault(e => e.Url?.EndsWith("/extension-" + name, StringComparison.OrdinalIgnoreCase) == true);
        static string? Code(DataType? v) => (v as CodeableConcept)?.Coding.FirstOrDefault()?.Code ?? (v as Coding)?.Code ?? (v as Code)?.Value;

        // The payer changes or withdraws an authorization by sending it again with a reissue reason (IG 7.9.2):
        // "cancel" withdraws it, any other reason replaces it. Without one, a repeat is only a redelivery.
        var reissue = Code(Ext("adjudication-reissue")?.Value)?.ToLowerInvariant();
        var cancels = reissue == "cancel" || cr.Status == FinancialResourceStatusCodes.Cancelled;
        if (existing != null && reissue is null && !cancels) return (RcmEntityTypes.AdvancedAuthorization, existing.Id);

        var patientResource = Resolve(bundle!, cr.Patient) as Hl7.Fhir.Model.Patient;
        var patientIdentifier = cr.Patient?.Identifier?.Value ?? patientResource?.Identifier.FirstOrDefault()?.Value;
        var insurerLicense = OrgLicense(bundle!, cr.Insurer);
        var providerLicense = OrgLicense(bundle!, Ext("serviceProvider")?.Value as ResourceReference);
        var referringLicense = OrgLicense(bundle!, Ext("referringProvider")?.Value as ResourceReference);

        var patient = string.IsNullOrWhiteSpace(patientIdentifier) ? null
            : await _db.Patients.Where(p => p.NationalId == patientIdentifier).Select(p => new { p.Id }).FirstOrDefaultAsync(ct);
        var insurer = string.IsNullOrWhiteSpace(insurerLicense) ? null
            : await _db.Organizations.Where(o => o.LicenseNumber == insurerLicense).Select(o => new { o.Id }).FirstOrDefaultAsync(ct);
        var provider = string.IsNullOrWhiteSpace(providerLicense) ? null
            : await _db.Organizations.Where(o => o.LicenseNumber == providerLicense).Select(o => new { o.Id }).FirstOrDefaultAsync(ct);

        decimal? Total(string category) => cr.Total.FirstOrDefault(t => t.Category?.Coding.Any(c => c.Code == category) == true)?.Amount?.Value;

        var items = cr.AddItem.Select((item, index) => new
        {
            Sequence = (item.Extension.FirstOrDefault(e => e.Url?.EndsWith("/extension-sequence") == true)?.Value as PositiveInt)?.Value ?? index + 1,
            Outcome = Code(item.Extension.FirstOrDefault(e => e.Url?.EndsWith("/extension-adjudication-outcome") == true)?.Value),
            System = item.ProductOrService?.Coding.FirstOrDefault()?.System,
            Code = item.ProductOrService?.Coding.FirstOrDefault()?.Code,
            Display = item.ProductOrService?.Coding.FirstOrDefault()?.Display ?? item.ProductOrService?.Text,
            Quantity = item.Quantity?.Value,
            Adjudication = item.Adjudication.Select(a => new
            {
                Category = a.Category?.Coding.FirstOrDefault()?.Code,
                Reason = a.Reason?.Coding.FirstOrDefault()?.Code,
                Amount = a.Amount?.Value,
                a.Value,
            }),
        }).ToList();

        var diagnoses = cr.Extension.Where(e => e.Url?.EndsWith("/extension-diagnosis") == true)
            .SelectMany(e => Flatten(e))
            .Select(e => e.Value as CodeableConcept).Where(c => c != null)
            .SelectMany(c => c!.Coding).Where(c => !string.IsNullOrWhiteSpace(c.Code))
            .Select(c => new { c.System, c.Code, c.Display }).ToList();

        var entity = new AdvancedAuthorization
        {
            ClaimResponseId = Truncate(key, 100)!,
            IdentifierSystem = Truncate(identifier?.System, 500),
            IdentifierValue = Truncate(identifier?.Value, 200),
            PreAuthRef = Truncate(cr.PreAuthRef, 200),
            PreAuthPeriodStart = ParseDate(cr.PreAuthPeriod?.Start),
            PreAuthPeriodEnd = ParseDate(cr.PreAuthPeriod?.End),
            Reason = Truncate(Code(Ext("advancedAuth-reason")?.Value), 50),
            ClaimType = Truncate(cr.Type?.Coding.FirstOrDefault()?.Code, 50),
            SubType = Truncate(cr.SubType?.Coding.FirstOrDefault()?.Code, 50),
            Outcome = cr.Outcome?.ToString().ToLowerInvariant(),
            AdjudicationOutcome = Truncate(Code(Ext("adjudication-outcome")?.Value), 50),
            Disposition = Truncate(cr.Disposition, 1000),
            PatientId = patient?.Id,
            PatientIdentifier = Truncate(patientIdentifier, 100),
            PatientName = Truncate(patientResource?.Name.FirstOrDefault()?.Text
                ?? string.Join(' ', (patientResource?.Name.FirstOrDefault()?.Given ?? Enumerable.Empty<string>()).Append(patientResource?.Name.FirstOrDefault()?.Family).Where(s => !string.IsNullOrWhiteSpace(s))), 300),
            InsurerId = insurer?.Id,
            InsurerLicense = Truncate(insurerLicense, 100),
            ServiceProviderId = provider?.Id,
            ServiceProviderLicense = Truncate(providerLicense, 100),
            ReferringProviderLicense = Truncate(referringLicense, 100),
            TotalEligible = Total("eligible"),
            TotalBenefit = Total("benefit"),
            Currency = cr.Total.FirstOrDefault()?.Amount?.Currency?.ToString() ?? "SAR",
            ItemsJson = JsonSerializer.Serialize(items),
            DiagnosesJson = diagnoses.Count == 0 ? null : JsonSerializer.Serialize(diagnoses),
            FhirClaimResponseJson = Serializer.SerializeToString(cr),
            Status = cancels ? "Cancelled" : "Active",
            ReissueReason = Truncate(reissue, 30),
            MessageId = response.MessageId,
            ReceivedAt = DateTime.UtcNow,
        };
        if (string.IsNullOrWhiteSpace(entity.PatientName)) entity.PatientName = null;

        if (existing != null)
        {
            // The same authorization sent again: change it in place and keep what we did with it (the claim that used it).
            if (cancels) existing.Status = "Cancelled";
            else
            {
                existing.PreAuthRef = entity.PreAuthRef ?? existing.PreAuthRef;
                existing.PreAuthPeriodStart = entity.PreAuthPeriodStart;
                existing.PreAuthPeriodEnd = entity.PreAuthPeriodEnd;
                existing.Outcome = entity.Outcome;
                existing.AdjudicationOutcome = entity.AdjudicationOutcome;
                existing.TotalEligible = entity.TotalEligible;
                existing.TotalBenefit = entity.TotalBenefit;
                existing.ItemsJson = entity.ItemsJson;
                existing.DiagnosesJson = entity.DiagnosesJson;
                existing.FhirClaimResponseJson = entity.FhirClaimResponseJson;
            }
            existing.Disposition = entity.Disposition ?? existing.Disposition;
            existing.ReissueReason = entity.ReissueReason;
            existing.MessageId = entity.MessageId;
            existing.UpdatedAt = DateTime.UtcNow;
            return (RcmEntityTypes.AdvancedAuthorization, existing.Id);
        }

        // A new response with the preAuthRef of an authorization we hold withdraws or replaces that one.
        if (!string.IsNullOrWhiteSpace(entity.PreAuthRef))
        {
            var earlier = await _db.AdvancedAuthorizations
                .Where(a => a.PreAuthRef == entity.PreAuthRef && a.InsurerLicense == entity.InsurerLicense && a.Status != "Cancelled" && a.Status != "Superseded")
                .ToListAsync(ct);
            foreach (var previous in earlier)
            {
                previous.Status = cancels ? "Cancelled" : "Superseded";
                previous.ReissueReason = entity.ReissueReason;
                previous.UpdatedAt = DateTime.UtcNow;
            }
        }
        _db.AdvancedAuthorizations.Add(entity);
        return (RcmEntityTypes.AdvancedAuthorization, entity.Id);
    }

    private static IEnumerable<Extension> Flatten(Extension e) => new[] { e }.Concat(e.Extension.SelectMany(Flatten));

    private static Resource? Resolve(Bundle bundle, ResourceReference? reference)
    {
        var r = reference?.Reference;
        if (string.IsNullOrWhiteSpace(r)) return null;
        return bundle.Entry.FirstOrDefault(e => e.FullUrl == r
            || (e.Resource != null && r.EndsWith($"{e.Resource.TypeName}/{e.Resource.Id}", StringComparison.OrdinalIgnoreCase)))?.Resource;
    }

    private static string? OrgLicense(Bundle bundle, ResourceReference? reference) =>
        reference?.Identifier?.Value ?? (Resolve(bundle, reference) as Hl7.Fhir.Model.Organization)?.Identifier.FirstOrDefault()?.Value;

    // ===================== Communication request =====================

    private async Task<(string?, string?)> IngestCommunicationRequestAsync(NphiesResponse response, CancellationToken ct)
    {
        var cr = response.CommunicationRequest!;
        var key = cr.Id ?? cr.Identifier ?? response.MessageId ?? Guid.NewGuid().ToString();
        var existing = await _db.CommunicationRequests.FirstOrDefaultAsync(c =>
            c.CommunicationRequestId == key || (cr.Identifier != null && c.IdentifierValue == cr.Identifier), ct);
        if (existing != null) return (RcmEntityTypes.CommunicationRequest, existing.Id);

        var claim = await FindClaimAsync(null, cr.AboutClaimIdentifierSystem, cr.AboutClaimIdentifier, ct);
        // A request about an advanced authorization names its identifier (IG 7.9.1).
        var authorization = claim is not null || string.IsNullOrWhiteSpace(cr.AboutClaimIdentifier) ? null
            : await _db.AdvancedAuthorizations.Where(a => a.IdentifierValue == cr.AboutClaimIdentifier)
                .OrderBy(a => a.IdentifierSystem == cr.AboutClaimIdentifierSystem ? 0 : 1).ThenByDescending(a => a.ReceivedAt)
                .FirstOrDefaultAsync(ct);
        var resource = response.Bundle?.Entry.Select(e => e.Resource).OfType<Hl7.Fhir.Model.CommunicationRequest>().FirstOrDefault();
        var receivedAt = DateTime.UtcNow;
        var entity = new CommunicationRequestEntity
        {
            CommunicationRequestId = Truncate(key, 100)!,
            IdentifierSystem = cr.IdentifierSystem,
            IdentifierValue = cr.Identifier,
            Status = cr.Status ?? "active",
            Category = cr.Category,
            Priority = cr.Priority,
            AboutResourceType = authorization is null ? "Claim" : cr.AboutType ?? "ClaimResponse",
            AboutIdentifierSystem = cr.AboutClaimIdentifierSystem,
            AboutIdentifierValue = cr.AboutClaimIdentifier,
            PayloadContent = string.Join("\n", cr.PayloadText),
            PayloadsJson = JsonSerializer.Serialize(new
            {
                text = cr.PayloadText,
                items = cr.Payloads.Where(p => p.ClaimItemSequence.HasValue && p.Text != null).Select(p => new { sequence = p.ClaimItemSequence, text = p.Text }),
                attachments = cr.Payloads.Where(p => p.Attachment != null)
                    .Select(p => new { p.Attachment!.ContentType, p.Attachment.Title, p.Attachment.Url, size = p.Attachment.Data?.Length, itemSequence = p.ClaimItemSequence }),
            }),
            Reason = cr.Reason,
            ItemSequences = RcmProjections.FormatSequences(cr.ItemSequences),
            ClaimId = claim?.Id,
            AdvancedAuthorizationId = authorization?.Id,
            SubjectPatientId = claim?.PatientId ?? authorization?.PatientId,
            SenderId = claim?.InsurerId ?? authorization?.InsurerId,
            RecipientId = claim?.ProviderId ?? authorization?.ServiceProviderId,
            MessageId = response.MessageId,
            ReceivedAt = receivedAt,
            DueAt = _options.InformationRequestDueAt(receivedAt, cr.Priority, cr.Category, ParseDate(cr.Occurrence)),
            FhirCommunicationRequestJson = resource is null ? null : Serializer.SerializeToString(resource),
        };
        _db.CommunicationRequests.Add(entity);

        // Claim Re-submission guide, step 4.2: the payer explains in a notification why it keeps the denial.
        if (claim != null && NphiesCommunicationReasons.Normalize(cr.Reason) == NphiesCommunicationReasons.ReAdjudication
            && string.Equals(cr.Category, "notification", StringComparison.OrdinalIgnoreCase))
            await UpholdReAdjudicationAsync(claim, cr.ItemSequences, cr.PayloadText, ct);
        return (RcmEntityTypes.CommunicationRequest, entity.Id);
    }

    /// <summary>
    /// The payer declined to change its decision on services under a re-adjudication appeal: they are Upheld (all of
    /// them when the notification names no service) and keep their denial, with a fresh window for a further appeal.
    /// </summary>
    private async Task UpholdReAdjudicationAsync(ClaimEntity claim, List<int> sequences, List<string> texts, CancellationToken ct)
    {
        var appealed = await CarriedDenialsAsync(claim.Id, RcmDenialStatus.Appealed, null, ct);
        var named = sequences.Count == 0 ? appealed : appealed.Where(d => d.ItemSequence.HasValue && sequences.Contains(d.ItemSequence.Value)).ToList();
        if (named.Count == 0) return;
        var deadline = DateTime.UtcNow.Date.AddDays(_options.AppealWindowDays);
        var why = texts.Count == 0 ? null : string.Join(" ", texts);
        foreach (var d in named)
        {
            d.Status = RcmDenialStatus.Upheld;
            d.AppealDeadline = deadline;
            if (why != null) d.Notes = Truncate(string.IsNullOrWhiteSpace(d.Notes) ? $"Payer: {why}" : $"{d.Notes}\nPayer: {why}", 2000);
            d.UpdatedAt = DateTime.UtcNow;
        }
        await DecideAppealsAsync(named.Select(d => d.AppealRequestId!).Distinct().ToList(),
            why ?? $"The payer kept its decision on claim {claim.ClaimNumber}.", claim.ApprovedAmount, ct);
    }

    // ===================== Eligibility =====================

    private async Task<(string?, string?)> IngestEligibilityAsync(NphiesResponse response, NphiesTransaction? tx, CancellationToken ct)
    {
        var elig = response.Eligibility!;
        EligibilityCheck? check = null;
        if (tx?.EntityType == RcmEntityTypes.EligibilityCheck)
            check = await _db.EligibilityChecks.FirstOrDefaultAsync(c => c.Id == tx.EntityId, ct);
        if (check is null && !string.IsNullOrWhiteSpace(elig.RequestIdentifier))
            check = await _db.EligibilityChecks.OrderByDescending(c => c.CreatedAt)
                .FirstOrDefaultAsync(c => c.RequestIdentifierValue == elig.RequestIdentifier, ct);
        if (check is null) return (null, null);

        ApplyEligibility(check, response);
        return (RcmEntityTypes.EligibilityCheck, check.Id);
    }

    public static void ApplyEligibility(EligibilityCheck check, NphiesResponse response)
    {
        var elig = response.Eligibility;
        check.RespondedAt = DateTime.UtcNow;
        check.ErrorsJson = response.Errors.Count == 0 ? null : JsonSerializer.Serialize(response.Errors.Select(e => new { e.Code, e.Message, e.Expression }));
        if (elig is null)
        {
            check.Status = "Error";
            return;
        }

        var insurance = elig.Insurance.FirstOrDefault();
        check.Status = string.Equals(elig.Outcome, "error", StringComparison.OrdinalIgnoreCase) || !response.IsSuccess ? "Error" : "Completed";
        check.Outcome = elig.Outcome;
        check.IsEligible = elig.IsEligible;
        check.InForce = insurance?.InForce;
        check.SiteEligibility = elig.SiteEligibility;
        check.Disposition = Truncate(elig.Disposition, 1000);
        check.NotInForceReason = insurance?.NotInForceReason;
        check.BenefitPeriodStart = ParseDate(insurance?.BenefitPeriodStart);
        check.BenefitPeriodEnd = ParseDate(insurance?.BenefitPeriodEnd);
        check.ResponseIdentifier = elig.Identifier;
        check.ResponseIdentifierSystem = elig.IdentifierSystem;
        check.BenefitsJson = elig.Insurance.Count == 0 ? null : JsonSerializer.Serialize(elig.Insurance.Select(i => new
        {
            i.InForce,
            i.NotInForceReason,
            // Per policy when the payer gives it; otherwise the header value applies to the only policy.
            SiteEligibility = i.SiteEligibility ?? (elig.Insurance.Count == 1 ? elig.SiteEligibility : null),
            SiteEligibilityReason = RcmEligibilityGuidance.SiteEligibilityReason(i.SiteEligibility ?? (elig.Insurance.Count == 1 ? elig.SiteEligibility : null)),
            i.BenefitPeriodStart,
            i.BenefitPeriodEnd,
            Items = i.Items.Select(b => new
            {
                b.Category, b.ProductOrService, b.Name, b.Description, b.Network, b.Unit, b.Term, b.Excluded, b.AuthorizationRequired,
                Benefits = b.Benefits.Select(x => new { x.Type, x.AllowedMoney, x.UsedMoney, x.Allowed, x.Used }),
            }),
        }));
        check.DiscoveredCoveragesJson = elig.Coverages.Count == 0 ? null : JsonSerializer.Serialize(elig.Coverages.Select(c => new
        {
            c.Id,
            Status = c.Status?.ToString(),
            MemberId = c.Identifier.FirstOrDefault()?.Value,
            MemberIdSystem = c.Identifier.FirstOrDefault()?.System,
            Type = c.Type?.Coding.FirstOrDefault()?.Code,
            Payor = c.Payor.FirstOrDefault()?.Reference ?? c.Payor.FirstOrDefault()?.Display,
            Relationship = c.Relationship?.Coding.FirstOrDefault()?.Code,
            PeriodStart = c.Period?.Start,
            PeriodEnd = c.Period?.End,
            Classes = c.Class.Select(k => new { Type = k.Type?.Coding.FirstOrDefault()?.Code, k.Value, k.Name }),
        }));
    }

    // ===================== helpers =====================

    private async Task<ClaimEntity?> FindClaimAsync(NphiesTransaction? tx, string? system, string? value, CancellationToken ct)
    {
        if (tx?.EntityType == RcmEntityTypes.Claim && tx.EntityId != null)
        {
            var byTx = await _db.Claims.FirstOrDefaultAsync(c => c.Id == tx.EntityId, ct);
            if (byTx != null && (value is null || byTx.ClaimIdentifierValue == value)) return byTx;
        }
        if (string.IsNullOrWhiteSpace(value)) return null;

        var candidates = await _db.Claims.Where(c => c.ClaimIdentifierValue == value)
            .OrderByDescending(c => c.CreatedDate).ToListAsync(ct);
        return candidates.FirstOrDefault(c => system != null && string.Equals(c.ClaimIdentifierSystem, system, StringComparison.OrdinalIgnoreCase))
               ?? candidates.FirstOrDefault();
    }

    /// <summary>
    /// A deferred response to a batch claim, when its request identifier does not find the claim: the batch-identifier
    /// and batch-number extensions do (Claims guide, batch).
    /// </summary>
    private async Task<ClaimEntity?> FindBatchClaimAsync(string? batchIdentifier, int? batchNumber, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(batchIdentifier) || batchNumber is null) return null;
        return await _db.Claims.Where(c => c.BatchIdentifier == batchIdentifier && c.BatchNumber == batchNumber)
            .OrderByDescending(c => c.CreatedDate).FirstOrDefaultAsync(ct);
    }

    private static DateTime? ParseDate(string? value) =>
        DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var d) ? d : null;

    private static string? Truncate(string? s, int max) => s is null || s.Length <= max ? s : s[..max];

    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("duplicate key", StringComparison.OrdinalIgnoreCase) == true
        || ex.InnerException?.Message.Contains("UNIQUE", StringComparison.OrdinalIgnoreCase) == true;
}
