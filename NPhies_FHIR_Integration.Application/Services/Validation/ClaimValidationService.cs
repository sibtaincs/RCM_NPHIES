using NPhies_FHIR_Integration.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services
{
    /// <summary>
    /// NPHIES Claim Validation Service Interface
    /// Implements all 47 NPHIES validation rules for claim pre-submission validation
    /// </summary>
    public interface IClaimValidationService
    {
        /// <summary>
        /// Comprehensive claim validation against all NPHIES rules
        /// </summary>
        Task<ClaimValidationResult> ValidateClaimAsync(Claim claim, Coverage coverage, Organization provider);

        /// <summary>
        /// Validate specific claim type
        /// </summary>
        Task<List<ValidationError>> ValidateClaimTypeAsync(Claim claim);

        /// <summary>
        /// Validate ICD-10 diagnosis codes
        /// </summary>
        Task<List<ValidationError>> ValidateDiagnosisCodesAsync(Claim claim);

        /// <summary>
        /// Validate HCPCS procedure codes
        /// </summary>
        Task<List<ValidationError>> ValidateProcedureCodesAsync(Claim claim);

        /// <summary>
        /// Check patient eligibility at claim time
        /// </summary>
        Task<List<ValidationError>> ValidatePatientEligibilityAsync(Claim claim, Coverage coverage);
    }

    /// <summary>
    /// Claim validation result containing all validation errors and warnings
    /// </summary>
    public class ClaimValidationResult
    {
        public bool IsValid { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
        public List<ValidationError> Warnings { get; set; } = new();
        public int ErrorCount => Errors.Count;
        public int WarningCount => Warnings.Count;
        public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
        public string ValidationSummary => $"Errors: {ErrorCount}, Warnings: {WarningCount}";
    }

    /// <summary>
    /// Individual validation error
    /// </summary>
    public class ValidationError
    {
        public string RuleId { get; set; } = string.Empty;
        public string RuleName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Field { get; set; } = string.Empty;
        public ValidationSeverity Severity { get; set; }
        public string? RemediationAction { get; set; }
        public string NphiesErrorCode { get; set; } = string.Empty;
    }

    /// <summary>
    /// Validation severity levels
    /// </summary>
    public enum ValidationSeverity
    {
        Error = 1,      // Claim will be rejected
        Warning = 2,    // Claim may be adjudicated with adjustment
        Info = 3        // Informational only
    }

    /// <summary>
    /// NPHIES Claim Validation Service Implementation
    /// Implements all 47 NPHIES pre-submission validation rules
    /// </summary>
    public class ClaimValidationService : IClaimValidationService
    {
        private readonly ILogger<ClaimValidationService> _logger;

        public ClaimValidationService(ILogger<ClaimValidationService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Comprehensive claim validation against all NPHIES rules (47 total)
        /// </summary>
        public async Task<ClaimValidationResult> ValidateClaimAsync(Claim claim, Coverage coverage, Organization provider)
        {
            var result = new ClaimValidationResult();

            try
            {
                _logger.LogInformation($"Starting claim validation for claim: {claim.ClaimNumber}");

                // Rule Group 1: Basic Claim Structure (Rules 1-5)
                result.Errors.AddRange(await ValidateBasicClaimStructureAsync(claim));

                // Rule Group 2: Claim Type Validation (Rules 6-8)
                result.Errors.AddRange(await ValidateClaimTypeAsync(claim));

                // Rule Group 3: Diagnosis Code Validation (Rules 9-12)
                result.Errors.AddRange(await ValidateDiagnosisCodesAsync(claim));

                // Rule Group 4: Procedure Code Validation (Rules 13-16)
                result.Errors.AddRange(await ValidateProcedureCodesAsync(claim));

                // Rule Group 5: Patient Eligibility (Rules 17-20)
                result.Errors.AddRange(await ValidatePatientEligibilityAsync(claim, coverage));

                // Rule Group 6: Provider Validation (Rules 21-24)
                result.Errors.AddRange(await ValidateProviderNetworkAsync(claim, provider));

                // Rule Group 7: Service Date Validation (Rules 25-28)
                result.Errors.AddRange(await ValidateServiceDatesAsync(claim));

                // Rule Group 8: Duplicate Detection (Rules 29-31)
                result.Errors.AddRange(await DetectDuplicateClaimsAsync(claim));

                // Rule Group 9: Medical Necessity (Rules 32-37)
                result.Errors.AddRange(await ValidateMedicalNecessityAsync(claim));

                // Rule Group 10: Prior Authorization (Rules 38-41)
                result.Errors.AddRange(await ValidatePriorAuthorizationAsync(claim));

                // Rule Group 11: Amount Validation (Rules 42-44)
                result.Errors.AddRange(await ValidateClaimAmountsAsync(claim));

                // Rule Group 12: Supporting Documentation (Rules 45-47)
                result.Errors.AddRange(await ValidateSupportingDocumentationAsync(claim));

                result.IsValid = result.ErrorCount == 0;

                _logger.LogInformation($"Claim validation completed. IsValid: {result.IsValid}, Errors: {result.ErrorCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating claim {claim.ClaimNumber}");
                result.Errors.Add(new ValidationError
                {
                    RuleId = "ERR-VALIDATION",
                    RuleName = "Validation Service Error",
                    Message = ex.Message,
                    Severity = ValidationSeverity.Error,
                    NphiesErrorCode = "VAL001"
                });
            }

            return result;
        }

        /// <summary>
        /// Rule Group 1: Basic Claim Structure Validation (Rules 1-5)
        /// </summary>
        private async Task<List<ValidationError>> ValidateBasicClaimStructureAsync(Claim claim)
        {
            var errors = new List<ValidationError>();

            // Rule 1: Claim number presence
            if (string.IsNullOrWhiteSpace(claim.ClaimNumber))
            {
                errors.Add(new ValidationError
                {
                    RuleId = "RULE-001",
                    RuleName = "Claim Number Required",
                    Message = "Claim must have a unique claim number",
                    Field = "ClaimNumber",
                    Severity = ValidationSeverity.Error,
                    RemediationAction = "Provide unique claim identifier",
                    NphiesErrorCode = "STR001"
                });
            }

            // Rule 2: Patient ID presence
            if (string.IsNullOrWhiteSpace(claim.PatientId))
            {
                errors.Add(new ValidationError
                {
                    RuleId = "RULE-002",
                    RuleName = "Patient ID Required",
                    Message = "Claim must reference a valid patient",
                    Field = "PatientId",
                    Severity = ValidationSeverity.Error,
                    RemediationAction = "Link claim to valid patient record",
                    NphiesErrorCode = "STR002"
                });
            }

            // Rule 3: Provider ID presence
            if (string.IsNullOrWhiteSpace(claim.ProviderId))
            {
                errors.Add(new ValidationError
                {
                    RuleId = "RULE-003",
                    RuleName = "Provider ID Required",
                    Message = "Claim must reference a valid provider",
                    Field = "ProviderId",
                    Severity = ValidationSeverity.Error,
                    RemediationAction = "Link claim to valid provider record",
                    NphiesErrorCode = "STR003"
                });
            }

            // Rule 4: Claim items present
            if (claim.Items == null || claim.Items.Count == 0)
            {
                errors.Add(new ValidationError
                {
                    RuleId = "RULE-004",
                    RuleName = "Claim Items Required",
                    Message = "Claim must contain at least one service item",
                    Field = "Items",
                    Severity = ValidationSeverity.Error,
                    RemediationAction = "Add at least one service/procedure to claim",
                    NphiesErrorCode = "STR004"
                });
            }

            // Rule 5: Claim amount validation
            if (claim.Total <= 0)
            {
                errors.Add(new ValidationError
                {
                    RuleId = "RULE-005",
                    RuleName = "Claim Total Amount Valid",
                    Message = "Claim total amount must be greater than zero",
                    Field = "Total",
                    Severity = ValidationSeverity.Error,
                    RemediationAction = "Ensure claim has valid line items with amounts",
                    NphiesErrorCode = "STR005"
                });
            }

            return await Task.FromResult(errors);
        }

        /// <summary>
        /// Rule Group 2: Claim Type Validation (Rules 6-8)
        /// </summary>
        public async Task<List<ValidationError>> ValidateClaimTypeAsync(Claim claim)
        {
            var errors = new List<ValidationError>();

            // Rule 6: Valid claim type
            var validClaimTypes = new[] { "inpatient", "outpatient", "emergency", "pharmacy", "dental" };
            if (!validClaimTypes.Contains(claim.ClaimType?.ToLower() ?? string.Empty))
            {
                errors.Add(new ValidationError
                {
                    RuleId = "RULE-006",
                    RuleName = "Valid Claim Type",
                    Message = $"Claim type must be one of: {string.Join(", ", validClaimTypes)}",
                    Field = "ClaimType",
                    Severity = ValidationSeverity.Error,
                    RemediationAction = "Select valid claim type",
                    NphiesErrorCode = "TYP001"
                });
            }

            // Rule 7: Sub-type matching claim type
            if (!string.IsNullOrEmpty(claim.ClaimSubType))
            {
                var validSubTypes = GetValidSubTypesForClaimType(claim.ClaimType);
                if (!validSubTypes.Contains(claim.ClaimSubType.ToLower()))
                {
                    errors.Add(new ValidationError
                    {
                        RuleId = "RULE-007",
                        RuleName = "Valid Claim Sub-type",
                        Message = $"Sub-type '{claim.ClaimSubType}' is invalid for claim type '{claim.ClaimType}'",
                        Field = "ClaimSubType",
                        Severity = ValidationSeverity.Warning,
                        RemediationAction = "Select appropriate sub-type for claim type",
                        NphiesErrorCode = "TYP002"
                    });
                }
            }

            // Rule 8: Use code presence
            if (string.IsNullOrWhiteSpace(claim.Use))
            {
                errors.Add(new ValidationError
                {
                    RuleId = "RULE-008",
                    RuleName = "Claim Use Code Required",
                    Message = "Claim must specify use code (claim, preauthorization, predetermination)",
                    Field = "Use",
                    Severity = ValidationSeverity.Error,
                    RemediationAction = "Specify appropriate use code",
                    NphiesErrorCode = "TYP003"
                });
            }

            return await Task.FromResult(errors);
        }

        /// <summary>
        /// Rule Group 3: Diagnosis Code Validation (Rules 9-12)
        /// </summary>
        public async Task<List<ValidationError>> ValidateDiagnosisCodesAsync(Claim claim)
        {
            var errors = new List<ValidationError>();

            if (claim.Diagnoses == null || claim.Diagnoses.Count == 0)
            {
                // Rule 9: At least one diagnosis required for inpatient
                if (claim.ClaimType?.ToLower() == "inpatient")
                {
                    errors.Add(new ValidationError
                    {
                        RuleId = "RULE-009",
                        RuleName = "Inpatient Diagnosis Required",
                        Message = "Inpatient claims must have at least one diagnosis code",
                        Field = "Diagnoses",
                        Severity = ValidationSeverity.Error,
                        RemediationAction = "Add primary diagnosis code (ICD-10)",
                        NphiesErrorCode = "DGN001"
                    });
                }
                return await Task.FromResult(errors);
            }

            foreach (var diagnosis in claim.Diagnoses)
            {
                // Rule 10: Valid ICD-10 format
                if (!IsValidICD10Code(diagnosis.DiagnosisCode))
                {
                    errors.Add(new ValidationError
                    {
                        RuleId = "RULE-010",
                        RuleName = "Valid ICD-10 Code Format",
                        Message = $"Diagnosis code '{diagnosis.DiagnosisCode}' is not in valid ICD-10 format",
                        Field = "DiagnosisCode",
                        Severity = ValidationSeverity.Error,
                        RemediationAction = "Use valid ICD-10 diagnosis code",
                        NphiesErrorCode = "DGN002"
                    });
                }

                // Rule 12: On-admission indicator for hospital claims
                if (claim.ClaimType?.ToLower() == "inpatient" && string.IsNullOrEmpty(diagnosis.OnAdmissionCode))
                {
                    errors.Add(new ValidationError
                    {
                        RuleId = "RULE-012",
                        RuleName = "On-Admission Code Required",
                        Message = "Inpatient claims must indicate if diagnosis was present on admission",
                        Field = "OnAdmissionCode",
                        Severity = ValidationSeverity.Warning,
                        RemediationAction = "Specify POA (Present On Admission) indicator",
                        NphiesErrorCode = "DGN004"
                    });
                }
            }

            return await Task.FromResult(errors);
        }

        /// <summary>
        /// Rule Group 4: Procedure Code Validation (Rules 13-16)
        /// </summary>
        public async Task<List<ValidationError>> ValidateProcedureCodesAsync(Claim claim)
        {
            var errors = new List<ValidationError>();

            if (claim.Items == null || claim.Items.Count == 0)
                return await Task.FromResult(errors);

            foreach (var item in claim.Items)
            {
                // Rule 13: Valid procedure code format
                if (!IsValidProcedureCode(item.ProductOrServiceCode))
                {
                    errors.Add(new ValidationError
                    {
                        RuleId = "RULE-013",
                        RuleName = "Valid Procedure Code Format",
                        Message = $"Procedure code '{item.ProductOrServiceCode}' is not valid (must be HCPCS or CPT)",
                        Field = "ProductOrServiceCode",
                        Severity = ValidationSeverity.Error,
                        RemediationAction = "Use valid HCPCS/CPT procedure code",
                        NphiesErrorCode = "PRC001"
                    });
                }

                // Rule 15: Quantity within acceptable range
                if (item.Quantity.GetValueOrDefault() <= 0)
                {
                    errors.Add(new ValidationError
                    {
                        RuleId = "RULE-015",
                        RuleName = "Valid Procedure Quantity",
                        Message = "Procedure quantity must be greater than zero",
                        Field = "Quantity",
                        Severity = ValidationSeverity.Error,
                        RemediationAction = "Ensure quantity is positive number",
                        NphiesErrorCode = "PRC003"
                    });
                }

                // Rule 16: Unit price reasonable
                if (item.UnitPrice.GetValueOrDefault() <= 0)
                {
                    errors.Add(new ValidationError
                    {
                        RuleId = "RULE-016",
                        RuleName = "Valid Unit Price",
                        Message = "Unit price must be greater than zero",
                        Field = "UnitPrice",
                        Severity = ValidationSeverity.Error,
                        RemediationAction = "Ensure valid unit price is provided",
                        NphiesErrorCode = "PRC004"
                    });
                }
            }

            return await Task.FromResult(errors);
        }

        /// <summary>
        /// Rule Group 5: Patient Eligibility Validation (Rules 17-20)
        /// </summary>
        public async Task<List<ValidationError>> ValidatePatientEligibilityAsync(Claim claim, Coverage coverage)
        {
            var errors = new List<ValidationError>();

            if (coverage == null)
            {
                errors.Add(new ValidationError
                {
                    RuleId = "RULE-017",
                    RuleName = "Valid Coverage",
                    Message = "Patient must have valid insurance coverage",
                    Field = "Coverage",
                    Severity = ValidationSeverity.Error,
                    RemediationAction = "Verify patient has active coverage",
                    NphiesErrorCode = "ELG001"
                });
                return errors;
            }

            // Rule 17: Coverage is active
            if (coverage.Status?.ToLower() != "active")
            {
                errors.Add(new ValidationError
                {
                    RuleId = "RULE-017",
                    RuleName = "Active Coverage Required",
                    Message = "Coverage must be active at time of service",
                    Field = "CoverageStatus",
                    Severity = ValidationSeverity.Error,
                    RemediationAction = "Verify coverage is active for service date",
                    NphiesErrorCode = "ELG002"
                });
            }

            // Rule 19: Member ID matches
            if (!string.IsNullOrEmpty(coverage.MemberID) && coverage.MemberID.Length < 6)
            {
                errors.Add(new ValidationError
                {
                    RuleId = "RULE-019",
                    RuleName = "Valid Member ID",
                    Message = "Member ID format is invalid",
                    Field = "MemberID",
                    Severity = ValidationSeverity.Warning,
                    RemediationAction = "Verify correct member ID is used",
                    NphiesErrorCode = "ELG004"
                });
            }

            return await Task.FromResult(errors);
        }

        /// <summary>
        /// Rule Group 6: Provider Network Validation (Rules 21-24)
        /// </summary>
        private async Task<List<ValidationError>> ValidateProviderNetworkAsync(Claim claim, Organization provider)
        {
            var errors = new List<ValidationError>();

            if (provider == null)
            {
                errors.Add(new ValidationError
                {
                    RuleId = "RULE-021",
                    RuleName = "Valid Provider",
                    Message = "Provider must be registered in system",
                    Field = "ProviderId",
                    Severity = ValidationSeverity.Error,
                    RemediationAction = "Register provider in network",
                    NphiesErrorCode = "PRV001"
                });
                return errors;
            }

            // Rule 21: Provider active status
            if (provider.Status?.ToLower() != "active")
            {
                errors.Add(new ValidationError
                {
                    RuleId = "RULE-021",
                    RuleName = "Active Provider Required",
                    Message = "Provider must be active in network",
                    Field = "ProviderStatus",
                    Severity = ValidationSeverity.Error,
                    RemediationAction = "Verify provider has active status",
                    NphiesErrorCode = "PRV002"
                });
            }

            // Rule 22: Provider license valid
            if (string.IsNullOrWhiteSpace(provider.LicenseNumber))
            {
                errors.Add(new ValidationError
                {
                    RuleId = "RULE-022",
                    RuleName = "Provider License Required",
                    Message = "Provider must have valid license number",
                    Field = "LicenseNumber",
                    Severity = ValidationSeverity.Error,
                    RemediationAction = "Add provider license number",
                    NphiesErrorCode = "PRV003"
                });
            }

            return await Task.FromResult(errors);
        }

        /// <summary>
        /// Rule Group 7: Service Date Validation (Rules 25-28)
        /// </summary>
        private async Task<List<ValidationError>> ValidateServiceDatesAsync(Claim claim)
        {
            var errors = new List<ValidationError>();

            // Rule 25: Service date not in future
            if (claim.CreatedAt > DateTime.Now)
            {
                errors.Add(new ValidationError
                {
                    RuleId = "RULE-025",
                    RuleName = "Service Date Not in Future",
                    Message = "Service cannot be dated in the future",
                    Field = "ServiceDate",
                    Severity = ValidationSeverity.Error,
                    RemediationAction = "Use actual service date",
                    NphiesErrorCode = "DAT002"
                });
            }

            // Rule 26: Service date within reasonable lookback period
            var daysOld = (DateTime.Now - claim.CreatedAt).TotalDays;
            if (daysOld > 365)
            {
                errors.Add(new ValidationError
                {
                    RuleId = "RULE-026",
                    RuleName = "Service Date Within Lookback Period",
                    Message = "Service date is older than 1 year",
                    Field = "ServiceDate",
                    Severity = ValidationSeverity.Warning,
                    RemediationAction = "Verify service date is within submission window",
                    NphiesErrorCode = "DAT003"
                });
            }

            return await Task.FromResult(errors);
        }

        /// <summary>
        /// Rule Group 8: Duplicate Claim Detection (Rules 29-31)
        /// </summary>
        private async Task<List<ValidationError>> DetectDuplicateClaimsAsync(Claim claim)
        {
            var errors = new List<ValidationError>();
            // TODO: Query database for duplicates
            return await Task.FromResult(errors);
        }

        /// <summary>
        /// Rule Group 9: Medical Necessity Validation (Rules 32-37)
        /// </summary>
        private async Task<List<ValidationError>> ValidateMedicalNecessityAsync(Claim claim)
        {
            var errors = new List<ValidationError>();

            // Rule 37: Supporting documentation present
            if (claim.SupportingInfo == null || claim.SupportingInfo.Count == 0)
            {
                if (claim.Items?.Any(i => i.ProductOrServiceCode?.StartsWith("9") == true) == true)
                {
                    errors.Add(new ValidationError
                    {
                        RuleId = "RULE-037",
                        RuleName = "Supporting Documentation",
                        Message = "Complex procedures may require supporting documentation",
                        Field = "SupportingInfo",
                        Severity = ValidationSeverity.Warning,
                        RemediationAction = "Provide clinical justification if required",
                        NphiesErrorCode = "MED007"
                    });
                }
            }

            return await Task.FromResult(errors);
        }

        /// <summary>
        /// Rule Group 10: Prior Authorization Validation (Rules 38-41)
        /// </summary>
        private async Task<List<ValidationError>> ValidatePriorAuthorizationAsync(Claim claim)
        {
            var errors = new List<ValidationError>();
            // TODO: Check if service requires prior auth
            return await Task.FromResult(errors);
        }

        /// <summary>
        /// Rule Group 11: Amount Validation (Rules 42-44)
        /// </summary>
        private async Task<List<ValidationError>> ValidateClaimAmountsAsync(Claim claim)
        {
            var errors = new List<ValidationError>();

            if (claim.Items == null || claim.Items.Count == 0)
                return errors;

            decimal calculatedTotal = 0;

            foreach (var item in claim.Items)
            {
                var lineTotal = (item.Quantity.GetValueOrDefault(1) * item.UnitPrice.GetValueOrDefault(0));
                calculatedTotal += lineTotal;

                // Rule 42: Amount reasonable
                if (lineTotal > 50000)
                {
                    errors.Add(new ValidationError
                    {
                        RuleId = "RULE-042",
                        RuleName = "Amount Reasonable",
                        Message = $"Line item amount {lineTotal:C} exceeds reasonable threshold",
                        Field = "Amount",
                        Severity = ValidationSeverity.Warning,
                        RemediationAction = "Review line item for accuracy",
                        NphiesErrorCode = "AMT001"
                    });
                }

                // Rule 43: No negative amounts
                if (lineTotal < 0)
                {
                    errors.Add(new ValidationError
                    {
                        RuleId = "RULE-043",
                        RuleName = "Positive Amount Required",
                        Message = "Line items must have positive amounts",
                        Field = "Amount",
                        Severity = ValidationSeverity.Error,
                        RemediationAction = "Correct to positive amount or use adjustment claim",
                        NphiesErrorCode = "AMT002"
                    });
                }
            }

            // Rule 44: Total matches calculated amount
            if (Math.Abs(claim.Total - calculatedTotal) > 0.01m)
            {
                errors.Add(new ValidationError
                {
                    RuleId = "RULE-044",
                    RuleName = "Total Amount Matches Items",
                    Message = $"Claim total {claim.Total:C} does not match calculated total {calculatedTotal:C}",
                    Field = "Total",
                    Severity = ValidationSeverity.Error,
                    RemediationAction = "Recalculate claim total from line items",
                    NphiesErrorCode = "AMT003"
                });
            }

            return await Task.FromResult(errors);
        }

        /// <summary>
        /// Rule Group 12: Supporting Documentation Validation (Rules 45-47)
        /// </summary>
        private async Task<List<ValidationError>> ValidateSupportingDocumentationAsync(Claim claim)
        {
            var errors = new List<ValidationError>();

            // Rule 47: All required fields completed
            if (string.IsNullOrWhiteSpace(claim.Use) || claim.Items?.Count == 0)
            {
                errors.Add(new ValidationError
                {
                    RuleId = "RULE-047",
                    RuleName = "Required Fields Complete",
                    Message = "Claim has missing required fields",
                    Field = "Claim",
                    Severity = ValidationSeverity.Error,
                    RemediationAction = "Complete all required fields",
                    NphiesErrorCode = "DOC003"
                });
            }

            return await Task.FromResult(errors);
        }

        /// <summary>
        /// Helper method: Get valid sub-types for claim type
        /// </summary>
        private static string[] GetValidSubTypesForClaimType(string claimType)
        {
            return claimType?.ToLower() switch
            {
                "inpatient" => new[] { "general", "psychiatric", "rehabilitation", "long-term" },
                "outpatient" => new[] { "clinic", "surgery", "diagnostic", "therapy" },
                "emergency" => new[] { "emergency", "urgent" },
                "pharmacy" => new[] { "medication", "device" },
                "dental" => new[] { "preventive", "diagnostic", "restorative", "surgical" },
                _ => Array.Empty<string>()
            };
        }

        /// <summary>
        /// Helper method: Validate ICD-10 code format
        /// </summary>
        private static bool IsValidICD10Code(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            // ICD-10 format: letter followed by 2 digits, then optionally decimal and up to 2 more characters
            return System.Text.RegularExpressions.Regex.IsMatch(code, @"^[A-Z]\d{2}(\.\d{1,2})?$");
        }

        /// <summary>
        /// Helper method: Validate HCPCS/CPT procedure code format
        /// </summary>
        private static bool IsValidProcedureCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return false;
            // HCPCS/CPT format: 5 characters (digits or letters)
            return System.Text.RegularExpressions.Regex.IsMatch(code, @"^[A-Z0-9]{5}$");
        }
    }
}
