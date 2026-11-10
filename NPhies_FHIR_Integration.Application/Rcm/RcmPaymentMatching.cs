using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Application.Rcm;

/// <summary>
/// Matching rules shared by payment ingestion and manual matching.
/// <para>
/// NPHIES sends recoupments and refunds as <c>PaymentReconciliation.detail</c> lines of type <c>adjustment</c>
/// (usually with a negative amount and a <c>predecessor</c> pointing at the original payment line). They are not
/// compared to the adjudicated amount; they reduce the claim's paid amount instead.
/// </para>
/// </summary>
public static class RcmPaymentMatching
{
    public const string Unmatched = "Unmatched";
    public const string Matched = "Matched";
    public const string Underpaid = "Underpaid";
    public const string Overpaid = "Overpaid";
    public const string Recoupment = "Recoupment";
    public const string Adjustment = "Adjustment";
    public const string Advance = "Advance";

    public static decimal Paid(PaymentReconciliationDetail d) => d.ComponentPayment ?? d.Amount;

    public static bool IsAdjustment(PaymentReconciliationDetail d) =>
        d.DetailType is "adjustment" or "correction" || !string.IsNullOrWhiteSpace(d.PredecessorIdentifier) || Paid(d) < 0m;

    /// <summary>Sets expected/variance/status of a line matched to a claim whose payable amount is <paramref name="expected"/>.</summary>
    public static void Classify(PaymentReconciliationDetail d, string claimId, decimal? expected, decimal tolerance)
    {
        var paid = Paid(d);
        d.MatchedClaimId = claimId;
        if (IsAdjustment(d))
        {
            d.ExpectedAmount = null;
            d.VarianceAmount = paid;
            d.MatchStatus = paid < 0m ? Recoupment : Adjustment;
            return;
        }
        d.ExpectedAmount = expected;
        d.VarianceAmount = expected.HasValue ? paid - expected.Value : null;
        d.MatchStatus = !expected.HasValue || Math.Abs(paid - expected.Value) <= tolerance ? Matched
            : paid < expected.Value ? Underpaid : Overpaid;
    }

    /// <summary>Lines that are not tied to a claim by design (advance payments).</summary>
    public static void ClassifyUnmatched(PaymentReconciliationDetail d) =>
        d.MatchStatus = d.DetailType == "advance" ? Advance : Unmatched;

    public static void RecomputeHeader(PaymentReconciliation recon)
    {
        var payments = recon.Details.Where(d => d.MatchedClaimId != null && !IsAdjustment(d)).ToList();
        recon.ExpectedAmount = payments.Where(d => d.ExpectedAmount.HasValue).Sum(d => d.ExpectedAmount!.Value);
        recon.VarianceAmount = payments.Sum(Paid) - recon.ExpectedAmount;
        var statuses = recon.Details.Select(d => d.MatchStatus).ToList();
        recon.MatchStatus =
            statuses.Count == 0 || statuses.All(s => s is Unmatched) ? Unmatched
            : statuses.Any(s => s is Unmatched) ? "PartiallyMatched"
            : statuses.Any(s => s is Underpaid or Overpaid) ? "Discrepancy"
            : statuses.Any(s => s is Recoupment) ? Recoupment
            : Matched;
        recon.UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>Claim status from the net amount paid (payments minus recoupments).</summary>
    public static string ClaimStatus(decimal netPaid, decimal approved, bool hasRecoupment, decimal tolerance) =>
        hasRecoupment && netPaid <= tolerance ? RcmClaimStatus.Recouped
        : netPaid + tolerance >= approved ? RcmClaimStatus.Paid
        : RcmClaimStatus.PartiallyPaid;
}
