namespace NPhies_FHIR_Integration.Application.DTOs;

using NPhies_FHIR_Integration.Domain.DTOs;

/// <summary>
/// DTO for PaymentReconciliation entity
/// Used for API communication and data transfer
/// </summary>
public class PaymentReconciliationDto : BaseDto
{
    /// <summary>
    /// Payment Reconciliation ID
    /// </summary>
    public string PaymentReconciliationId { get; set; } = string.Empty;

    /// <summary>
    /// Identifier system
    /// </summary>
    public string? IdentifierSystem { get; set; }

    /// <summary>
 /// Identifier value
    /// </summary>
    public string? IdentifierValue { get; set; }

    /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// Outcome
 /// </summary>
    public string? Outcome { get; set; }

    /// <summary>
    /// Disposition
    /// </summary>
    public string? Disposition { get; set; }

    /// <summary>
    /// Period start
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Period end
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Payment date
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Payment amount
    /// </summary>
    public decimal PaymentAmount { get; set; }

    /// <summary>
    /// Payment currency
    /// </summary>
    public string PaymentCurrency { get; set; } = "SAR";

    /// <summary>
    /// Payment method type
    /// </summary>
    public string? PaymentMethodType { get; set; }

    /// <summary>
    /// Payment method system
    /// </summary>
    public string? PaymentMethodSystem { get; set; }

    /// <summary>
    /// Payment identifier system
    /// </summary>
    public string? PaymentIdentifierSystem { get; set; }

    /// <summary>
    /// Payment identifier value
    /// </summary>
    public string? PaymentIdentifierValue { get; set; }

    /// <summary>
    /// Payment issuer ID
    /// </summary>
    public string? PaymentIssuerId { get; set; }

    /// <summary>
/// Requestor ID
    /// </summary>
    public string? RequestorId { get; set; }

    /// <summary>
    /// Payment reconciliation details
    /// </summary>
    public List<PaymentReconciliationDetailDto> Details { get; set; } = new();
}

/// <summary>
/// DTO for creating a PaymentReconciliation
/// </summary>
public class CreatePaymentReconciliationDto
{
    /// <summary>
    /// Payment Reconciliation ID
    /// </summary>
    public string PaymentReconciliationId { get; set; } = string.Empty;

    /// <summary>
    /// Identifier system
    /// </summary>
    public string? IdentifierSystem { get; set; }

    /// <summary>
    /// Identifier value
    /// </summary>
    public string? IdentifierValue { get; set; }

 /// <summary>
    /// Status
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// Outcome
    /// </summary>
    public string? Outcome { get; set; }

    /// <summary>
    /// Disposition
    /// </summary>
    public string? Disposition { get; set; }

    /// <summary>
    /// Period start
    /// </summary>
    public DateTime PeriodStart { get; set; }

    /// <summary>
    /// Period end
    /// </summary>
    public DateTime PeriodEnd { get; set; }

    /// <summary>
    /// Created date
    /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Payment date
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Payment amount
/// </summary>
    public decimal PaymentAmount { get; set; }

    /// <summary>
    /// Payment currency
    /// </summary>
    public string PaymentCurrency { get; set; } = "SAR";

    /// <summary>
    /// Payment method type
    /// </summary>
    public string? PaymentMethodType { get; set; }

    /// <summary>
    /// Payment method system
    /// </summary>
 public string? PaymentMethodSystem { get; set; }

    /// <summary>
    /// Payment identifier system
    /// </summary>
    public string? PaymentIdentifierSystem { get; set; }

    /// <summary>
    /// Payment identifier value
    /// </summary>
    public string? PaymentIdentifierValue { get; set; }

    /// <summary>
    /// Payment issuer ID
    /// </summary>
    public string? PaymentIssuerId { get; set; }

    /// <summary>
    /// Requestor ID
    /// </summary>
    public string? RequestorId { get; set; }
}

