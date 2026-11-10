using NPhies_FHIR_Integration.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM
{
    /// <summary>
    /// NPHIES Benefit Determination Engine Service Interface
    /// Determines applicable benefits for services based on plan design and coverage
    /// </summary>
 public interface IBenefitDeterminationEngine
    {
        /// <summary>
        /// Determine benefits for a service
        /// </summary>
        Task<BenefitDeterminationResult> DetermineBenefitAsync(BenefitDeterminationRequest request);

        /// <summary>
        /// Get service coverage status
     /// </summary>
        Task<ServiceCoverageInfo> GetServiceCoverageAsync(string serviceCode, string coverageId);

        /// <summary>
/// Check service limitations
        /// </summary>
        Task<ServiceLimitation> GetServiceLimitationAsync(string serviceCode, string coverageId);

        /// <summary>
 /// Get benefit exclusions
      /// </summary>
 Task<List<BenefitExclusion>> GetBenefitExclusionsAsync(string coverageId);

    /// <summary>
        /// Calculate copay for service
        /// </summary>
        Task<decimal> CalculateCopayAsync(string serviceCode, string coverageId, bool isNetworkProvider);

        /// <summary>
        /// Calculate coinsurance percentage
  /// </summary>
    Task<decimal> GetCoinsurancePercentAsync(string serviceCode, string coverageId, bool isNetworkProvider);

 /// <summary>
   /// Check frequency limits
        /// </summary>
     Task<FrequencyLimit> GetFrequencyLimitAsync(string serviceCode, string coverageId);

        /// <summary>
  /// Check duration limits
      /// </summary>
        Task<DurationLimit> GetDurationLimitAsync(string serviceCode, string coverageId);

        /// <summary>
    /// Get benefit period information
        /// </summary>
        Task<BenefitPeriod> GetBenefitPeriodAsync(string coverageId);

        /// <summary>
        /// Get multi-tier benefit structure
        /// </summary>
        Task<MultiTierBenefit> GetMultiTierBenefitAsync(string serviceCode, string coverageId);

        /// <summary>
        /// Check if service requires authorization
        /// </summary>
        Task<bool> RequiresAuthorizationAsync(string serviceCode, string coverageId);

     /// <summary>
        /// Get all benefits for coverage
  /// </summary>
        Task<List<CoverageBenefit>> GetAllBenefitsAsync(string coverageId);
    }

    /// <summary>
 /// Benefit determination request
    /// </summary>
    public class BenefitDeterminationRequest
    {
        public string ServiceCode { get; set; } = string.Empty;
    public string CoverageId { get; set; } = string.Empty;
  public string ClaimType { get; set; } = string.Empty; // inpatient, outpatient, emergency
        public bool IsNetworkProvider { get; set; }
        public DateTime ServiceDate { get; set; }
        public decimal RequestedAmount { get; set; }
        public string DiagnosisCode { get; set; } = string.Empty;
    public Dictionary<string, object> AdditionalContext { get; set; } = new();
  }

    /// <summary>
    /// Benefit determination result
    /// </summary>
    public class BenefitDeterminationResult
    {
        public bool IsCovered { get; set; }
        public string CoverageStatus { get; set; } = string.Empty; // Covered, Excluded, Limited, RequiresAuth
        public decimal CopayAmount { get; set; }
        public decimal CoinsurancePercentage { get; set; }
        public decimal BenefitMaximum { get; set; }
   public decimal BenefitUsed { get; set; }
   public decimal RemainingBenefit { get; set; }
        public List<string> AppliedLimitations { get; set; } = new();
        public List<string> AppliedExclusions { get; set; } = new();
        public bool RequiresPreAuthorization { get; set; }
 public string DenialReason { get; set; } = string.Empty;
public DateTime DeterminationDate { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> BenefitDetails { get; set; } = new();
    }

 /// <summary>
    /// Service coverage information
    /// </summary>
    public class ServiceCoverageInfo
    {
        public string ServiceCode { get; set; } = string.Empty;
        public bool IsCovered { get; set; }
     public string CoverageType { get; set; } = string.Empty; // Full, Partial, Excluded
        public decimal CopayAmount { get; set; }
        public decimal CoinsuranceInNetwork { get; set; }
        public decimal CoinsuranceOutOfNetwork { get; set; }
        public decimal DeductibleApplicable { get; set; }
        public string Description { get; set; } = string.Empty;
  public List<string> Limitations { get; set; } = new();
     public bool RequiresAuthorization { get; set; }
    }

    /// <summary>
    /// Service limitation
    /// </summary>
    public class ServiceLimitation
    {
    public string ServiceCode { get; set; } = string.Empty;
        public LimitationType LimitationType { get; set; }
  public string LimitationValue { get; set; } = string.Empty;
        public string TimeFrame { get; set; } = string.Empty; // Calendar Year, Rolling 12 months, Lifetime
      public int Quantity { get; set; } // For frequency limits
        public int Days { get; set; } // For duration limits
        public decimal MaximumAmount { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    /// <summary>
    /// Limitation type
    /// </summary>
    public enum LimitationType
    {
        Frequency = 1,      // X times per year
        Duration = 2,    // Limited to X days
  Amount = 3,    // Limited to $X
        Quantity = 4  // Limited to X units
    }

    /// <summary>
    /// Benefit exclusion
    /// </summary>
    public class BenefitExclusion
    {
        public string ExclusionCode { get; set; } = string.Empty;
      public string ExclusionDescription { get; set; } = string.Empty;
      public string ServiceCategory { get; set; } = string.Empty;
        public List<string> ExcludedServiceCodes { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Frequency limit
    /// </summary>
    public class FrequencyLimit
    {
        public string ServiceCode { get; set; } = string.Empty;
        public int MaxFrequency { get; set; } // Times per period
      public string FrequencyPeriod { get; set; } = string.Empty; // Year, Month, Lifetime
  public int CurrentUsage { get; set; }
     public int RemainingAllowed { get; set; }
        public bool LimitReached { get; set; }
    }

    /// <summary>
    /// Duration limit
    /// </summary>
    public class DurationLimit
    {
        public string ServiceCode { get; set; } = string.Empty;
        public int MaxDays { get; set; }
        public string DurationPeriod { get; set; } = string.Empty; // Year, Lifetime, Per Condition
        public int DaysUsed { get; set; }
        public int DaysRemaining { get; set; }
   public bool LimitReached { get; set; }
    }

    /// <summary>
    /// Benefit period
    /// </summary>
    public class BenefitPeriod
    {
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
    public string PeriodType { get; set; } = string.Empty; // Calendar Year, Plan Year, Rolling
        public bool IsActive { get; set; }
        public int DaysRemaining => Math.Max(0, (int)(PeriodEndDate - DateTime.Now).TotalDays);
    }

    /// <summary>
    /// Multi-tier benefit
    /// </summary>
public class MultiTierBenefit
    {
        public string ServiceCode { get; set; } = string.Empty;
        public List<BenefitTier> Tiers { get; set; } = new();
    }

    /// <summary>
    /// Benefit tier
    /// </summary>
    public class BenefitTier
    {
        public int TierLevel { get; set; } // 1 = Network, 2 = Out-of-Network, 3 = Non-Participating
        public string TierName { get; set; } = string.Empty;
        public decimal CopayAmount { get; set; }
        public decimal CoinsurancePercentage { get; set; }
        public bool Covered { get; set; }
    }

    /// <summary>
    /// Coverage benefit
    /// </summary>
    public class CoverageBenefit
    {
        public string BenefitCode { get; set; } = string.Empty;
        public string BenefitName { get; set; } = string.Empty;
        public string BenefitCategory { get; set; } = string.Empty;
    public bool IsCovered { get; set; }
        public decimal CopayAmount { get; set; }
        public decimal CoinsurancePercentage { get; set; }
        public decimal AnnualMaximum { get; set; }
    }

    /// <summary>
    /// NPHIES Benefit Determination Engine Implementation
    /// </summary>
    public class BenefitDeterminationEngine : IBenefitDeterminationEngine
    {
  private readonly ILogger<BenefitDeterminationEngine> _logger;

        public BenefitDeterminationEngine(ILogger<BenefitDeterminationEngine> logger)
        {
        _logger = logger;
    }

        /// <summary>
        /// Main benefit determination method
   /// </summary>
    public async Task<BenefitDeterminationResult> DetermineBenefitAsync(BenefitDeterminationRequest request)
        {
            var result = new BenefitDeterminationResult();

     try
     {
          _logger.LogInformation($"Determining benefits for service {request.ServiceCode}, coverage {request.CoverageId}");

        // Get service coverage
 var coverage = await GetServiceCoverageAsync(request.ServiceCode, request.CoverageId);
       if (coverage == null || !coverage.IsCovered)
    {
               result.IsCovered = false;
         result.CoverageStatus = "Excluded";
        result.DenialReason = "Service not covered under this plan";
           return result;
 }

        result.IsCovered = true;
     result.CoverageStatus = "Covered";

       // Get copay
      result.CopayAmount = await CalculateCopayAsync(request.ServiceCode, request.CoverageId, request.IsNetworkProvider);

    // Get coinsurance
      result.CoinsurancePercentage = await GetCoinsurancePercentAsync(request.ServiceCode, request.CoverageId, request.IsNetworkProvider);

                // Check limitations
   var limitation = await GetServiceLimitationAsync(request.ServiceCode, request.CoverageId);
       if (limitation != null && limitation.LimitationType == LimitationType.Frequency)
     {
           if (limitation.Quantity <= 0)
    {
  result.AppliedLimitations.Add($"Frequency limit reached: {limitation.Quantity} per {limitation.TimeFrame}");
       result.CoverageStatus = "Limited";
      }
                }

        // Check exclusions
        var exclusions = await GetBenefitExclusionsAsync(request.CoverageId);
     var applicableExclusions = exclusions.Where(e => e.ExcludedServiceCodes.Contains(request.ServiceCode)).ToList();
  if (applicableExclusions.Any())
          {
              result.AppliedExclusions.AddRange(applicableExclusions.Select(e => e.ExclusionDescription));
            result.CoverageStatus = "Excluded";
  result.IsCovered = false;
          result.DenialReason = $"Service excluded: {string.Join(", ", result.AppliedExclusions)}";
       }

                // Check authorization requirement
   result.RequiresPreAuthorization = await RequiresAuthorizationAsync(request.ServiceCode, request.CoverageId);

     // Get benefit period
  var period = await GetBenefitPeriodAsync(request.CoverageId);
 if (period != null && !period.IsActive)
   {
        result.CoverageStatus = "Inactive";
          result.IsCovered = false;
         result.DenialReason = "Benefit period not active";
              }

      _logger.LogInformation($"Benefit determination completed: {result.CoverageStatus}");
            }
            catch (Exception ex)
            {
      _logger.LogError(ex, $"Error determining benefit for service {request.ServiceCode}");
          result.IsCovered = false;
    result.CoverageStatus = "Error";
         result.DenialReason = $"Error processing benefit: {ex.Message}";
            }

      return result;
        }

        /// <summary>
   /// Get service coverage information
        /// </summary>
        public async Task<ServiceCoverageInfo> GetServiceCoverageAsync(string serviceCode, string coverageId)
    {
   try
            {
  _logger.LogInformation($"Retrieving coverage for service {serviceCode}");

      // This would query the database in production
                var coverage = new ServiceCoverageInfo
      {
            ServiceCode = serviceCode,
     IsCovered = !serviceCode.StartsWith("EXCL"),
          CoverageType = "Full",
         CopayAmount = 20,
       CoinsuranceInNetwork = 20,
         CoinsuranceOutOfNetwork = 40,
   DeductibleApplicable = 500,
RequiresAuthorization = serviceCode.StartsWith("AUTH")
   };

  return coverage;
      }
            catch (Exception ex)
  {
                _logger.LogError(ex, $"Error retrieving coverage for service {serviceCode}");
                return null;
   }
        }

        /// <summary>
 /// Get service limitation
        /// </summary>
        public async Task<ServiceLimitation> GetServiceLimitationAsync(string serviceCode, string coverageId)
        {
  try
        {
            _logger.LogInformation($"Retrieving limitation for service {serviceCode}");

    // Physical therapy limitation example
             if (serviceCode.StartsWith("PT"))
    {
                 return new ServiceLimitation
           {
        ServiceCode = serviceCode,
   LimitationType = LimitationType.Frequency,
  Quantity = 30,
 TimeFrame = "Calendar Year",
                    Description = "Limited to 30 visits per calendar year"
              };
         }

         return null;
            }
 catch (Exception ex)
     {
    _logger.LogError(ex, $"Error retrieving limitation for service {serviceCode}");
         return null;
            }
    }

        /// <summary>
  /// Get benefit exclusions
        /// </summary>
        public async Task<List<BenefitExclusion>> GetBenefitExclusionsAsync(string coverageId)
        {
            try
            {
                _logger.LogInformation($"Retrieving exclusions for coverage {coverageId}");

        var exclusions = new List<BenefitExclusion>
   {
          new BenefitExclusion
     {
    ExclusionCode = "EXC-001",
         ExclusionDescription = "Cosmetic procedures",
    ExcludedServiceCodes = new List<string> { "EXCL-COSMETIC" },
            Reason = "Not medically necessary"
   },
   new BenefitExclusion
  {
         ExclusionCode = "EXC-002",
       ExclusionDescription = "Experimental treatments",
  ExcludedServiceCodes = new List<string> { "EXCL-EXP" },
               Reason = "Not FDA approved"
             }
  };

  return exclusions;
            }
 catch (Exception ex)
{
         _logger.LogError(ex, $"Error retrieving exclusions for coverage {coverageId}");
    return new List<BenefitExclusion>();
 }
        }

        /// <summary>
        /// Calculate copay
  /// </summary>
    public async Task<decimal> CalculateCopayAsync(string serviceCode, string coverageId, bool isNetworkProvider)
        {
  try
            {
      // Network providers: $20 copay, Out-of-network: $50
       return isNetworkProvider ? 20m : 50m;
         }
        catch (Exception ex)
            {
      _logger.LogError(ex, $"Error calculating copay for service {serviceCode}");
       return 0m;
            }
        }

        /// <summary>
 /// Get coinsurance percentage
    /// </summary>
        public async Task<decimal> GetCoinsurancePercentAsync(string serviceCode, string coverageId, bool isNetworkProvider)
        {
 try
     {
       // Network providers: 20%, Out-of-network: 40%
 return isNetworkProvider ? 20m : 40m;
       }
 catch (Exception ex)
            {
    _logger.LogError(ex, $"Error getting coinsurance for service {serviceCode}");
       return 0m;
          }
  }

        /// <summary>
        /// Get frequency limit
     /// </summary>
      public async Task<FrequencyLimit> GetFrequencyLimitAsync(string serviceCode, string coverageId)
 {
    try
       {
          _logger.LogInformation($"Retrieving frequency limit for service {serviceCode}");

              if (serviceCode.StartsWith("PT"))
  {
      return new FrequencyLimit
  {
    ServiceCode = serviceCode,
     MaxFrequency = 30,
  FrequencyPeriod = "Calendar Year",
             CurrentUsage = 10,
              RemainingAllowed = 20,
   LimitReached = false
     };
        }

    return null;
 }
            catch (Exception ex)
      {
                _logger.LogError(ex, $"Error retrieving frequency limit for service {serviceCode}");
            return null;
         }
        }

        /// <summary>
        /// Get duration limit
/// </summary>
        public async Task<DurationLimit> GetDurationLimitAsync(string serviceCode, string coverageId)
 {
            try
      {
   _logger.LogInformation($"Retrieving duration limit for service {serviceCode}");

     if (serviceCode.StartsWith("REHAB"))
     {
         return new DurationLimit
           {
       ServiceCode = serviceCode,
               MaxDays = 90,
         DurationPeriod = "Per Condition",
 DaysUsed = 30,
  DaysRemaining = 60,
            LimitReached = false
           };
     }

     return null;
    }
       catch (Exception ex)
       {
   _logger.LogError(ex, $"Error retrieving duration limit for service {serviceCode}");
         return null;
      }
        }

  /// <summary>
        /// Get benefit period
        /// </summary>
        public async Task<BenefitPeriod> GetBenefitPeriodAsync(string coverageId)
      {
   try
        {
_logger.LogInformation($"Retrieving benefit period for coverage {coverageId}");

           return new BenefitPeriod
       {
                 PeriodStartDate = new DateTime(DateTime.Now.Year, 1, 1),
           PeriodEndDate = new DateTime(DateTime.Now.Year, 12, 31),
              PeriodType = "Calendar Year",
      IsActive = true
      };
            }
            catch (Exception ex)
            {
      _logger.LogError(ex, $"Error retrieving benefit period for coverage {coverageId}");
       return null;
   }
        }

        /// <summary>
     /// Get multi-tier benefit
 /// </summary>
        public async Task<MultiTierBenefit> GetMultiTierBenefitAsync(string serviceCode, string coverageId)
   {
         try
        {
             _logger.LogInformation($"Retrieving multi-tier benefit for service {serviceCode}");

     return new MultiTierBenefit
    {
       ServiceCode = serviceCode,
      Tiers = new List<BenefitTier>
        {
      new BenefitTier { TierLevel = 1, TierName = "In-Network", CopayAmount = 20, CoinsurancePercentage = 20, Covered = true },
    new BenefitTier { TierLevel = 2, TierName = "Out-of-Network", CopayAmount = 50, CoinsurancePercentage = 40, Covered = true },
            new BenefitTier { TierLevel = 3, TierName = "Non-Participating", CopayAmount = 0, CoinsurancePercentage = 100, Covered = false }
       }
          };
     }
            catch (Exception ex)
          {
  _logger.LogError(ex, $"Error retrieving multi-tier benefit for service {serviceCode}");
                return null;
  }
     }

      /// <summary>
   /// Check if service requires authorization
        /// </summary>
        public async Task<bool> RequiresAuthorizationAsync(string serviceCode, string coverageId)
        {
    try
            {
      // Services starting with AUTH require authorization
         return serviceCode.StartsWith("AUTH");
        }
            catch (Exception ex)
            {
        _logger.LogError(ex, $"Error checking authorization requirement for service {serviceCode}");
    return false;
            }
        }

        /// <summary>
        /// Get all benefits for coverage
  /// </summary>
     public async Task<List<CoverageBenefit>> GetAllBenefitsAsync(string coverageId)
        {
            try
         {
          _logger.LogInformation($"Retrieving all benefits for coverage {coverageId}");

         return new List<CoverageBenefit>
{
        new CoverageBenefit
          {
              BenefitCode = "MED-001",
           BenefitName = "Office Visits",
   BenefitCategory = "Medical",
    IsCovered = true,
CopayAmount = 20,
         CoinsurancePercentage = 20,
       AnnualMaximum = 1000
              },
        new CoverageBenefit
 {
       BenefitCode = "HOSP-001",
 BenefitName = "Inpatient Hospital",
    BenefitCategory = "Hospital",
         IsCovered = true,
            CopayAmount = 500,
           CoinsurancePercentage = 20,
            AnnualMaximum = 50000
           },
 new CoverageBenefit
    {
         BenefitCode = "PT-001",
          BenefitName = "Physical Therapy",
      BenefitCategory = "Rehabilitation",
IsCovered = true,
            CopayAmount = 30,
       CoinsurancePercentage = 20,
     AnnualMaximum = 2000
        }
                };
  }
      catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving benefits for coverage {coverageId}");
       return new List<CoverageBenefit>();
 }
        }
    }
}
