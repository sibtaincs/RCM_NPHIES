using NPhies_FHIR_Integration.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Hl7.Fhir.Model;
using DomainCoverage = NPhies_FHIR_Integration.Domain.Entities.Coverage;
using DomainOrganization = NPhies_FHIR_Integration.Domain.Entities.Organization;
using DomainClaim = NPhies_FHIR_Integration.Domain.Entities.Claim;
using DomainPatient = NPhies_FHIR_Integration.Domain.Entities.Patient;
using DomainCoverageEligibilityRequest = NPhies_FHIR_Integration.Domain.Entities.CoverageEligibilityRequest;

namespace NPhies_FHIR_Integration.Application.Services
{
    /// <summary>
    /// NPHIES Eligibility Real-time Validation Service Interface
    /// Provides real-time eligibility checking and validation per NPHIES standards
    /// </summary>
    public interface IEligibilityValidationService
    {
        /// <summary>
        /// Verify patient eligibility in real-time
        /// </summary>
        Task<EligibilityVerificationResult> VerifyPatientEligibilityAsync(string patientId, string memberId, DateTime serviceDate);

        /// <summary>
        /// Check coverage period validity
        /// </summary>
        Task<bool> IsCoverageActiveAsync(DomainCoverage coverage, DateTime serviceDate);

        /// <summary>
        /// Get coverage deductible status
        /// </summary>
        Task<DeductibleInfo> GetDeductibleStatusAsync(DomainCoverage coverage, int year = 0);

        /// <summary>
        /// Get out-of-pocket tracking
        /// </summary>
        Task<OutOfPocketInfo> GetOutOfPocketStatusAsync(DomainCoverage coverage, int year = 0);

        /// <summary>
        /// Calculate copay for service
        /// </summary>
        Task<decimal> CalculateCopayAsync(DomainCoverage coverage, string serviceCode);

        /// <summary>
        /// Calculate coinsurance percentage
        /// </summary>
        Task<decimal> GetCoinsurancePercentAsync(DomainCoverage coverage, string serviceCode);

        /// <summary>
        /// Check if service has benefit limitation
        /// </summary>
        Task<BenefitLimitation> GetBenefitLimitationAsync(DomainCoverage coverage, string serviceCode);

        /// <summary>
        /// Validate benefit period and limits
        /// </summary>
        Task<BenefitPeriodValidation> ValidateBenefitPeriodAsync(DomainCoverage coverage, string serviceCode, DateTime serviceDate);

        /// <summary>
        /// Check if service is covered under plan
        /// </summary>
        Task<bool> IsServiceCoveredAsync(DomainCoverage coverage, string serviceCode, string diagnosis);

        /// <summary>
        /// Check exclusions for service
        /// </summary>
        Task<List<string>> GetServiceExclusionsAsync(DomainCoverage coverage, string serviceCode);

        /// <summary>
        /// Check waiting period requirements
        /// </summary>
        Task<WaitingPeriodInfo> GetWaitingPeriodAsync(DomainCoverage coverage, string serviceCode);

        /// <summary>
        /// Validate pre-authorization requirements
        /// </summary>
        Task<PreAuthRequirement> GetPreAuthRequirementAsync(DomainCoverage coverage, string serviceCode);

        /// <summary>
        /// Get network status for provider
        /// </summary>
        Task<NetworkStatus> GetNetworkStatusAsync(DomainOrganization provider, DomainCoverage coverage);

        /// <summary>
        /// Comprehensive eligibility check for claim submission
        /// </summary>
        Task<ComprehensiveEligibilityCheck> PerformComprehensiveEligibilityCheckAsync(
            DomainClaim claim, DomainCoverage coverage, DomainOrganization provider);
    }