/// <summary>
/// DTO for updating a PaymentReconciliation
/// </summary>
public class UpdatePaymentReconciliationDto
{
    /// <summary>
    /// Status
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Outcome
    /// </summary>
 public string? Outcome { get; set; }

    /// <summary>
    /// Disposition
    /// </summary>
    public string? Disposition { get; set; }
}

/// <summary>
/// DTO for PaymentReconciliationDetail entity
/// </summary>
public class PaymentReconciliationDetailDto : BaseDto
{
  /// <summary>
    /// Payment Reconciliation ID
    /// </summary>
    public string PaymentReconciliationId { get; set; } = string.Empty;

    /// <summary>
    /// Detail type
  /// </summary>
    public string DetailType { get; set; } = "payment";

    /// <summary>
    /// Request identifier system
    /// </summary>
    public string? RequestIdentifierSystem { get; set; }

  /// <summary>
    /// Request identifier value
    /// </summary>
 public string? RequestIdentifierValue { get; set; }

    /// <summary>
    /// Request reference
    /// </summary>
 public string? RequestReference { get; set; }

    /// <summary>
    /// Response identifier system
    /// </summary>
    public string? ResponseIdentifierSystem { get; set; }

    /// <summary>
    /// Response identifier value
    /// </summary>
    public string? ResponseIdentifierValue { get; set; }

    /// <summary>
    /// Response reference
    /// </summary>
    public string? ResponseReference { get; set; }

    /// <summary>
    /// Detail date
    /// </summary>
    public DateTime? DetailDate { get; set; }

  /// <summary>
    /// Amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Amount currency
    /// </summary>
    public string AmountCurrency { get; set; } = "SAR";

    /// <summary>
    /// Submitter ID
    /// </summary>
    public string? SubmitterId { get; set; }

    /// <summary>
    /// Payee ID
    /// </summary>
    public string? PayeeId { get; set; }

    /// <summary>
    /// Component payment
    /// </summary>
    public decimal? ComponentPayment { get; set; }

    /// <summary>
    /// Early fee
    /// </summary>
    public decimal? EarlyFee { get; set; }

    /// <summary>
    /// NPHIES fee
    /// </summary>
    public decimal? NphiesFee { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }
}

/// <summary>
/// DTO for creating a PaymentReconciliationDetail
/// </summary>
public class CreatePaymentReconciliationDetailDto
{
    /// <summary>
    /// Payment Reconciliation ID
    /// </summary>
    public string PaymentReconciliationId { get; set; } = string.Empty;

    /// <summary>
    /// Detail type
    /// </summary>
    public string DetailType { get; set; } = "payment";

    /// <summary>
    /// Request identifier system
    /// </summary>
    public string? RequestIdentifierSystem { get; set; }

    /// <summary>
    /// Request identifier value
    /// </summary>
    public string? RequestIdentifierValue { get; set; }

  /// <summary>
    /// Request reference
    /// </summary>
    public string? RequestReference { get; set; }

 /// <summary>
    /// Response identifier system
    /// </summary>
    public string? ResponseIdentifierSystem { get; set; }

    /// <summary>
    /// Response identifier value
    /// </summary>
    public string? ResponseIdentifierValue { get; set; }

    /// <summary>
    /// Response reference
    /// </summary>
    public string? ResponseReference { get; set; }

    /// <summary>
    /// Detail date
    /// </summary>
    public DateTime? DetailDate { get; set; }

    /// <summary>
    /// Amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Amount currency
    /// </summary>
    public string AmountCurrency { get; set; } = "SAR";

    /// <summary>
    /// Submitter ID
    /// </summary>
    public string? SubmitterId { get; set; }

    /// <summary>
    /// Payee ID
    /// </summary>
    public string? PayeeId { get; set; }

    /// <summary>
    /// Component payment
  /// </summary>
    public decimal? ComponentPayment { get; set; }

    /// <summary>
    /// Early fee
    /// </summary>
    public decimal? EarlyFee { get; set; }

    /// <summary>
    /// NPHIES fee
    /// </summary>
    public decimal? NphiesFee { get; set; }

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }
}
