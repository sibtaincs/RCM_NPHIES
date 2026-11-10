using System;
using System.Collections.Generic;
using System.Linq;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Application.Services
{
    /// <summary>
    /// Payment Calculation Engine
    /// Handles benefit calculations, deductible tracking, coinsurance, and out-of-pocket calculations
    /// </summary>
    public interface IPaymentCalculationEngine
    {
        /// <summary>
        /// Calculate deductible impact on claim item
        /// </summary>
        DeductibleCalculationResult CalculateDeductible(
        decimal allowedAmount,
           decimal deductibleLimit,
        decimal deductibleMet,
       bool isNetworkProvider);

        /// <summary>
        /// Calculate coinsurance impact
        /// </summary>
        CoinsuranceCalculationResult CalculateCoinsurance(
   decimal allowedAmount,
        decimal coinsurancePercentage,
            decimal deductibleRemaining);

        /// <summary>
        /// Calculate out-of-pocket impact
        /// </summary>
        OutOfPocketCalculationResult CalculateOutOfPocket(
               decimal patientResponsibility,
                 decimal outOfPocketLimit,
       decimal outOfPocketMet);

        /// <summary>
        /// Calculate total benefit for a claim item
        /// </summary>
        BenefitCalculationResult CalculateBenefit(
                   ClaimItemBenefitContext context);

        /// <summary>
        /// Calculate total claim benefit amount
        /// </summary>
        ClaimBenefitCalculationResult CalculateClaimBenefit( List<ClaimItemBenefitContext> items,  BenefitConfiguration benefitConfig);
    }

    /// <summary>
    /// Deductible Calculation Result
    /// </summary>
    public class DeductibleCalculationResult
    {
        public bool IsSubjectToDeductible { get; set; }
        public decimal DeductibleApplied { get; set; }
        public decimal AmountAfterDeductible { get; set; }
        public decimal DeductibleRemaining { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>
    /// Coinsurance Calculation Result
    /// </summary>
    public class CoinsuranceCalculationResult
    {
        public bool IsSubjectToCoinsurance { get; set; }
        public decimal CoinsurancePercentage { get; set; }
        public decimal CoinsuranceAmount { get; set; }
        public decimal InsuranceResponsibility { get; set; }
        public decimal PatientResponsibility { get; set; }
    }

    /// <summary>
    /// Out-of-Pocket Calculation Result
    /// </summary>
    public class OutOfPocketCalculationResult
    {
        public bool IsSubjectToOutOfPocket { get; set; }
        public decimal OutOfPocketApplied { get; set; }
        public decimal OutOfPocketRemaining { get; set; }
        public bool OutOfPocketMet { get; set; }
        public string Notes { get; set; }
    }

    /// <summary>
    /// Complete benefit calculation for a single claim item
    /// </summary>
    public class BenefitCalculationResult
    {
        public int ItemSequence { get; set; }
        public string BenefitCategory { get; set; }

        public decimal SubmittedAmount { get; set; }
        public decimal AllowedAmount { get; set; }
        public decimal NotAllowedAmount { get; set; }

        public decimal DeductibleApplied { get; set; }
        public decimal DeductibleRemaining { get; set; }

        public decimal CoinsuranceAmount { get; set; }
        public decimal CoinsurancePercent { get; set; }

        public decimal OutOfPocketApplied { get; set; }
        public decimal OutOfPocketRemaining { get; set; }

        public decimal InsuranceResponsibility { get; set; }
        public decimal PatientResponsibility { get; set; }

        public string AdjudicationStatus { get; set; } // approved, denied, pended
        public List<string> Notes { get; set; } = new();
    }

    /// <summary>
    /// Complete benefit calculation for entire claim
    /// </summary>
    public class ClaimBenefitCalculationResult
    {
        public int ClaimId { get; set; }
        public decimal TotalSubmittedAmount { get; set; }
        public decimal TotalAllowedAmount { get; set; }
        public decimal TotalNotAllowedAmount { get; set; }

        public decimal TotalDeductibleApplied { get; set; }
        public decimal TotalCoinsuranceApplied { get; set; }
        public decimal TotalOutOfPocketApplied { get; set; }

        public decimal TotalInsuranceResponsibility { get; set; }
        public decimal TotalPatientResponsibility { get; set; }

        public List<BenefitCalculationResult> ItemCalculations { get; set; } = new();
        public List<string> ValidationErrors { get; set; } = new();
    }

    /// <summary>
    /// Context for benefit calculation
    /// </summary>
    public class ClaimItemBenefitContext
    {
        public int ItemSequence { get; set; }
        public string BenefitCategory { get; set; }
        public decimal SubmittedAmount { get; set; }
        public decimal AllowedAmount { get; set; }
        public bool IsNetworkProvider { get; set; } = true;
        public bool WaivesCoinsurance { get; set; }
    }

    /// <summary>
    /// Benefit Configuration
    /// </summary>
    public class BenefitConfiguration
    {
        public decimal AnnualDeductible { get; set; }
        public decimal DeductibleMet { get; set; }
        public decimal CoinsurancePercent { get; set; }
        public decimal OutOfPocketMax { get; set; }
        public decimal OutOfPocketMet { get; set; }
        public bool IsInNetwork { get; set; } = true;
    }

    /// <summary>
    /// Implementation of Payment Calculation Engine
    /// </summary>
    public class PaymentCalculationEngine : IPaymentCalculationEngine
    {
        /// <summary>
        /// Calculate deductible impact on claim item
        /// </summary>
        public DeductibleCalculationResult CalculateDeductible(
   decimal allowedAmount,
decimal deductibleLimit,
    decimal deductibleMet,
       bool isNetworkProvider)
        {
            var result = new DeductibleCalculationResult();

            // Deductible doesn't apply to out-of-network in some plans
            if (!isNetworkProvider && deductibleLimit == 0)
            {
                result.IsSubjectToDeductible = false;
                result.DeductibleApplied = 0;
                result.AmountAfterDeductible = allowedAmount;
                result.DeductibleRemaining = 0;
                return result;
            }

            decimal deductibleRemaining = deductibleLimit - deductibleMet;

            if (deductibleRemaining <= 0)
            {
                result.IsSubjectToDeductible = false;
                result.DeductibleApplied = 0;
                result.AmountAfterDeductible = allowedAmount;
                result.DeductibleRemaining = 0;
                result.Notes = "Deductible has been met";
                return result;
            }

            result.IsSubjectToDeductible = true;
            result.DeductibleApplied = Math.Min(allowedAmount, deductibleRemaining);
            result.AmountAfterDeductible = allowedAmount - result.DeductibleApplied;
            result.DeductibleRemaining = deductibleRemaining - result.DeductibleApplied;
            result.Notes = $"Deductible applied: ${result.DeductibleApplied:F2}, Remaining: ${result.DeductibleRemaining:F2}";

            return result;
        }

        /// <summary>
        /// Calculate coinsurance impact
        /// </summary>
        public CoinsuranceCalculationResult CalculateCoinsurance(
            decimal allowedAmount,
            decimal coinsurancePercentage,
            decimal deductibleRemaining)
        {
            var result = new CoinsuranceCalculationResult();

            // No coinsurance if still in deductible phase
            if (deductibleRemaining > 0)
            {
                result.IsSubjectToCoinsurance = false;
                result.CoinsuranceAmount = 0;
                result.InsuranceResponsibility = 0;
                result.PatientResponsibility = allowedAmount;
                return result;
            }

            if (coinsurancePercentage <= 0 || coinsurancePercentage >= 100)
            {
                result.IsSubjectToCoinsurance = false;
                result.CoinsurancePercentage = coinsurancePercentage;
                result.CoinsuranceAmount = 0;
                result.InsuranceResponsibility = allowedAmount;
                result.PatientResponsibility = 0;
                return result;
            }

            result.IsSubjectToCoinsurance = true;
            result.CoinsurancePercentage = coinsurancePercentage;
            result.CoinsuranceAmount = allowedAmount * (coinsurancePercentage / 100);
            result.PatientResponsibility = result.CoinsuranceAmount;
            result.InsuranceResponsibility = allowedAmount - result.CoinsuranceAmount;

            return result;
        }

        /// <summary>
        /// Calculate out-of-pocket impact
        /// </summary>
        public OutOfPocketCalculationResult CalculateOutOfPocket(
     decimal patientResponsibility,
        decimal outOfPocketLimit,
decimal outOfPocketMet)
        {
            var result = new OutOfPocketCalculationResult();

            if (outOfPocketLimit <= 0)
            {
                result.IsSubjectToOutOfPocket = false;
                result.OutOfPocketApplied = 0;
                result.OutOfPocketRemaining = 0;
                result.OutOfPocketMet = false;
                return result;
            }

            decimal outOfPocketRemaining = outOfPocketLimit - outOfPocketMet;

            if (outOfPocketRemaining <= 0)
            {
                result.IsSubjectToOutOfPocket = false;
                result.OutOfPocketApplied = 0;
                result.OutOfPocketRemaining = 0;
                result.OutOfPocketMet = true;
                result.Notes = "Out-of-pocket maximum has been met";
                return result;
            }

            result.IsSubjectToOutOfPocket = true;
            result.OutOfPocketApplied = Math.Min(patientResponsibility, outOfPocketRemaining);
            result.OutOfPocketRemaining = outOfPocketRemaining - result.OutOfPocketApplied;
            result.OutOfPocketMet = result.OutOfPocketRemaining <= 0;

            return result;
        }

        /// <summary>
        /// Calculate total benefit for a single claim item
        /// </summary>
        public BenefitCalculationResult CalculateBenefit(ClaimItemBenefitContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var result = new BenefitCalculationResult
            {
                ItemSequence = context.ItemSequence,
                BenefitCategory = context.BenefitCategory,
                SubmittedAmount = context.SubmittedAmount,
                AllowedAmount = context.AllowedAmount,
                NotAllowedAmount = context.SubmittedAmount - context.AllowedAmount,
                AdjudicationStatus = "approved"
            };

            // Validate amounts
            if (context.AllowedAmount < 0 || context.SubmittedAmount < 0)
            {
                result.AdjudicationStatus = "denied";
                result.Notes.Add("Invalid amount: amounts cannot be negative");
                return result;
            }

            // If no allowed amount, deny
            if (context.AllowedAmount == 0)
            {
                result.AdjudicationStatus = "denied";
                result.PatientResponsibility = context.SubmittedAmount;
                result.Notes.Add("No allowed amount for this service");
                return result;
            }

            // Start with allowed amount as insurance responsibility
            result.InsuranceResponsibility = context.AllowedAmount;
            result.PatientResponsibility = result.NotAllowedAmount;

            return result;
        }

        /// <summary>
        /// Calculate total benefit for entire claim
        /// </summary>
        public ClaimBenefitCalculationResult CalculateClaimBenefit(         List<ClaimItemBenefitContext> items,
     BenefitConfiguration benefitConfig)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("Items list cannot be null or empty");

            if (benefitConfig == null)
                throw new ArgumentNullException(nameof(benefitConfig));

            var result = new ClaimBenefitCalculationResult
            {
                ItemCalculations = new List<BenefitCalculationResult>()
            };

            decimal deductibleRemaining = benefitConfig.DeductibleMet == 0
                 ? benefitConfig.AnnualDeductible
              : benefitConfig.AnnualDeductible - benefitConfig.DeductibleMet;

            decimal outOfPocketRemaining = benefitConfig.OutOfPocketMet == 0
      ? benefitConfig.OutOfPocketMax
         : benefitConfig.OutOfPocketMax - benefitConfig.OutOfPocketMet;

            // Process each item
            foreach (var item in items)
            {
                var itemResult = CalculateBenefit(item);

                // Apply deductible
                if (deductibleRemaining > 0 && itemResult.AllowedAmount > 0)
                {
                    var deductResult = CalculateDeductible(
                             itemResult.AllowedAmount,
                               benefitConfig.AnnualDeductible,
                    benefitConfig.AnnualDeductible - deductibleRemaining,
                      item.IsNetworkProvider);

                    itemResult.DeductibleApplied = deductResult.DeductibleApplied;
                    itemResult.DeductibleRemaining = deductResult.DeductibleRemaining;
                    deductibleRemaining = deductResult.DeductibleRemaining;

                    // Deductible reduces insurance responsibility
                    itemResult.InsuranceResponsibility -= itemResult.DeductibleApplied;
                    itemResult.PatientResponsibility += itemResult.DeductibleApplied;
                }

                // Apply coinsurance (after deductible)
                if (deductibleRemaining <= 0 && itemResult.InsuranceResponsibility > 0)
                {
                    var coinsResult = CalculateCoinsurance(
                     itemResult.InsuranceResponsibility,
                    benefitConfig.CoinsurancePercent,
                   deductibleRemaining);

                    if (coinsResult.IsSubjectToCoinsurance && !item.WaivesCoinsurance)
                    {
                        itemResult.CoinsuranceAmount = coinsResult.CoinsuranceAmount;
                        itemResult.CoinsurancePercent = benefitConfig.CoinsurancePercent;
                        itemResult.InsuranceResponsibility = coinsResult.InsuranceResponsibility;
                        itemResult.PatientResponsibility += coinsResult.CoinsuranceAmount;
                    }
                }

                // Apply out-of-pocket cap
                if (itemResult.PatientResponsibility > 0)
                {
                    var ooResult = CalculateOutOfPocket(
                     itemResult.PatientResponsibility,
                    benefitConfig.OutOfPocketMax,
                      benefitConfig.OutOfPocketMax - outOfPocketRemaining);

                    if (ooResult.IsSubjectToOutOfPocket)
                    {
                        itemResult.OutOfPocketApplied = ooResult.OutOfPocketApplied;
                        itemResult.OutOfPocketRemaining = ooResult.OutOfPocketRemaining;

                        // Amount exceeding OOP max is covered by insurance
                        if (itemResult.PatientResponsibility > ooResult.OutOfPocketApplied)
                        {
                            itemResult.InsuranceResponsibility += (itemResult.PatientResponsibility - ooResult.OutOfPocketApplied);
                            itemResult.PatientResponsibility = ooResult.OutOfPocketApplied;
                        }

                        outOfPocketRemaining = ooResult.OutOfPocketRemaining;
                    }
                }

                result.ItemCalculations.Add(itemResult);

                // Accumulate totals
                result.TotalSubmittedAmount += itemResult.SubmittedAmount;
                result.TotalAllowedAmount += itemResult.AllowedAmount;
                result.TotalNotAllowedAmount += itemResult.NotAllowedAmount;
                result.TotalDeductibleApplied += itemResult.DeductibleApplied;
                result.TotalCoinsuranceApplied += itemResult.CoinsuranceAmount;
                result.TotalOutOfPocketApplied += itemResult.OutOfPocketApplied;
                result.TotalInsuranceResponsibility += itemResult.InsuranceResponsibility;
                result.TotalPatientResponsibility += itemResult.PatientResponsibility;
            }

            // Validate totals
            if (Math.Abs((result.TotalInsuranceResponsibility + result.TotalPatientResponsibility) - result.TotalAllowedAmount) > 0.01m)
            {
                result.ValidationErrors.Add("Insurance + Patient responsibility does not equal allowed amount");
            }

            return result;
        }
    }
}