    /// <summary>
    /// Eligibility verification result
    /// </summary>
    public class EligibilityVerificationResult
    {
        public bool IsEligible { get; set; }
        public string PatientId { get; set; } = string.Empty;
        public string MemberId { get; set; } = string.Empty;
        public DateTime VerificationDate { get; set; } = DateTime.UtcNow;
        public string CoverageStatus { get; set; } = string.Empty;
        public DateTime? CoverageEffectiveDate { get; set; }
        public DateTime? CoverageTerminationDate { get; set; }
        public List<string> EligibilityErrors { get; set; } = new();
        public string Reason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Deductible information
    /// </summary>
    public class DeductibleInfo
    {
        public decimal AnnualDeductible { get; set; }
        public decimal DeductibleMet { get; set; }
        public decimal DeductibleRemaining => AnnualDeductible - DeductibleMet;
        public decimal DeductibleMetPercentage => AnnualDeductible > 0 ? (DeductibleMet / AnnualDeductible) * 100 : 0;
        public bool IsDeductibleMet => DeductibleRemaining <= 0;
        public DateTime DeductibleResetDate { get; set; }
        public string DeductibleStatus => IsDeductibleMet ? "Met" : "Not Met";
    }

    /// <summary>
    /// Out-of-pocket tracking information
    /// </summary>
    public class OutOfPocketInfo
    {
        public decimal OutOfPocketMax { get; set; }
        public decimal OutOfPocketMet { get; set; }
        public decimal OutOfPocketRemaining => OutOfPocketMax - OutOfPocketMet;
        public decimal OutOfPocketMetPercentage => OutOfPocketMax > 0 ? (OutOfPocketMet / OutOfPocketMax) * 100 : 0;
        public bool IsOutOfPocketMaxMet => OutOfPocketRemaining <= 0;
        public DateTime OutOfPocketResetDate { get; set; }
        public string OutOfPocketStatus => IsOutOfPocketMaxMet ? "Max Met" : "Below Max";
    }

    /// <summary>
    /// Benefit limitation information
    /// </summary>
    public class BenefitLimitation
    {
        public string LimitationType { get; set; } = string.Empty; // Frequency, Quantity, Amount, Duration
        public string ServiceCode { get; set; } = string.Empty;
        public decimal LimitValue { get; set; }
        public string LimitUnit { get; set; } = string.Empty; // Per Visit, Per Year, Lifetime, etc.
        public decimal UsedValue { get; set; }
        public decimal RemainingValue => LimitValue - UsedValue;
        public bool IsLimitExceeded => RemainingValue <= 0;
        public DateTime LimitResetDate { get; set; }
        public string LimitationStatus => IsLimitExceeded ? "Exceeded" : "Within Limit";
    }

    /// <summary>
    /// Benefit period validation
    /// </summary>
    public class BenefitPeriodValidation
    {
        public bool IsValid { get; set; }
        public string BenefitPeriodType { get; set; } = string.Empty; // Calendar Year, Plan Year, Rolling
        public DateTime PeriodStartDate { get; set; }
        public DateTime PeriodEndDate { get; set; }
        public bool IsServiceDateInPeriod { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }

    /// <summary>
    /// Waiting period information
    /// </summary>
    public class WaitingPeriodInfo
    {
        public bool HasWaitingPeriod { get; set; }
        public int WaitingPeriodDays { get; set; }
        public DateTime CoverageStartDate { get; set; }
        public DateTime WaitingPeriodEndDate => CoverageStartDate.AddDays(WaitingPeriodDays);
        public bool IsWaitingPeriodSatisfied { get; set; }
        public string WaitingPeriodStatus => IsWaitingPeriodSatisfied ? "Satisfied" : "Not Satisfied";
        public int RemainingWaitingDays => Math.Max(0, (int)(WaitingPeriodEndDate - DateTime.Now).TotalDays);
    }

    /// <summary>
    /// Pre-authorization requirement
    /// </summary>
    public class PreAuthRequirement
    {
        public bool IsRequired { get; set; }
        public string ServiceCode { get; set; } = string.Empty;
        public string ServiceDescription { get; set; } = string.Empty;
        public List<string> RequiredDocuments { get; set; } = new();
        public int EstimatedProcessingDays { get; set; }
        public string AuthorizationLevel { get; set; } = string.Empty; // Routine, Urgent, Emergency
    }

