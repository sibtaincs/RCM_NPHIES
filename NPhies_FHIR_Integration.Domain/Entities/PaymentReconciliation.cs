namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// PaymentReconciliation entity - represents payment details sent by insurer to provider
/// FHIR Resource: PaymentReconciliation
/// Used for financial tracking and payment reconciliation between insurers and providers
/// </summary>
public class PaymentReconciliation : BaseEntity
{
    /// <summary>
    /// Payment Reconciliation ID (FHIR resource ID)
    /// Example: "99060"
    /// </summary>
    public string PaymentReconciliationId { get; set; } = string.Empty;

    /// <summary>
    /// Identifier system (e.g., "http://sni.com.sa/paymentreconciliation")
    /// System URL for the reconciliation identifier
    /// </summary>
    public string? IdentifierSystem { get; set; }

    /// <summary>
    /// Identifier value (e.g., "929060")
    /// Unique identifier within the system
    /// </summary>
    public string? IdentifierValue { get; set; }

    /// <summary>
    /// Status: "active", "cancelled", "draft", "entered-in-error"
    /// FHIR: FinancialResourceStatusCodes
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// Outcome: "complete", "partial", "error", "pending"
    /// Indicates success or failure of payment
    /// </summary>
 public string? Outcome { get; set; }

    /// <summary>
  /// Disposition - description of the payment
    /// Example: "Payment made for the month of October, 2021"
    /// </summary>
    public string? Disposition { get; set; }

    /// <summary>
    /// Period start date - beginning of the reconciliation period
    /// Example: "2021-10-01"
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Period end date - end of the reconciliation period
    /// Example: "2021-10-31"
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Created date - when the reconciliation was created
    /// Example: "2022-02-14"
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Payment date - when the payment was made
    /// Example: "2021-11-02"
    /// </summary>
  public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Payment amount
    /// Example: 193.55
    /// </summary>
  public decimal PaymentAmount { get; set; }

    /// <summary>
    /// Payment currency (e.g., "SAR")
    /// Default: Saudi Riyal
    /// </summary>
    public string PaymentCurrency { get; set; } = "SAR";

    /// <summary>
 /// Payment method type (e.g., "eft", "check", "wire", "card")
    /// System: http://nphies.sa/terminology/CodeSystem/payment-method
    /// </summary>
    public string? PaymentMethodType { get; set; }

    /// <summary>
    /// Payment method system URL
    /// Example: "http://nphies.sa/terminology/CodeSystem/payment-method"
/// </summary>
    public string? PaymentMethodSystem { get; set; }

    /// <summary>
  /// Payment identifier system (e.g., bank transfer reference system)
    /// Example: "http://saudinationalbank/payment-45908"
    /// </summary>
    public string? PaymentIdentifierSystem { get; set; }

    /// <summary>
    /// Payment identifier value (e.g., bank transaction reference)
    /// Example: "URN-987590432322"
    /// </summary>
    public string? PaymentIdentifierValue { get; set; }

    /// <summary>
    /// Payment issuer organization ID (insurer sending the payment)
    /// FHIR: PaymentReconciliation.paymentIssuer.reference
    /// </summary>
    public string? PaymentIssuerId { get; set; }

    /// <summary>
  /// Payment issuer organization (navigation property)
    /// </summary>
    public Organization? PaymentIssuer { get; set; }

    /// <summary>
    /// Requestor organization ID (provider receiving the payment)
    /// FHIR: PaymentReconciliation.requestor.reference
    /// </summary>
    public string? RequestorId { get; set; }

    /// <summary>
    /// Requestor organization (navigation property)
    /// </summary>
    public Organization? Requestor { get; set; }

    /// <summary>
    /// FHIR PaymentReconciliation JSON for storage (backup/reference)
    /// Stores complete FHIR PaymentReconciliation resource as JSON
    /// </summary>
    public string? FhirPaymentReconciliationJson { get; set; }

    // ===== NPHIES RCM ingestion / acknowledgement =====
    /// <summary>MessageHeader id of the inbound payment-reconciliation message (idempotency key).</summary>
    public string? MessageId { get; set; }
    public DateTime? ReceivedAt { get; set; }
    /// <summary>Unmatched | Matched | Discrepancy</summary>
    public string MatchStatus { get; set; } = "Unmatched";
    public decimal? ExpectedAmount { get; set; }
    public decimal? VarianceAmount { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public string? PaymentNoticeMessageId { get; set; }
    /// <summary>Pending | Sent | Failed | Timeout</summary>
    public string? AcknowledgementStatus { get; set; }
    public string? AcknowledgementError { get; set; }

    /// <summary>
    /// Collection of payment reconciliation details
    /// Each detail represents a payment or adjustment item
    /// </summary>
  public ICollection<PaymentReconciliationDetail> Details { get; set; } 
        = new List<PaymentReconciliationDetail>();

    /// <summary>
    /// Get payment summary text
    /// </summary>
    public string GetPaymentSummary()
    {
        return $"{PaymentAmount} {PaymentCurrency} via {PaymentMethodType ?? "unknown"} on {PaymentDate:yyyy-MM-dd}";
    }

    /// <summary>
    /// Get period description
    /// </summary>
    public string GetPeriodDescription()
    {
        return $"{PeriodStart:MMM yyyy} to {PeriodEnd:MMM yyyy}";
    }

    /// <summary>
    /// Check if payment is complete
    /// </summary>
    public bool IsComplete()
    {
        return Outcome == "complete" && Status == "active";
    }

    /// <summary>
    /// Calculate total detail amount
    /// </summary>
    public decimal GetTotalDetailAmount()
    {
        return Details.Sum(d => d.Amount);
    }

    /// <summary>
    /// Get outcome display text
    /// </summary>
    public string GetOutcomeDisplay()
    {
        return Outcome switch
        {
 "complete" => "Payment Complete",
       "partial" => "Partial Payment",
       "error" => "Payment Error",
            "pending" => "Payment Pending",
            _ => Outcome ?? "Unknown"
        };
    }
}
