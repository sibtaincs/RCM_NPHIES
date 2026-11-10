using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM
{
    /// <summary>
    /// NPHIES Enhanced Payment Calculation Engine Service Interface
    /// Calculates complex payment amounts including deductibles, copays, coinsurance
    /// </summary>
    public interface IEnhancedPaymentCalculationEngine
    {
 /// <summary>
        /// Calculate payment for a service
        /// </summary>
        Task<PaymentCalculationResult> CalculatePaymentAsync(PaymentCalculationRequest request);

        /// <summary>
        /// Apply deductible to amount
     /// </summary>
      Task<DeductibleApplication> ApplyDeductibleAsync(decimal amount, decimal deductibleMet, decimal annualDeductible);

        /// <summary>
  /// Calculate copay based on service and coverage
        /// </summary>
        Task<decimal> CalculateCopayAsync(string serviceCode, decimal allowedAmount, bool isNetworkProvider);

     /// <summary>
        /// Calculate coinsurance based on allowed amount
        /// </summary>
        Task<CoinsuranceCalculation> CalculateCoinsuranceAsync(decimal allowedAmount, decimal coinsurancePercent);

   /// <summary>
     /// Calculate out-of-pocket max constraint
   /// </summary>
        Task<OutOfPocketCalculation> CalculateOutOfPocketAsync(decimal patientCost, decimal oomMet, decimal oomMax);

  /// <summary>
        /// Apply network discounts
        /// </summary>
        Task<decimal> ApplyNetworkDiscountAsync(decimal billed, bool isNetworkProvider);

 /// <summary>
        /// Calculate multi-tier payment structure
        /// </summary>
        Task<MultiTierPaymentResult> CalculateMultiTierPaymentAsync(MultiTierPaymentRequest request);

    /// <summary>
        /// Detect overpayments
        /// </summary>
    Task<OverpaymentDetection> DetectOverpaymentAsync(decimal insurancePays, decimal allowedAmount, decimal previousPayments);

  /// <summary>
        /// Calculate refund amount
        /// </summary>
        Task<decimal> CalculateRefundAsync(decimal overpaymentAmount);

        /// <summary>
        /// Get payment breakdown
        /// </summary>
        Task<PaymentBreakdown> GetPaymentBreakdownAsync(PaymentCalculationRequest request);
    }

    /// <summary>
    /// Payment calculation request
    /// </summary>
    public class PaymentCalculationRequest
    {
        public string ServiceCode { get; set; } = string.Empty;
        public decimal BilledAmount { get; set; }
        public decimal AllowedAmount { get; set; }
        public decimal CopayAmount { get; set; }
        public decimal CoinsurancePercent { get; set; }
        public decimal AnnualDeductible { get; set; }
      public decimal DeductibleMet { get; set; }
        public decimal OutOfPocketMax { get; set; }
        public decimal OutOfPocketMet { get; set; }
        public bool IsNetworkProvider { get; set; }
        public string ClaimType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Payment calculation result
    /// </summary>
    public class PaymentCalculationResult
    {
        public decimal BilledAmount { get; set; }
        public decimal AllowedAmount { get; set; }
    public decimal DeductibleApplied { get; set; }
 public decimal CopayApplied { get; set; }
     public decimal CoinsuranceApplied { get; set; }
        public decimal PatientResponsibility { get; set; }
        public decimal InsurancePayment { get; set; }
        public decimal WriteOff { get; set; }
 public decimal OutOfPocketMet { get; set; }
        public DateTime CalculatedAt { get; set; } = DateTime.UtcNow;
        public List<string> AppliedRules { get; set; } = new();
        public Dictionary<string, decimal> Breakdown { get; set; } = new();
    }

    /// <summary>
    /// Deductible application
    /// </summary>
    public class DeductibleApplication
    {
        public decimal AnnualDeductible { get; set; }
        public decimal DeductiblePreviouslyMet { get; set; }
      public decimal DeductibleAppliedThisService { get; set; }
        public decimal AmountAfterDeductible { get; set; }
        public decimal RemainingDeductible { get; set; }
   public bool DeductibleMet { get; set; }
    }

    /// <summary>
    /// Coinsurance calculation
    /// </summary>
    public class CoinsuranceCalculation
    {
    public decimal AllowedAmount { get; set; }
        public decimal CoinsurancePercent { get; set; }
        public decimal InsuranceShare { get; set; }
        public decimal PatientShare { get; set; }
        public decimal InsurancePayment { get; set; }
    }

    /// <summary>
    /// Out-of-pocket calculation
    /// </summary>
    public class OutOfPocketCalculation
    {
    public decimal OutOfPocketMax { get; set; }
      public decimal PreviouslyMet { get; set; }
        public decimal ThisServiceCost { get; set; }
      public decimal NewTotal { get; set; }
        public decimal RemainingToMax { get; set; }
        public bool MaxReached { get; set; }
 }

    /// <summary>
    /// Multi-tier payment request
    /// </summary>
    public class MultiTierPaymentRequest
    {
    public string ServiceCode { get; set; } = string.Empty;
        public decimal AllowedAmount { get; set; }
        public int TierLevel { get; set; } // 1 = In-Network, 2 = Out-of-Network
        public decimal CopayByTier { get; set; }
        public decimal CoinsuranceByTier { get; set; }
    }

    /// <summary>
    /// Multi-tier payment result
    /// </summary>
    public class MultiTierPaymentResult
    {
        public int TierLevel { get; set; }
      public string TierName { get; set; } = string.Empty;
        public decimal AllowedAmount { get; set; }
        public decimal CopayAmount { get; set; }
        public decimal CoinsurancePercent { get; set; }
        public decimal InsurancePayment { get; set; }
        public decimal PatientResponsibility { get; set; }
 }

    /// <summary>
    /// Overpayment detection
    /// </summary>
    public class OverpaymentDetection
    {
      public bool OverpaymentExists { get; set; }
     public decimal OverpaymentAmount { get; set; }
        public string Reason { get; set; } = string.Empty;
        public RefundStatus RefundStatus { get; set; }
    }

    /// <summary>
    /// Refund status
    /// </summary>
 public enum RefundStatus
    {
     NoRefundNeeded = 0,
        RefundPending = 1,
        RefundProcessed = 2,
        RefundHeld = 3
    }

    /// <summary>
    /// Payment breakdown
    /// </summary>
public class PaymentBreakdown
    {
        public decimal BilledAmount { get; set; }
        public decimal ContractualAdjustment { get; set; }
     public decimal AllowedAmount { get; set; }
     public decimal DeductibleAmount { get; set; }
        public decimal CopayAmount { get; set; }
   public decimal CoinsuranceAmount { get; set; }
      public decimal InsurancePayableAmount { get; set; }
        public decimal PatientResponsibility { get; set; }
    public Dictionary<string, object> Details { get; set; } = new();
    }

    /// <summary>
    /// NPHIES Enhanced Payment Calculation Engine Implementation
    /// </summary>
  public class EnhancedPaymentCalculationEngine : IEnhancedPaymentCalculationEngine
    {
        private readonly ILogger<EnhancedPaymentCalculationEngine> _logger;

 public EnhancedPaymentCalculationEngine(ILogger<EnhancedPaymentCalculationEngine> logger)
     {
            _logger = logger;
        }

        /// <summary>
     /// Main payment calculation method
        /// </summary>
        public async Task<PaymentCalculationResult> CalculatePaymentAsync(PaymentCalculationRequest request)
        {
        var result = new PaymentCalculationResult
            {
   BilledAmount = request.BilledAmount,
     AllowedAmount = request.AllowedAmount
            };

            try
            {
    _logger.LogInformation($"Calculating payment for service {request.ServiceCode}");

     // Apply network discount
  var afterDiscount = await ApplyNetworkDiscountAsync(request.BilledAmount, request.IsNetworkProvider);
  var allowedAmount = Math.Min(afterDiscount, request.AllowedAmount);

        // Apply deductible
    var deductibleApp = await ApplyDeductibleAsync(allowedAmount, request.DeductibleMet, request.AnnualDeductible);
             result.DeductibleApplied = deductibleApp.DeductibleAppliedThisService;
      var afterDeductible = deductibleApp.AmountAfterDeductible;

      // Apply copay
      result.CopayApplied = Math.Min(request.CopayAmount, afterDeductible);

 // Apply coinsurance to remaining after deductible and copay
     var coinsuranceBase = Math.Max(0, afterDeductible - result.CopayApplied);
      var coinsuranceCalc = await CalculateCoinsuranceAsync(coinsuranceBase, request.CoinsurancePercent);
                result.CoinsuranceApplied = coinsuranceCalc.PatientShare;

       // Calculate insurance payment
        result.InsurancePayment = allowedAmount - result.DeductibleApplied - result.CopayApplied - result.CoinsuranceApplied;

                // Apply out-of-pocket max
var patientCost = result.DeductibleApplied + result.CopayApplied + result.CoinsuranceApplied;
                var oopCalc = await CalculateOutOfPocketAsync(patientCost, request.OutOfPocketMet, request.OutOfPocketMax);
 
            // If OOP max reached, insurance pays more
      if (oopCalc.MaxReached && oopCalc.PreviouslyMet + patientCost > request.OutOfPocketMax)
           {
 var excessOOP = oopCalc.PreviouslyMet + patientCost - request.OutOfPocketMax;
         result.InsurancePayment += excessOOP;
     patientCost -= excessOOP;
       }

   result.PatientResponsibility = patientCost;
                result.OutOfPocketMet = oopCalc.NewTotal;
     result.WriteOff = request.BilledAmount - allowedAmount;

            result.AppliedRules.Add("Network discount applied");
         result.AppliedRules.Add("Deductible applied");
           result.AppliedRules.Add("Copay applied");
             result.AppliedRules.Add("Coinsurance applied");
     result.AppliedRules.Add("OOP max checked");

        _logger.LogInformation($"Payment calculation completed. Insurance pays: ${result.InsurancePayment}, Patient pays: ${result.PatientResponsibility}");
 }
catch (Exception ex)
       {
    _logger.LogError(ex, $"Error calculating payment for service {request.ServiceCode}");
         result.InsurancePayment = 0;
       result.PatientResponsibility = request.AllowedAmount;
            }

            return result;
        }

      /// <summary>
        /// Apply deductible
        /// </summary>
        public async Task<DeductibleApplication> ApplyDeductibleAsync(decimal amount, decimal deductibleMet, decimal annualDeductible)
     {
   try
            {
     var remaining = annualDeductible - deductibleMet;
                var appliedThisService = Math.Min(remaining, amount);
    var afterDeductible = amount - appliedThisService;

     return new DeductibleApplication
          {
          AnnualDeductible = annualDeductible,
      DeductiblePreviouslyMet = deductibleMet,
    DeductibleAppliedThisService = appliedThisService,
            AmountAfterDeductible = afterDeductible,
      RemainingDeductible = remaining - appliedThisService,
            DeductibleMet = (deductibleMet + appliedThisService) >= annualDeductible
    };
          }
          catch (Exception ex)
            {
            _logger.LogError(ex, "Error applying deductible");
                return new DeductibleApplication { AmountAfterDeductible = amount };
     }
        }

        /// <summary>
/// Calculate copay
        /// </summary>
        public async Task<decimal> CalculateCopayAsync(string serviceCode, decimal allowedAmount, bool isNetworkProvider)
        {
   try
       {
      // Standard copays: $20 network, $50 out-of-network
         return isNetworkProvider ? Math.Min(20m, allowedAmount) : Math.Min(50m, allowedAmount);
  }
        catch (Exception ex)
       {
                _logger.LogError(ex, $"Error calculating copay for {serviceCode}");
      return 0m;
   }
        }

        /// <summary>
        /// Calculate coinsurance
      /// </summary>
        public async Task<CoinsuranceCalculation> CalculateCoinsuranceAsync(decimal allowedAmount, decimal coinsurancePercent)
{
 try
            {
                var patientShare = (allowedAmount * coinsurancePercent) / 100m;
       var insuranceShare = allowedAmount - patientShare;

     return new CoinsuranceCalculation
   {
      AllowedAmount = allowedAmount,
         CoinsurancePercent = coinsurancePercent,
                    PatientShare = patientShare,
          InsuranceShare = insuranceShare,
         InsurancePayment = insuranceShare
       };
            }
    catch (Exception ex)
    {
       _logger.LogError(ex, "Error calculating coinsurance");
  return new CoinsuranceCalculation { InsuranceShare = allowedAmount };
      }
        }

        /// <summary>
        /// Calculate out-of-pocket
    /// </summary>
        public async Task<OutOfPocketCalculation> CalculateOutOfPocketAsync(decimal patientCost, decimal oopMet, decimal oopMax)
        {
      try
     {
      var newTotal = oopMet + patientCost;
    var remaining = Math.Max(0, oopMax - newTotal);
    var maxReached = newTotal >= oopMax;

          return new OutOfPocketCalculation
              {
OutOfPocketMax = oopMax,
                PreviouslyMet = oopMet,
               ThisServiceCost = patientCost,
             NewTotal = newTotal,
  RemainingToMax = remaining,
            MaxReached = maxReached
              };
         }
      catch (Exception ex)
   {
          _logger.LogError(ex, "Error calculating out-of-pocket");
                return new OutOfPocketCalculation { NewTotal = oopMet + patientCost };
            }
        }

      /// <summary>
        /// Apply network discount
 /// </summary>
     public async Task<decimal> ApplyNetworkDiscountAsync(decimal billed, bool isNetworkProvider)
        {
            try
     {
    // Network providers get 20% discount, out-of-network 0%
                if (isNetworkProvider)
        {
   return billed * 0.8m;  // 20% discount
           }
    return billed;
   }
    catch (Exception ex)
       {
         _logger.LogError(ex, "Error applying network discount");
       return billed;
  }
        }

        /// <summary>
        /// Calculate multi-tier payment
        /// </summary>
     public async Task<MultiTierPaymentResult> CalculateMultiTierPaymentAsync(MultiTierPaymentRequest request)
   {
        try
      {
      var coinsuranceCalc = await CalculateCoinsuranceAsync(request.AllowedAmount, request.CoinsuranceByTier);

        return new MultiTierPaymentResult
            {
           TierLevel = request.TierLevel,
 TierName = request.TierLevel == 1 ? "In-Network" : request.TierLevel == 2 ? "Out-of-Network" : "Non-Participating",
          AllowedAmount = request.AllowedAmount,
     CopayAmount = request.CopayByTier,
         CoinsurancePercent = request.CoinsuranceByTier,
         InsurancePayment = coinsuranceCalc.InsuranceShare - request.CopayByTier,
           PatientResponsibility = request.CopayByTier + coinsuranceCalc.PatientShare
                };
            }
        catch (Exception ex)
       {
      _logger.LogError(ex, "Error calculating multi-tier payment");
        return new MultiTierPaymentResult { TierLevel = request.TierLevel };
       }
        }

        /// <summary>
        /// Detect overpayment
    /// </summary>
        public async Task<OverpaymentDetection> DetectOverpaymentAsync(decimal insurancePays, decimal allowedAmount, decimal previousPayments)
        {
            try
 {
     var totalPaid = previousPayments + insurancePays;
     var overpaymentExists = totalPaid > allowedAmount;

     return new OverpaymentDetection
                {
   OverpaymentExists = overpaymentExists,
        OverpaymentAmount = overpaymentExists ? totalPaid - allowedAmount : 0,
      Reason = overpaymentExists ? "Total payments exceed allowed amount" : "No overpayment",
          RefundStatus = overpaymentExists ? RefundStatus.RefundPending : RefundStatus.NoRefundNeeded
      };
     }
     catch (Exception ex)
            {
   _logger.LogError(ex, "Error detecting overpayment");
     return new OverpaymentDetection();
     }
        }

        /// <summary>
        /// Calculate refund
    /// </summary>
   public async Task<decimal> CalculateRefundAsync(decimal overpaymentAmount)
        {
            try
     {
      return Math.Max(0, overpaymentAmount);
      }
            catch (Exception ex)
 {
     _logger.LogError(ex, "Error calculating refund");
     return 0m;
        }
        }

        /// <summary>
    /// Get payment breakdown
     /// </summary>
        public async Task<PaymentBreakdown> GetPaymentBreakdownAsync(PaymentCalculationRequest request)
        {
       try
      {
       var calcResult = await CalculatePaymentAsync(request);
     var contractAdjustment = request.BilledAmount - request.AllowedAmount;

            return new PaymentBreakdown
        {
    BilledAmount = request.BilledAmount,
             ContractualAdjustment = contractAdjustment,
         AllowedAmount = request.AllowedAmount,
   DeductibleAmount = calcResult.DeductibleApplied,
               CopayAmount = calcResult.CopayApplied,
         CoinsuranceAmount = calcResult.CoinsuranceApplied,
     InsurancePayableAmount = calcResult.InsurancePayment,
        PatientResponsibility = calcResult.PatientResponsibility,
      Details = new Dictionary<string, object>
             {
         { "NetworkProvider", request.IsNetworkProvider },
       { "ClaimType", request.ClaimType },
       { "CalculatedAt", DateTime.UtcNow }
          }
 };
}
      catch (Exception ex)
    {
        _logger.LogError(ex, "Error getting payment breakdown");
 return new PaymentBreakdown { BilledAmount = request.BilledAmount };
    }
        }
    }
}