    /// <summary>
    /// Network status information
    /// </summary>
    public class NetworkStatus
    {
        public bool IsInNetwork { get; set; }
        public string NetworkName { get; set; } = string.Empty;
        public decimal CopayAmount { get; set; }
        public decimal CoinsurancePercentage { get; set; }
        public decimal DeductibleAmount { get; set; }
        public decimal OutOfNetworkCopay { get; set; }
        public decimal OutOfNetworkCoinsurance { get; set; }
        public bool RequiresPriorAuth { get; set; }
        public string NetworkType { get; set; } = string.Empty; // HMO, PPO, EPO, etc.
    }

    /// <summary>
    /// Comprehensive eligibility check result
    /// </summary>
    public class ComprehensiveEligibilityCheck
    {
        public bool IsEligible { get; set; }
        public bool IsCoverageActive { get; set; }
        public bool IsProviderInNetwork { get; set; }
        public bool IsServiceCovered { get; set; }
        public bool IsBenefitAvailable { get; set; }
        public bool IsWaitingPeriodSatisfied { get; set; }
        public bool IsBenefitLimitExceeded { get; set; }
        public decimal EstimatedMemberResponsibility { get; set; }
        public decimal EstimatedPlanResponsibility { get; set; }
        public List<string> CheckErrors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public DateTime CheckTimestamp { get; set; } = DateTime.UtcNow;
        public string OverallStatus => IsEligible ? "Eligible" : "Not Eligible";
    }

    /// <summary>
    /// NPHIES Eligibility Real-time Validation Service Implementation
    /// </summary>
    public class EligibilityValidationService : IEligibilityValidationService
    {
        private readonly ILogger<EligibilityValidationService> _logger;

