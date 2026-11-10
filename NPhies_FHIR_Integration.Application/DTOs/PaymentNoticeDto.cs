namespace NPhies_FHIR_Integration.Application.DTOs;

using NPhies_FHIR_Integration.Domain.DTOs;

/// <summary>
/// DTO for PaymentNotice entity
/// Used for API communication and data transfer
/// </summary>
public class PaymentNoticeDto : BaseDto
{
    /// <summary>
    /// Payment Notice ID
    /// </summary>
    public string PaymentNoticeId { get; set; } = string.Empty;

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
    /// Created date
    /// </summary>
public DateTime CreatedDate { get; set; }

  /// <summary>
    /// Payment date
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Payment identifier system
    /// </summary>
    public string? PaymentIdentifierSystem { get; set; }

    /// <summary>
    /// Payment identifier value
    /// </summary>
    public string? PaymentIdentifierValue { get; set; }

    /// <summary>
    /// Amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
    public string Currency { get; set; } = "SAR";

    /// <summary>
    /// Payment status
    /// </summary>
    public string? PaymentStatus { get; set; }

    /// <summary>
    /// Payment status system
    /// </summary>
    public string? PaymentStatusSystem { get; set; }

    /// <summary>
    /// Provider ID
  /// </summary>
    public string? ProviderId { get; set; }

    /// <summary>
    /// Payee ID
    /// </summary>
    public string? PayeeId { get; set; }

    /// <summary>
    /// Recipient system
    /// </summary>
    public string? RecipientSystem { get; set; }

    /// <summary>
    /// Recipient value
    /// </summary>
    public string? RecipientValue { get; set; }
}

/// <summary>
/// DTO for creating a PaymentNotice
/// </summary>
public class CreatePaymentNoticeDto
{
    /// <summary>
    /// Payment Notice ID
    /// </summary>
    public string PaymentNoticeId { get; set; } = string.Empty;

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
    /// Created date
 /// </summary>
    public DateTime CreatedDate { get; set; }

    /// <summary>
    /// Payment date
    /// </summary>
    public DateTime? PaymentDate { get; set; }

    /// <summary>
    /// Payment identifier system
    /// </summary>
    public string? PaymentIdentifierSystem { get; set; }

    /// <summary>
    /// Payment identifier value
    /// </summary>
    public string? PaymentIdentifierValue { get; set; }

    /// <summary>
  /// Amount
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Currency
    /// </summary>
  public string Currency { get; set; } = "SAR";

    /// <summary>
    /// Payment status
  /// </summary>
    public string? PaymentStatus { get; set; }

    /// <summary>
    /// Payment status system
    /// </summary>
    public string? PaymentStatusSystem { get; set; }

    /// <summary>
    /// Provider ID
    /// </summary>
    public string? ProviderId { get; set; }

    /// <summary>
    /// Payee ID
    /// </summary>
    public string? PayeeId { get; set; }

    /// <summary>
    /// Recipient system
    /// </summary>
    public string? RecipientSystem { get; set; }

    /// <summary>
    /// Recipient value
    /// </summary>
    public string? RecipientValue { get; set; }
}

/// <summary>
/// DTO for updating a PaymentNotice
/// </summary>
public class UpdatePaymentNoticeDto
{
    /// <summary>
    /// Status
 /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// Payment status
  /// </summary>
    public string? PaymentStatus { get; set; }
}
