using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPhies_FHIR_Integration.Application.Services.Masters;
using NPhies_FHIR_Integration.Application.Services.RCM.Rules;

namespace NPhies_FHIR_Integration.Application.Services.RCM;

/// <summary>
/// Revenue Cycle Management Service Interface
/// Orchestrates adjudication process with error codes and rules
/// </summary>
public interface IRCMService
{
    /// <summary>
    /// Adjudicate a claim item with complete financial breakdown
    /// </summary>
    Task<ClaimAdjudicationResult> AdjudicateClaimItemAsync(
        ClaimAdjudicationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get error codes associated with adjudication denial
    /// </summary>
    Task<ErrorCodeSummary> GetErrorCodeSummaryAsync(
        string errorCode,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Claim adjudication request
/// </summary>
public class ClaimAdjudicationRequest
{
    public int ItemSequence { get; set; }
    public string ServiceCode { get; set; } = string.Empty;
    public decimal SubmittedAmount { get; set; }
    public decimal AllowedAmount { get; set; }
public string NetworkStatus { get; set; } = "in-network";
    public decimal CoveragePercentage { get; set; } = 80m;

    // Deductible
    public decimal AnnualDeductible { get; set; }
    public decimal DeductibleMet { get; set; }

    // Copay
    public decimal CopayAmount { get; set; }

    // Coinsurance
    public decimal CoinsurancePercentage { get; set; } = 20m;

    // Out of Pocket
    public decimal OutOfPocketMax { get; set; }
    public decimal OutOfPocketMet { get; set; }

// Benefit Limits
    public decimal AnnualBenefitLimit { get; set; }
 public decimal BenefitUsed { get; set; }

    // Service Details
    public string ServiceType { get; set; } = string.Empty;
    public string DiagnosisCode { get; set; } = string.Empty;

    // Coverage Flags
    public bool IsServiceExcluded { get; set; }
    public bool RequiresPriorAuth { get; set; }
    public bool HasValidPriorAuth { get; set; }
    public bool WithinWaitingPeriod { get; set; }
    public bool IsAgeQualified { get; set; } = true;
}

/// <summary>
/// Claim adjudication result with financial breakdown
/// </summary>
public class ClaimAdjudicationResult
{
    public bool IsApproved { get; set; }
    public bool IsDenied { get; set; }
 public decimal InsuranceResponsibility { get; set; }
    public decimal PatientResponsibility { get; set; }
    public string? DenialReasonCode { get; set; }
    public string? DenialReasonDescription { get; set; }
    public List<RuleExecutionDetail> RuleExecutions { get; set; } = new();
    public double ProcessingTimeMs { get; set; }
 public bool CanAppeal { get; set; }
    public int AppealDeadlineDays { get; set; }
}

/// <summary>
/// Error code summary for display
/// </summary>
public class ErrorCodeSummary
{
    public string ErrorCode { get; set; } = string.Empty;
    public string ErrorDescription { get; set; } = string.Empty;
    public string ErrorCategory { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public bool IsRecoverable { get; set; }
    public bool AllowsAppeal { get; set; }
    public int AppealDeadlineDays { get; set; }
    public string? RecommendedAction { get; set; }
}

/// <summary>
/// Rule execution detail
/// </summary>
public class RuleExecutionDetail
{
  public string RuleId { get; set; } = string.Empty;
  public string RuleName { get; set; } = string.Empty;
    public bool WasApplied { get; set; }
    public string? Message { get; set; }
    public decimal? AmountApplied { get; set; }
}

/// <summary>
/// RCM Service Implementation
/// Integrates adjudication rules with error codes
/// </summary>
public class RCMService : IRCMService
{
    private readonly ILogger<RCMService> _logger;
    private readonly IErrorCodeService _errorCodeService;
private readonly AdjudicationRuleEngine _ruleEngine;

    public RCMService(
      ILogger<RCMService> logger,
        IErrorCodeService errorCodeService,
        AdjudicationRuleEngine ruleEngine)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _errorCodeService = errorCodeService ?? throw new ArgumentNullException(nameof(errorCodeService));
        _ruleEngine = ruleEngine ?? throw new ArgumentNullException(nameof(ruleEngine));
    }

    /// <summary>
    /// Adjudicate a claim item
    /// </summary>
    public async Task<ClaimAdjudicationResult> AdjudicateClaimItemAsync(
  ClaimAdjudicationRequest request,
        CancellationToken cancellationToken = default)
    {
      _logger.LogInformation("Starting adjudication for service {ServiceCode}, item {Sequence}",
        request.ServiceCode, request.ItemSequence);

        var startTime = DateTime.UtcNow;

     try
     {
        // Create context from request
        var context = new Rules.AdjudicationContext
        {
            ItemSequence = request.ItemSequence,
  ServiceCode = request.ServiceCode,
      SubmittedAmount = request.SubmittedAmount,
AllowedAmount = request.AllowedAmount,
  NetworkStatus = request.NetworkStatus,
    CoveragePercentage = request.CoveragePercentage,
      AnnualDeductible = request.AnnualDeductible,
   DeductibleMet = request.DeductibleMet,
            CopayAmount = request.CopayAmount,
            CoinsurancePercentage = request.CoinsurancePercentage,
       OutOfPocketMax = request.OutOfPocketMax,
            OutOfPocketMet = request.OutOfPocketMet,
AnnualBenefitLimit = request.AnnualBenefitLimit,
       BenefitUsed = request.BenefitUsed,
            ServiceType = request.ServiceType,
            DiagnosisCode = request.DiagnosisCode,
 IsServiceExcluded = request.IsServiceExcluded,
     RequiresPriorAuth = request.RequiresPriorAuth,
      HasValidPriorAuth = request.HasValidPriorAuth,
            WithinWaitingPeriod = request.WithinWaitingPeriod,
         IsAgeQualified = request.IsAgeQualified
        };

            // Execute rules
            var engineResult = await _ruleEngine.ExecuteAsync(context);

   // Map to result
         var result = new ClaimAdjudicationResult
          {
  IsApproved = engineResult.IsSuccessful && engineResult.InsuranceResponsibility > 0,
IsDenied = !engineResult.IsSuccessful || engineResult.InsuranceResponsibility == 0,
     InsuranceResponsibility = engineResult.InsuranceResponsibility,
         PatientResponsibility = engineResult.PatientResponsibility,
       ProcessingTimeMs = engineResult.DurationMs,
              RuleExecutions = engineResult.ExecutedRules
 .Select(r => new RuleExecutionDetail
      {
        RuleId = r.RuleId,
          RuleName = r.RuleName,
       WasApplied = r.IsApplied,
       Message = r.Message,
 AmountApplied = r.PatientResponsibilityApplied
          })
         .ToList()
 };

   // If denied, get error code info
            if (result.IsDenied && !string.IsNullOrEmpty(engineResult.ErrorMessage))
    {
         // Try to find matching error code
          var errorCodes = await _errorCodeService.SearchErrorCodesAsync(engineResult.ErrorMessage);
      if (errorCodes.Any())
                {
          var errorCode = errorCodes.First();
       result.DenialReasonCode = errorCode.ErrorCode;
         result.DenialReasonDescription = errorCode.ErrorDescription;
    result.CanAppeal = errorCode.AllowsAppeal;
     result.AppealDeadlineDays = errorCode.StandardAppealDays;
      }
   }

  _logger.LogInformation(
 "Adjudication completed for {ServiceCode}: Insurance=${Insurance:F2}, Patient=${Patient:F2}",
                request.ServiceCode, result.InsuranceResponsibility, result.PatientResponsibility);

            return result;
 }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adjudicating claim item {ItemSequence}", request.ItemSequence);
            throw;
        }
    }

    /// <summary>
    /// Get error code summary
    /// </summary>
    public async Task<ErrorCodeSummary> GetErrorCodeSummaryAsync(
string errorCode,
        CancellationToken cancellationToken = default)
    {
        var code = await _errorCodeService.GetErrorCodeAsync(errorCode);

        if (code == null)
    {
      _logger.LogWarning("Error code not found: {ErrorCode}", errorCode);
         return new ErrorCodeSummary();
        }

        return new ErrorCodeSummary
        {
        ErrorCode = code.ErrorCode,
         ErrorDescription = code.ErrorDescription,
    ErrorCategory = code.ErrorCategory,
            Severity = code.Severity,
            IsRecoverable = code.IsRecoverable,
     AllowsAppeal = code.AllowsAppeal,
            AppealDeadlineDays = code.StandardAppealDays,
         RecommendedAction = code.RecommendedAction
        };
    }
}