        public EligibilityValidationService(ILogger<EligibilityValidationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Verify patient eligibility in real-time
        /// </summary>
        public async Task<EligibilityVerificationResult> VerifyPatientEligibilityAsync(
                    string patientId, string memberId, DateTime serviceDate)
        {
            var result = new EligibilityVerificationResult
            {
                PatientId = patientId,
                MemberId = memberId
            };

            try
            {
                _logger.LogInformation($"Verifying eligibility for patient: {patientId}, member: {memberId}, service date: {serviceDate:yyyy-MM-dd}");

                // Rule 1: Validate patient ID presence
                if (string.IsNullOrWhiteSpace(patientId))
                {
                    result.EligibilityErrors.Add("Patient ID is required");
                    result.Reason = "Missing patient identifier";
                    return result;
                }

                // Rule 2: Validate member ID presence
                if (string.IsNullOrWhiteSpace(memberId))
                {
                    result.EligibilityErrors.Add("Member ID is required");
                    result.Reason = "Missing member identifier";
                    return result;
                }

                // Rule 3: Validate service date not in future
                if (serviceDate.Date > DateTime.Now.Date)
                {
                    result.EligibilityErrors.Add("Service date cannot be in the future");
                    result.Reason = "Invalid service date";
                    return result;
                }

                // Rule 4: Check member ID format (SAR requirement: minimum 6 characters)
                if (memberId.Length < 6)
                {
                    result.EligibilityErrors.Add("Member ID format is invalid (minimum 6 characters required)");
                    result.Reason = "Invalid member ID format";
                    return result;
                }

                // If all basic validations pass
                result.IsEligible = true;
                result.CoverageStatus = "Active";
                result.Reason = "Patient is eligible for coverage";

                _logger.LogInformation($"Eligibility verification completed. IsEligible: {result.IsEligible}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error verifying eligibility for patient {patientId}");
                result.EligibilityErrors.Add($"Error during eligibility verification: {ex.Message}");
                result.Reason = "System error during verification";
            }

            return result;
        }

        /// <summary>
        /// Check if coverage is active on service date
        /// </summary>
        public async Task<bool> IsCoverageActiveAsync(DomainCoverage coverage, DateTime serviceDate)
        {
            try
            {
                // Rule 5: Coverage status check
                if (coverage == null)
                    return false;

                if (coverage.Status?.ToLower() != "active")
                    return false;

                // Rule 6: Coverage effective date check
                // NOTE: Claim entity uses CreatedAt, Coverage uses implicit period tracking
                // Service date validation would use claim service period

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking coverage active status");
                return false;
            }
        }

        /// <summary>
        /// Get deductible status
        /// </summary>
        public async Task<DeductibleInfo> GetDeductibleStatusAsync(DomainCoverage coverage, int year = 0)
        {
            var info = new DeductibleInfo();

            try
            {
                if (coverage == null)
                    return info;

                // Rule 7: Calculate deductible status
                info.AnnualDeductible = coverage.AnnualDeductible;
                info.DeductibleMet = coverage.DeductibleMet;
                info.DeductibleResetDate = DateTime.Now.AddMonths(12); // TODO: Use plan year

                _logger.LogInformation($"Deductible Status - Annual: {info.AnnualDeductible:C}, Met: {info.DeductibleMet:C}, Remaining: {info.DeductibleRemaining:C}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving deductible status");
            }

            return info;
        }

        /// <summary>
        /// Get out-of-pocket tracking
        /// </summary>
        public async Task<OutOfPocketInfo> GetOutOfPocketStatusAsync(DomainCoverage coverage, int year = 0)
        {
            var info = new OutOfPocketInfo();

            try
            {
                if (coverage == null)
                    return info;

                // Rule 8: Calculate out-of-pocket status
                info.OutOfPocketMax = coverage.OutOfPocketMax;
                // TODO: Track actual out-of-pocket met from claims

                _logger.LogInformation($"Out-of-Pocket Status - Max: {info.OutOfPocketMax:C}, Remaining: {info.OutOfPocketRemaining:C}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving out-of-pocket status");
            }

            return info;
        }

        /// <summary>
        /// Calculate copay for service
        /// </summary>
        public async Task<decimal> CalculateCopayAsync(DomainCoverage coverage, string serviceCode)
        {
            try
            {
                // Rule 9: Copay calculation based on service type
                if (coverage == null)
                    return 0;

                // TODO: Lookup copay amount from CoverageType and service classification
                decimal copay = coverage.Copay;

                _logger.LogInformation($"Copay for service {serviceCode}: {copay:C}");
                return copay;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error calculating copay for service {serviceCode}");
                return 0;
            }
        }

        /// <summary>
        /// Get coinsurance percentage
        /// </summary>
        public async Task<decimal> GetCoinsurancePercentAsync(DomainCoverage coverage, string serviceCode)
        {
            try
            {
                // Rule 10: Coinsurance percentage lookup
                if (coverage == null)
                    return 0;

                decimal coinsurance = coverage.CoinsurancePercent;

                _logger.LogInformation($"Coinsurance for service {serviceCode}: {coinsurance}%");
                return coinsurance;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving coinsurance for service {serviceCode}");
                return 0;
            }
        }

        /// <summary>
        /// Get benefit limitation
        /// </summary>
        public async Task<BenefitLimitation> GetBenefitLimitationAsync(DomainCoverage coverage, string serviceCode)
        {
            var limitation = new BenefitLimitation
            {
                ServiceCode = serviceCode
            };

            try
            {
                // Rule 11: Benefit limitation lookup
                // TODO: Query BenefitCodeMaster for service limitations

                _logger.LogInformation($"Benefit limitation for service {serviceCode}: {limitation.LimitationStatus}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving benefit limitation for service {serviceCode}");
            }

            return limitation;
        }

        /// <summary>
        /// Validate benefit period
        /// </summary>
        public async Task<BenefitPeriodValidation> ValidateBenefitPeriodAsync(
             DomainCoverage coverage, string serviceCode, DateTime serviceDate)
        {
            var validation = new BenefitPeriodValidation { IsValid = true };

            try
            {
                // Rule 12: Benefit period validation (Calendar year, plan year, or rolling)
                validation.BenefitPeriodType = "Calendar Year";
                validation.PeriodStartDate = new DateTime(serviceDate.Year, 1, 1);
                validation.PeriodEndDate = new DateTime(serviceDate.Year, 12, 31);
                validation.IsServiceDateInPeriod = serviceDate >= validation.PeriodStartDate && serviceDate <= validation.PeriodEndDate;

                if (!validation.IsServiceDateInPeriod)
                {
                    validation.IsValid = false;
                    validation.ValidationErrors.Add($"Service date {serviceDate:yyyy-MM-dd} is outside benefit period");
                }

                _logger.LogInformation($"Benefit period validation: {validation.BenefitPeriodType} - {validation.PeriodStartDate:yyyy-MM-dd} to {validation.PeriodEndDate:yyyy-MM-dd}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating benefit period");
                validation.IsValid = false;
                validation.ValidationErrors.Add($"Error: {ex.Message}");
            }

            return validation;
        }

        /// <summary>
        /// Check if service is covered
        /// </summary>
        public async Task<bool> IsServiceCoveredAsync(DomainCoverage coverage, string serviceCode, string diagnosis)
        {
            try
            {
                // Rule 13: Service coverage check
                if (coverage == null)
                    return false;

                // TODO: Check ServiceCodeMaster and PolicyBenefitCoverage
                // Validate service code against plan benefits

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking if service {serviceCode} is covered");
                return false;
            }
        }

        /// <summary>
        /// Get service exclusions
        /// </summary>
        public async Task<List<string>> GetServiceExclusionsAsync(DomainCoverage coverage, string serviceCode)
        {
            var exclusions = new List<string>();

            try
            {
                // Rule 14: Get exclusions for service
                // TODO: Query plan exclusion rules
                // Common exclusions: cosmetic, experimental, not medically necessary

                _logger.LogInformation($"Service exclusions for {serviceCode}: {exclusions.Count} found");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving exclusions for service {serviceCode}");
            }

            return exclusions;
        }

        /// <summary>
        /// Get waiting period information
        /// </summary>
        public async Task<WaitingPeriodInfo> GetWaitingPeriodAsync(DomainCoverage coverage, string serviceCode)
        {
            var waitingPeriod = new WaitingPeriodInfo();

            try
            {
                // Rule 15: Waiting period validation (service-specific)
                // TODO: Check if service requires waiting period
                // Examples: dental - 12 months, vision - 6 months

                _logger.LogInformation($"Waiting period for service {serviceCode}: {waitingPeriod.WaitingPeriodStatus}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving waiting period for service {serviceCode}");
            }

            return waitingPeriod;
        }

        /// <summary>
        /// Get pre-authorization requirement
        /// </summary>
        public async Task<PreAuthRequirement> GetPreAuthRequirementAsync(DomainCoverage coverage, string serviceCode)
        {
            var requirement = new PreAuthRequirement
            {
                ServiceCode = serviceCode,
                IsRequired = false // Default - would be looked up from ClaimSubmissionRules
            };

            try
            {
                // Rule 16: Pre-authorization requirement
                // TODO: Check ClaimSubmissionRules for service

                if (requirement.IsRequired)
                {
                    requirement.RequiredDocuments.Add("Medical justification");
                    requirement.AuthorizationLevel = "Routine";
                    requirement.EstimatedProcessingDays = 3;
                }

                _logger.LogInformation($"Pre-auth requirement for service {serviceCode}: {(requirement.IsRequired ? "Required" : "Not Required")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking pre-auth requirement for service {serviceCode}");
            }

            return requirement;
        }

        /// <summary>
        /// Get network status
        /// </summary>
        public async Task<NetworkStatus> GetNetworkStatusAsync(DomainOrganization provider, DomainCoverage coverage)
        {
            var status = new NetworkStatus
            {
                IsInNetwork = true // Default - would be looked up
            };

            try
            {
                // Rule 17: Network status validation (in-network vs out-of-network pricing)
                if (provider == null || coverage == null)
                {
                    status.IsInNetwork = false;
                    return status;
                }

                // TODO: Check provider network membership
                // TODO: Set copay/coinsurance based on network status
                status.NetworkName = "Primary Network";
                status.CopayAmount = coverage.Copay;
                status.CoinsurancePercentage = coverage.CoinsurancePercent;

                _logger.LogInformation($"Network status: {(status.IsInNetwork ? "In-Network" : "Out-of-Network")}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking network status");
            }

            return status;
        }

        /// <summary>
        /// Perform comprehensive eligibility check
        /// </summary>
        public async Task<ComprehensiveEligibilityCheck> PerformComprehensiveEligibilityCheckAsync(
        DomainClaim claim, DomainCoverage coverage, DomainOrganization provider)
        {
            var check = new ComprehensiveEligibilityCheck();

            try
            {
                _logger.LogInformation($"Starting comprehensive eligibility check for claim {claim.ClaimNumber}");

                // Check 1: Coverage active
                check.IsCoverageActive = await IsCoverageActiveAsync(coverage, DateTime.Now);
                if (!check.IsCoverageActive)
                {
                    check.CheckErrors.Add("Coverage is not active");
                }

                // Check 2: Provider network status
                var networkStatus = await GetNetworkStatusAsync(provider, coverage);
                check.IsProviderInNetwork = networkStatus.IsInNetwork;

                // Check 3: Service coverage (if claim has items)
                if (claim.Items?.Count > 0)
                {
                    var firstItem = claim.Items.First();
                    check.IsServiceCovered = await IsServiceCoveredAsync(coverage, firstItem.ProductOrServiceCode, "");
                    if (!check.IsServiceCovered)
                    {
                        check.CheckErrors.Add($"Service {firstItem.ProductOrServiceCode} is not covered");
                    }

                    // Check 4: Benefit available
                    var benefitPeriod = await ValidateBenefitPeriodAsync(coverage, firstItem.ProductOrServiceCode, DateTime.Now);
                    check.IsBenefitAvailable = benefitPeriod.IsValid;
                    if (!benefitPeriod.IsValid)
                    {
                        check.CheckErrors.AddRange(benefitPeriod.ValidationErrors);
                    }

                    // Check 5: Waiting period
                    var waitingPeriod = await GetWaitingPeriodAsync(coverage, firstItem.ProductOrServiceCode);
                    check.IsWaitingPeriodSatisfied = waitingPeriod.IsWaitingPeriodSatisfied;
                    if (!check.IsWaitingPeriodSatisfied)
                    {
                        check.Warnings.Add($"Waiting period not satisfied. {waitingPeriod.RemainingWaitingDays} days remaining");
                    }

                    // Check 6: Benefit limit not exceeded
                    var limitation = await GetBenefitLimitationAsync(coverage, firstItem.ProductOrServiceCode);
                    check.IsBenefitLimitExceeded = limitation.IsLimitExceeded;
                    if (check.IsBenefitLimitExceeded)
                    {
                        check.CheckErrors.Add($"Benefit limit exceeded for {limitation.LimitationType}");
                    }

                    // Calculate member responsibility
                    var copay = await CalculateCopayAsync(coverage, firstItem.ProductOrServiceCode);
                    var coinsurance = await GetCoinsurancePercentAsync(coverage, firstItem.ProductOrServiceCode);
                    check.EstimatedMemberResponsibility = copay + (claim.Total * (coinsurance / 100));
                    check.EstimatedPlanResponsibility = claim.Total - check.EstimatedMemberResponsibility;
                }

                // Overall eligibility determination
                check.IsEligible = check.IsCoverageActive &&
           check.IsServiceCovered &&
                   check.IsBenefitAvailable &&
              check.IsWaitingPeriodSatisfied &&
             !check.IsBenefitLimitExceeded &&
                   check.CheckErrors.Count == 0;

                _logger.LogInformation($"Comprehensive eligibility check completed. IsEligible: {check.IsEligible}, Errors: {check.CheckErrors.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error performing comprehensive eligibility check for claim {claim.ClaimNumber}");
                check.CheckErrors.Add($"System error: {ex.Message}");
                check.IsEligible = false;
            }

            return check;
        }
    }


    public class Encounter : BaseEntity
    {
        public string EncounterId { get; set; }
        public string Status { get; set; } // planned, arrived, in-progress, finished
        public string Class { get; set; } // AMB, IMP, EMER, HH
        public string Type { get; set; }
        public DateTime Period_Start { get; set; }
        public DateTime? Period_End { get; set; }
        public string PatientId { get; set; }
        public virtual DomainPatient Patient { get; set; }
        public virtual ICollection<DomainClaim> Claims { get; set; }
    }
}
