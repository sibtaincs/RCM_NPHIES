namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// PaymentNotice entity - represents payment acknowledgment sent by provider to NPHIES
/// FHIR Resource: PaymentNotice
/// Used for payment confirmation and tracking
/// </summary>
public class PaymentNotice : BaseEntity
{
    /// <summary>
    /// Payment Notice ID (FHIR resource ID)
    /// Example: "99060"
    /// </summary>
    public string PaymentNoticeId { get; set; } = string.Empty;

    /// <summary>
    /// Identifier system (e.g., "http://sni.com.sa/paymentnotice")
    /// System URL for the notice identifier
    /// </summary>
public string? IdentifierSystem { get; set; }

    /// <summary>
    /// Identifier value (e.g., "929060")
    /// Unique identifier within the system
    /// </summary>
    public string? IdentifierValue { get; set; }

    /// <summary>
    /// Status: "active", "cancelled", "draft"
    /// FHIR: FinancialResourceStatusCodes
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// Created date - when the payment notice was created
    /// Example: "2022-02-14"
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Payment date - when the payment was made/cleared
    /// Example: "2021-11-02"
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Payment identifier system (link to PaymentReconciliation)
    /// Example: "http://sni.com.sa/paymentreconciliation"
    /// </summary>
    public string? PaymentIdentifierSystem { get; set; }

    /// <summary>
    /// Payment identifier value (PaymentReconciliation ID)
    /// Example: "929060"
    /// Links to the PaymentReconciliation entity
    /// </summary>
 public string? PaymentIdentifierValue { get; set; }

    /// <summary>
  /// Payment amount acknowledged
    /// Example: 193.55
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Payment currency (e.g., "SAR")
    /// Default: Saudi Riyal
    /// </summary>
    public string Currency { get; set; } = "SAR";

    /// <summary>
    /// Payment status: "cleared", "pending", "error"
    /// FHIR: paymentstatus CodeSystem
    /// System: http://terminology.hl7.org/CodeSystem/paymentstatus
    /// </summary>
public string? PaymentStatus { get; set; }

    /// <summary>
 /// Payment status system URL
    /// Example: "http://terminology.hl7.org/CodeSystem/paymentstatus"
  /// </summary>
    public string? PaymentStatusSystem { get; set; }

    /// <summary>
    /// Provider organization ID (who is sending the notice)
    /// FHIR: PaymentNotice.provider.reference
    /// </summary>
    public string? ProviderId { get; set; }

    /// <summary>
    /// Provider organization (navigation property)
    /// </summary>
    public Organization? Provider { get; set; }

    /// <summary>
    /// Payee organization ID (recipient of the payment)
    /// Usually same as Provider
    /// FHIR: PaymentNotice.payee.reference
    /// </summary>
    public string? PayeeId { get; set; }

    /// <summary>
    /// Payee organization (navigation property)
    /// </summary>
    public Organization? Payee { get; set; }

    /// <summary>
    /// Recipient identifier system (usually NPHIES)
    /// Example: "http://nphies.sa/license/nphies"
    /// </summary>
    public string? RecipientSystem { get; set; }

    /// <summary>
    /// Recipient identifier value (usually "NPHIES")
    /// The recipient of this payment notice
    /// </summary>
    public string? RecipientValue { get; set; }

    /// <summary>
    /// FHIR PaymentNotice JSON for storage (backup/reference)
    /// Stores complete FHIR PaymentNotice resource as JSON
    /// </summary>
    public string? FhirPaymentNoticeJson { get; set; }

    /// <summary>
    /// Get payment notice summary
    /// </summary>
    public string GetSummary()
    {
        return $"Payment Notice: {Amount} {Currency} - Status: {PaymentStatus} (ID: {PaymentNoticeId})";
    }

    /// <summary>
    /// Check if payment is cleared
    /// </summary>
    public bool IsCleared()
    {
        return PaymentStatus == "cleared" && Status == "active";
    }

/// <summary>
    /// Check if payment is pending
    /// </summary>
    public bool IsPending()
    {
        return PaymentStatus == "pending";
    }

    /// <summary>
    /// Check if payment has error
    /// </summary>
    public bool HasError()
    {
        return PaymentStatus == "error";
    }

    /// <summary>
    /// Get payment status display text
    /// </summary>
    public string GetPaymentStatusDisplay()
    {
        return PaymentStatus switch
        {
            "cleared" => "Payment Cleared",
            "pending" => "Payment Pending",
"error" => "Payment Error",
     _ => PaymentStatus ?? "Unknown"
        };
    }

    /// <summary>
    /// Get full notice information
    /// </summary>
    public string GetFullInfo()
    {
        return $"""
Payment Notice: {PaymentNoticeId}
Status: {Status}
Payment Status: {GetPaymentStatusDisplay()}
Amount: {Amount} {Currency}
Payment Date: {PaymentDate:yyyy-MM-dd}
Created: {CreatedDate:yyyy-MM-dd}
Recipient: {RecipientValue ?? "Unknown"}
Provider: {(Provider?.OrganizationName ?? "Unknown")}
""";
    }
}
