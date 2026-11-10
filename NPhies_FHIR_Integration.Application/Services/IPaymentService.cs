using NPhies_FHIR_Integration.Application.Services;

namespace NPhies_FHIR_Integration.Application.Services;

/// <summary>
/// Payment Service Interface
/// Handles payment calculations and summaries for claims
/// </summary>
public interface IPaymentService
{
  /// <summary>
    /// Calculate payment for a claim
    /// </summary>
    /// <param name="request">Payment calculation request</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment calculation response</returns>
    Task<PaymentCalculationResponse> CalculatePaymentAsync(
     PaymentCalculationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get payment summary for a claim
/// </summary>
    /// <param name="claimId">Claim ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Payment summary</returns>
    Task<PaymentSummary?> GetPaymentSummaryAsync(
 int claimId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get detailed payment information for a claim
    /// </summary>
    /// <param name="claimId">Claim ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
  /// <returns>Payment details</returns>
    Task<PaymentDetails?> GetPaymentDetailsAsync(
        int claimId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Save payment calculation result
    /// </summary>
    /// <param name="calculation">Payment calculation data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Task</returns>
    Task SaveCalculationAsync(
        ClaimPaymentCalculation calculation,
    CancellationToken cancellationToken = default);
}

/// <summary>
/// Payment calculation request
/// </summary>
public class PaymentCalculationRequest
{
    /// <summary>
    /// Claim ID to calculate payment for
    /// </summary>
    public int ClaimId { get; set; }

    /// <summary>
    /// Coverage ID (optional, uses claim's coverage if not provided)
    /// </summary>
    public int? CoverageId { get; set; }

    /// <summary>
    /// Force recalculation even if already calculated
    /// </summary>
    public bool ForceRecalculation { get; set; } = false;
}

/// <summary>
/// Payment calculation response
/// </summary>
public class PaymentCalculationResponse
{
    /// <summary>
    /// Claim ID
    /// </summary>
    public int ClaimId { get; set; }

    /// <summary>
    /// Whether calculation was successful
    /// </summary>
    public bool IsSuccessful { get; set; }

    /// <summary>
    /// Error message if calculation failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Validation errors if any
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Total submitted amount
    /// </summary>
    public decimal TotalSubmittedAmount { get; set; }

    /// <summary>
    /// Total allowed amount
    /// </summary>
    public decimal TotalAllowedAmount { get; set; }

    /// <summary>
    /// Total insurance responsibility
    /// </summary>
    public decimal TotalInsuranceResponsibility { get; set; }

    /// <summary>
    /// Total patient responsibility
    /// </summary>
    public decimal TotalPatientResponsibility { get; set; }

    /// <summary>
    /// Calculation timestamp
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Payment summary
/// </summary>
public class PaymentSummary
{
    /// <summary>
    /// Claim ID
    /// </summary>
    public int ClaimId { get; set; }

    /// <summary>
 /// Total submitted amount
    /// </summary>
    public decimal TotalSubmittedAmount { get; set; }

    /// <summary>
  /// Total allowed amount
    /// </summary>
    public decimal TotalAllowedAmount { get; set; }

    /// <summary>
    /// Total denied amount
    /// </summary>
    public decimal TotalDeniedAmount { get; set; }

    /// <summary>
    /// Total insurance pays
    /// </summary>
  public decimal TotalInsurancePays { get; set; }

    /// <summary>
    /// Total patient pays
    /// </summary>
    public decimal TotalPatientPays { get; set; }

    /// <summary>
    /// Deductible applied
    /// </summary>
    public decimal DeductibleApplied { get; set; }

    /// <summary>
    /// Deductible remaining
    /// </summary>
    public decimal DeductibleRemaining { get; set; }

  /// <summary>
    /// Coinsurance applied
    /// </summary>
    public decimal CoinsuranceApplied { get; set; }

    /// <summary>
    /// Out-of-pocket applied
    /// </summary>
    public decimal OutOfPocketApplied { get; set; }

    /// <summary>
    /// Calculation date
    /// </summary>
    public DateTime CalculatedAt { get; set; }

    /// <summary>
    /// Item count
    /// </summary>
    public int ItemCount { get; set; }
}

/// <summary>
/// Detailed payment information
/// </summary>
public class PaymentDetails
{
    /// <summary>
    /// Claim ID
    /// </summary>
  public int ClaimId { get; set; }

    /// <summary>
    /// Payment summary
    /// </summary>
    public PaymentSummary Summary { get; set; } = new();

    /// <summary>
    /// Item-level payment details
    /// </summary>
    public List<ItemPaymentDetail> ItemDetails { get; set; } = new();

    /// <summary>
    /// Notes and comments
/// </summary>
    public List<string> Notes { get; set; } = new();
}

/// <summary>
/// Item-level payment detail
/// </summary>
public class ItemPaymentDetail
{
    /// <summary>
    /// Item sequence number
    /// </summary>
    public int ItemSequence { get; set; }

    /// <summary>
    /// Service description
    /// </summary>
    public string? ServiceDescription { get; set; }

    /// <summary>
    /// Submitted amount
    /// </summary>
    public decimal SubmittedAmount { get; set; }

  /// <summary>
    /// Allowed amount
    /// </summary>
    public decimal AllowedAmount { get; set; }

    /// <summary>
    /// Deductible applied
    /// </summary>
    public decimal DeductibleApplied { get; set; }

    /// <summary>
    /// Coinsurance applied
    /// </summary>
    public decimal CoinsuranceApplied { get; set; }

    /// <summary>
    /// Out-of-pocket applied
    /// </summary>
    public decimal OutOfPocketApplied { get; set; }

    /// <summary>
    /// Insurance responsibility
    /// </summary>
    public decimal InsuranceResponsibility { get; set; }

    /// <summary>
    /// Patient responsibility
    /// </summary>
    public decimal PatientResponsibility { get; set; }

    /// <summary>
    /// Adjudication status
    /// </summary>
    public string? AdjudicationStatus { get; set; }
}

/// <summary>
/// Claim Payment Calculation
/// Domain model for storing payment calculations
/// </summary>
public class ClaimPaymentCalculation
{
    /// <summary>
    /// ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Claim ID
    /// </summary>
    public int ClaimId { get; set; }

  /// <summary>
 /// Coverage ID
    /// </summary>
    public int? CoverageId { get; set; }

    /// <summary>
    /// Submitted amount
    /// </summary>
    public decimal SubmittedAmount { get; set; }

    /// <summary>
    /// Allowed amount
    /// </summary>
    public decimal AllowedAmount { get; set; }

    /// <summary>
    /// Deductible applied
    /// </summary>
    public decimal DeductibleApplied { get; set; }

    /// <summary>
    /// Deductible remaining
    /// </summary>
    public decimal DeductibleRemaining { get; set; }

    /// <summary>
    /// Coinsurance applied
    /// </summary>
    public decimal CoinsuranceApplied { get; set; }

    /// <summary>
  /// Out-of-pocket applied
    /// </summary>
    public decimal OutOfPocketApplied { get; set; }

    /// <summary>
    /// Insurance responsibility
    /// </summary>
    public decimal InsuranceResponsibility { get; set; }

    /// <summary>
/// Patient responsibility
    /// </summary>
    public decimal PatientResponsibility { get; set; }

    /// <summary>
    /// Calculated at
    /// </summary>
    public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Notes
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Is valid
    /// </summary>
    public bool IsValid { get; set; }
}
