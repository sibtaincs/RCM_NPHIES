namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// PaymentReconciliationDetail entity - represents individual payment detail/line items
/// FHIR Resource: PaymentReconciliation.detail
/// Each detail represents a payment, adjustment, or advance for a specific claim
/// </summary>
public class PaymentReconciliationDetail : BaseEntity
{
    /// <summary>
  /// Payment Reconciliation ID this detail belongs to
    /// </summary>
    public string PaymentReconciliationId { get; set; } = string.Empty;

    /// <summary>
    /// Payment Reconciliation (navigation property)
    /// </summary>
    public PaymentReconciliation? PaymentReconciliation { get; set; }

    /// <summary>
    /// Detail type: "payment", "adjustment", "advance", "correction"
    /// FHIR: payment-type CodeSystem
    /// System: http://terminology.hl7.org/CodeSystem/payment-type
    /// </summary>
    public string DetailType { get; set; } = "payment";

    /// <summary>
    /// Request identifier system (claim request identifier)
    /// Example: "http://saudidentalclinic.com.sa/claim"
    /// </summary>
    public string? RequestIdentifierSystem { get; set; }

    /// <summary>
    /// Request identifier value (claim ID)
    /// Example: "req_299060"
 /// Links to original Claim submission
/// </summary>
    public string? RequestIdentifierValue { get; set; }

    /// <summary>
    /// Request reference URL
    /// Full reference to the claim that was submitted
    /// FHIR: PaymentReconciliation.detail.request.reference
    /// </summary>
    public string? RequestReference { get; set; }

    /// <summary>
    /// Response identifier system (claim response identifier)
    /// Example: "http://sni.com.sa/claimresponse"
    /// </summary>
    public string? ResponseIdentifierSystem { get; set; }

    /// <summary>
    /// Response identifier value (claim response ID)
    /// Example: "resp_419060"
    /// Links to ClaimResponse
/// </summary>
    public string? ResponseIdentifierValue { get; set; }

    /// <summary>
    /// Response reference URL
    /// Full reference to the claim response
    /// FHIR: PaymentReconciliation.detail.response.reference
    /// </summary>
    public string? ResponseReference { get; set; }

/// <summary>
    /// Detail date - when this payment detail was processed
    /// Example: "2021-10-07"
    /// </summary>
    public DateTime? DetailDate { get; set; }

    /// <summary>
    /// Amount for this detail
    /// Example: 193.55
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Amount currency (e.g., "SAR")
    /// Default: Saudi Riyal
    /// </summary>
    public string AmountCurrency { get; set; } = "SAR";

    /// <summary>
    /// Submitter organization ID (provider who submitted the claim)
    /// Example: "b1b3432921324f97af3be9fd0b1a14ae"
    /// </summary>
    public string? SubmitterId { get; set; }

    /// <summary>
    /// Submitter organization (navigation property)
    /// </summary>
    public Organization? Submitter { get; set; }

    /// <summary>
    /// Payee organization ID (provider receiving the payment)
    /// Usually same as Submitter for claim-based payments
    /// </summary>
    public string? PayeeId { get; set; }

    /// <summary>
    /// Payee organization (navigation property)
/// </summary>
    public Organization? Payee { get; set; }

    /// <summary>
    /// Component payment amount (base payment without fees)
    /// Example: 195.50
    /// From extension: extension-component-payment
    /// </summary>
    public decimal? ComponentPayment { get; set; }

    /// <summary>
    /// Early submission fee (discount for early submission)
 /// Example: 0 (no fee)
    /// From extension: extension-component-early-fee
  /// Usually positive for discounts, negative for penalties
    /// </summary>
    public decimal? EarlyFee { get; set; }

 /// <summary>
    /// NPHIES processing fee (charged by NPHIES)
  /// Example: -1.95 (charge)
/// From extension: extension-component-nphies-fee
    /// Usually negative (deducted from payment)
    /// </summary>
  public decimal? NphiesFee { get; set; }

    /// <summary>
    /// Notes or additional context for this detail
    /// </summary>
    public string? Notes { get; set; }

    // ===== Matching against provider claims =====
    /// <summary>PaymentReconciliation.detail.identifier (lets a later adjustment reference this line).</summary>
    public string? DetailIdentifier { get; set; }
    /// <summary>PaymentReconciliation.detail.predecessor: the earlier payment line a recoupment/adjustment reverses.</summary>
    public string? PredecessorIdentifier { get; set; }
    public string? MatchedClaimId { get; set; }
    public decimal? ExpectedAmount { get; set; }
    public decimal? VarianceAmount { get; set; }
    /// <summary>Unmatched | Matched | Underpaid | Overpaid | Recoupment | Adjustment | Advance</summary>
    public string MatchStatus { get; set; } = "Unmatched";

    /// <summary>
    /// Get reconciliation summary
    /// </summary>
    public string GetReconciliationSummary()
    {
        var summary = $"{DetailType.ToUpper()}: {Amount} {AmountCurrency} on {DetailDate:yyyy-MM-dd}";
        if (!string.IsNullOrEmpty(RequestIdentifierValue))
        {
summary += $"\n  Request: {RequestIdentifierValue}";
    }
        if (!string.IsNullOrEmpty(ResponseIdentifierValue))
        {
            summary += $"\n  Response: {ResponseIdentifierValue}";
        }
 return summary;
    }

    /// <summary>
    /// Get fee breakdown summary
    /// </summary>
    public string GetFeeBreakdown()
    {
        return $"Payment: {ComponentPayment} | Early Fee: {EarlyFee} | NPHIES Fee: {NphiesFee} = {Amount}";
    }

    /// <summary>
    /// Calculate total fees
    /// </summary>
    public decimal GetTotalFees()
    {
        return (EarlyFee ?? 0) + (NphiesFee ?? 0);
    }

    /// <summary>
    /// Validate component amounts
    /// </summary>
    public bool ValidateAmounts()
    {
        var calculated = (ComponentPayment ?? 0) + (EarlyFee ?? 0) + (NphiesFee ?? 0);
        return Math.Abs(calculated - Amount) < 0.01m; // Allow for rounding differences
    }

    /// <summary>
    /// Check if this detail has linked claim references
  /// </summary>
    public bool HasClaimReferences()
    {
        return !string.IsNullOrEmpty(RequestIdentifierValue) || 
   !string.IsNullOrEmpty(ResponseIdentifierValue);
 }

    /// <summary>
    /// Get detail type display text
    /// </summary>
    public string GetDetailTypeDisplay()
    {
        return DetailType switch
{
     "payment" => "Payment",
  "adjustment" => "Adjustment",
    "advance" => "Advance",
   "correction" => "Correction",
      _ => DetailType
    };
    }
}
