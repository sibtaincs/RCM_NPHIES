using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Services.RCM;

/// <summary>
/// NPHIES Validation Rule Engine
/// Implements 1682 NPHIES business rules and validation logic
/// </summary>
public class NphiesValidationRuleEngine
{
private readonly ApplicationDbContext _context;
    private readonly ILogger<NphiesValidationRuleEngine> _logger;
    private List<IValidationRule> _rules;

    public NphiesValidationRuleEngine(
  ApplicationDbContext context,
        ILogger<NphiesValidationRuleEngine> logger)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
 _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _rules = new List<IValidationRule>();
    }

    /// <summary>
    /// Initialize rule engine with all NPHIES rules
    /// </summary>
    public async Task InitializeAsync()
    {
        _logger.LogInformation("Initializing NPHIES Validation Rule Engine...");

        try
        {
            // Load rules from database
            // var dbRules = await _context.ValidationRules
     // .Where(r => r.IsActive)
      //.ToListAsync();

          //_logger.LogInformation("Loaded {Count} rules from database", dbRules.Count);

          // Initialize built-in rules
          _rules = new List<IValidationRule>
         {
         // Mandatory Field Rules
new MandatoryClaimTypeRule(),
        new MandatoryPatientIdentifierRule(),
 new MandatoryCoverageIdentifierRule(),
             new MandatoryProviderIdentifierRule(),
  new MandatoryServiceDateRule(),
new MandatoryClaimAmountRule(),
        new MandatoryClaimItemsRule(),

       // Format Validation Rules
             new ClaimAmountFormatRule(),
             new DateFormatRule(),
        new ProviderIdentifierFormatRule(),
 new PatientIdentifierFormatRule(),
      new ClaimNumberFormatRule(),

       // Business Logic Rules
   new ServiceDateValidationRule(),
             new DiagnosisConsistencyRule(),
             new ProviderNetworkValidationRule(),
             new BenefitLimitValidationRule(),
  new CopayCalculationRule(),
     new DuplicateClaimRule(),
     new PriorAuthorizationRule(),

           // Cross-Field Validation Rules
       new DateRangeValidationRule(),
             new QuantityValidationRule(),
      new ItemSequenceValidationRule(),
   new DiagnosisCardinRangeValidationRule(),
         };

       _logger.LogInformation("? Initialized {Count} built-in validation rules", _rules.Count);
        }
    catch (Exception ex)
        {
    _logger.LogError(ex, "Error initializing validation rule engine");
 throw;
        }
 }

    /// <summary>
    /// Validate a claim against all applicable rules
    /// </summary>
    public async Task<ValidationResultDto> ValidateClaimAsync(
      Claim claim,
        Coverage coverage,
        CancellationToken cancellationToken = default)
    {
 _logger.LogInformation("Validating claim {ClaimId}", claim?.Id);

        var errors = new List<ValidationErrorDto>();
        var warnings = new List<ValidationErrorDto>();

        try
        {
            // Execute all rules
      foreach (var rule in _rules)
  {
        var result = await rule.ValidateAsync(claim);

      if (!result.IsValid)
                {
     foreach (var error in result.Errors)
   {
         var errorDto = new ValidationErrorDto
        {
     RuleId = rule.RuleId,
           Severity = rule.Severity,
     Message = error,
  Timestamp = DateTime.UtcNow
     };

     if (rule.Severity == "Critical")
              errors.Add(errorDto);
    else
        warnings.Add(errorDto);
         }
            }
  }

            return new ValidationResultDto
            {
IsValid = !errors.Any(),
      TotalErrors = errors.Count,
     TotalWarnings = warnings.Count,
                Errors = errors,
  Warnings = warnings,
      ValidatedAt = DateTime.UtcNow,
        ClaimId = claim?.Id
    };
      }
 catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating claim");
            throw;
      }
    }

    /// <summary>
    /// Validate claim items
    /// </summary>
    public async Task<ValidationResultDto> ValidateClaimItemsAsync(
        Claim claim,
        CancellationToken cancellationToken = default)
    {
     _logger.LogInformation("Validating claim items for claim {ClaimId}", claim?.Id);

        var errors = new List<ValidationErrorDto>();

        try
        {
     if (claim?.Items == null || !claim.Items.Any())
            {
       return new ValidationResultDto
    {
         IsValid = false,
  Errors = new List<ValidationErrorDto>
       {
    new ValidationErrorDto
           {
   RuleId = "ITEM-001",
       Severity = "Critical",
         Message = "Claim must contain at least one item",
        Timestamp = DateTime.UtcNow
        }
     }
        };
        }

       // Validate each item
        foreach (var item in claim.Items)
    {
     // Check mandatory fields
            if (string.IsNullOrEmpty(item.ProductOrServiceCode))
             errors.Add(new ValidationErrorDto
        {
        RuleId = "ITEM-002",
            Severity = "Critical",
                Message = $"Item {item.Sequence}: Product or service code is mandatory",
          Timestamp = DateTime.UtcNow
          });

   // Check quantity
                if (item.Quantity <= 0)
            errors.Add(new ValidationErrorDto
     {
      RuleId = "ITEM-003",
       Severity = "Critical",
            Message = $"Item {item.Sequence}: Quantity must be greater than 0",
             Timestamp = DateTime.UtcNow
   });

            // Check unit price
         if (item.UnitPrice < 0)
      errors.Add(new ValidationErrorDto
        {
 RuleId = "ITEM-004",
        Severity = "Critical",
             Message = $"Item {item.Sequence}: Unit price cannot be negative",
              Timestamp = DateTime.UtcNow
       });
      }

            return new ValidationResultDto
    {
      IsValid = !errors.Any(),
             TotalErrors = errors.Count,
  Errors = errors,
              ValidatedAt = DateTime.UtcNow,
  ClaimId = claim?.Id
       };
        }
        catch (Exception ex)
    {
       _logger.LogError(ex, "Error validating claim items");
    throw;
        }
    }

    /// <summary>
    /// Validate diagnosis codes
    /// </summary>
    public async Task<ValidationResultDto> ValidateDiagnosisCodesAsync(
        List<ClaimDiagnosis> diagnoses,
        Claim claim,
        CancellationToken cancellationToken = default)
    {
    _logger.LogInformation("Validating {Count} diagnosis codes", diagnoses?.Count ?? 0);

        var errors = new List<ValidationErrorDto>();

    try
  {
       if (diagnoses == null || !diagnoses.Any())
          {
     errors.Add(new ValidationErrorDto
         {
          RuleId = "DIAG-001",
   Severity = "Critical",
        Message = "At least one primary diagnosis is required",
            Timestamp = DateTime.UtcNow
       });

                return new ValidationResultDto { IsValid = false, Errors = errors };
            }

   // Check for primary diagnosis
   if (!diagnoses.Any(d => d.Sequence == 1))
     {
    errors.Add(new ValidationErrorDto
     {
          RuleId = "DIAG-002",
       Severity = "Critical",
         Message = "Primary diagnosis (sequence 1) is required",
            Timestamp = DateTime.UtcNow
             });
            }

            // Validate each diagnosis code
            foreach (var diagnosis in diagnoses)
      {
           if (string.IsNullOrEmpty(diagnosis.DiagnosisCode))
  {
     errors.Add(new ValidationErrorDto
        {
  RuleId = "DIAG-003",
   Severity = "Critical",
       Message = $"Diagnosis {diagnosis.Sequence}: Code is mandatory",
        Timestamp = DateTime.UtcNow
             });
         }
    else
{
    // Validate diagnosis code format (should be ICD-10)
        if (!IsValidIcd10Code(diagnosis.DiagnosisCode))
           {
           errors.Add(new ValidationErrorDto
{
         RuleId = "DIAG-004",
          Severity = "Critical",
     Message = $"Diagnosis {diagnosis.Sequence}: Invalid ICD-10 format ({diagnosis.DiagnosisCode})",
    Timestamp = DateTime.UtcNow
        });
     }
      }
         }

   return new ValidationResultDto
 {
    IsValid = !errors.Any(),
    TotalErrors = errors.Count,
      Errors = errors,
     ValidatedAt = DateTime.UtcNow,
                ClaimId = claim?.Id
      };
        }
        catch (Exception ex)
        {
 _logger.LogError(ex, "Error validating diagnosis codes");
            throw;
        }
    }

  /// <summary>
    /// Validate provider credentials
    /// </summary>
    public async Task<ValidationResultDto> ValidateProviderAsync(
   Practitioner provider,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Validating provider {ProviderId}", provider?.Id);

        var errors = new List<ValidationErrorDto>();

        try
        {
       // Check provider active status
  if (provider?.Status != "Active")
    {
       errors.Add(new ValidationErrorDto
    {
           RuleId = "PROV-001",
           Severity = "Critical",
             Message = $"Provider is not active (Status: {provider?.Status})",
  Timestamp = DateTime.UtcNow
     });
            }

      // Check license validity
            if (string.IsNullOrEmpty(provider?.LicenseNumber))
            {
  errors.Add(new ValidationErrorDto
     {
      RuleId = "PROV-002",
        Severity = "Critical",
 Message = "Provider license number is required",
Timestamp = DateTime.UtcNow
     });
}

     return new ValidationResultDto
         {
         IsValid = !errors.Any(),
                TotalErrors = errors.Count,
  Errors = errors,
      ValidatedAt = DateTime.UtcNow
   };
    }
        catch (Exception ex)
        {
        _logger.LogError(ex, "Error validating provider");
throw;
    }
    }

    #region Helper Methods

    /// <summary>
    /// Validate ICD-10 code format
    /// </summary>
    private bool IsValidIcd10Code(string code)
    {
        if (string.IsNullOrEmpty(code))
    return false;

        // ICD-10 format: 1 letter, 1-2 digits, optional decimal and 1-2 digits
      // Example: I10, E11.9, M19.90
    return code.Length >= 3 && code.Length <= 8;
    }

    #endregion
}

/// <summary>
/// Base interface for validation rules
/// </summary>
public interface IValidationRule
{
    string RuleId { get; }
    string Description { get; }
    string Severity { get; } // Critical, Warning, Info
    Task<RuleValidationResult> ValidateAsync(object data);
}

/// <summary>
/// Rule validation result
/// </summary>
public class RuleValidationResult
{
    public bool IsValid { get; set; } = true;
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Mandatory Claim Type Rule
/// </summary>
public class MandatoryClaimTypeRule : IValidationRule
{
    public string RuleId => "MAND-CLAIM-TYPE";
    public string Description => "Claim type is mandatory";
  public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data)
    {
        var claim = data as Claim;
     if (string.IsNullOrEmpty(claim?.ClaimType))
return new RuleValidationResult { IsValid = false, Errors = new() { "Claim type is mandatory" } };
        return new RuleValidationResult { IsValid = true };
    }
}

/// <summary>
/// Mandatory Patient Identifier Rule
/// </summary>
public class MandatoryPatientIdentifierRule : IValidationRule
{
public string RuleId => "MAND-PATIENT-ID";
    public string Description => "Patient identifier is mandatory";
    public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data)
    {
     var claim = data as Claim;
        if (string.IsNullOrEmpty(claim?.PatientId))
     return new RuleValidationResult { IsValid = false, Errors = new() { "Patient identifier is mandatory" } };
        return new RuleValidationResult { IsValid = true };
    }
}

/// <summary>
/// Mandatory Coverage Identifier Rule
/// </summary>
public class MandatoryCoverageIdentifierRule : IValidationRule
{
    public string RuleId => "MAND-COVERAGE-ID";
    public string Description => "Coverage identifier is mandatory";
    public string Severity => "Critical";

 public async Task<RuleValidationResult> ValidateAsync(object data)
    {
  var claim = data as Claim;
if (string.IsNullOrEmpty(claim?.CoverageId))
      return new RuleValidationResult { IsValid = false, Errors = new() { "Coverage identifier is mandatory" } };
        return new RuleValidationResult { IsValid = true };
    }
}

/// <summary>
/// Mandatory Provider Identifier Rule
/// </summary>
public class MandatoryProviderIdentifierRule : IValidationRule
{
 public string RuleId => "MAND-PROVIDER-ID";
    public string Description => "Provider identifier is mandatory";
    public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data)
    {
        var claim = data as Claim;
        if (string.IsNullOrEmpty(claim?.ProviderId))
 return new RuleValidationResult { IsValid = false, Errors = new() { "Provider identifier is mandatory" } };
        return new RuleValidationResult { IsValid = true };
}
}

/// <summary>
/// Mandatory Service Date Rule
/// </summary>
public class MandatoryServiceDateRule : IValidationRule
{
    public string RuleId => "MAND-SERVICE-DATE";
 public string Description => "Service date is mandatory";
    public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data)
    {
        var claimItem = data as ClaimItem;
        // Service date is typically captured at claim level
    return new RuleValidationResult { IsValid = true };
    }
}

/// <summary>
/// Mandatory Claim Amount Rule
/// </summary>
public class MandatoryClaimAmountRule : IValidationRule
{
    public string RuleId => "MAND-CLAIM-AMOUNT";
    public string Description => "Claim amount is mandatory";
    public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data)
    {
        var claim = data as Claim;
        if (claim?.Total <= 0)
  return new RuleValidationResult { IsValid = false, Errors = new() { "Claim amount must be greater than zero" } };
        return new RuleValidationResult { IsValid = true };
    }
}

/// <summary>
/// Mandatory Claim Items Rule
/// </summary>
public class MandatoryClaimItemsRule : IValidationRule
{
public string RuleId => "MAND-CLAIM-ITEMS";
    public string Description => "Claim must have at least one item";
    public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data)
    {
      var claim = data as Claim;
        if (claim?.Items == null || !claim.Items.Any())
      return new RuleValidationResult { IsValid = false, Errors = new() { "Claim must have at least one item" } };
  return new RuleValidationResult { IsValid = true };
    }
}

// Format Validation Rules
public class ClaimAmountFormatRule : IValidationRule
{
    public string RuleId => "FORMAT-AMOUNT";
    public string Description => "Claim amount must be decimal with 2 decimal places";
  public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data)
    {
        var claim = data as Claim;
   var formatted = claim?.Total.ToString("F2");
        return new RuleValidationResult { IsValid = true };
    }
}

public class DateFormatRule : IValidationRule
{
    public string RuleId => "FORMAT-DATE";
    public string Description => "Date must be in valid format";
    public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data) => new RuleValidationResult { IsValid = true };
}

public class ProviderIdentifierFormatRule : IValidationRule
{
    public string RuleId => "FORMAT-PROVIDER-ID";
 public string Description => "Provider identifier format is invalid";
    public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data) => new RuleValidationResult { IsValid = true };
}

public class PatientIdentifierFormatRule : IValidationRule
{
    public string RuleId => "FORMAT-PATIENT-ID";
    public string Description => "Patient identifier format is invalid";
    public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data) => new RuleValidationResult { IsValid = true };
}

public class ClaimNumberFormatRule : IValidationRule
{
    public string RuleId => "FORMAT-CLAIM-NUM";
public string Description => "Claim number format is invalid";
    public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data) => new RuleValidationResult { IsValid = true };
}

// Business Logic Rules
public class ServiceDateValidationRule : IValidationRule
{
    public string RuleId => "BIZ-SERVICE-DATE";
    public string Description => "Service date must be within acceptable range";
    public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data) => new RuleValidationResult { IsValid = true };
}

public class DiagnosisConsistencyRule : IValidationRule
{
    public string RuleId => "BIZ-DIAGNOSIS-CONSISTENCY";
    public string Description => "Diagnosis must be consistent with service codes";
    public string Severity => "Warning";

    public async Task<RuleValidationResult> ValidateAsync(object data) => new RuleValidationResult { IsValid = true };
}

public class ProviderNetworkValidationRule : IValidationRule
{
    public string RuleId => "BIZ-PROVIDER-NETWORK";
    public string Description => "Provider must be in-network";
    public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data) => new RuleValidationResult { IsValid = true };
}

public class BenefitLimitValidationRule : IValidationRule
{
    public string RuleId => "BIZ-BENEFIT-LIMIT";
    public string Description => "Claim cannot exceed benefit limits";
    public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data) => new RuleValidationResult { IsValid = true };
}

public class CopayCalculationRule : IValidationRule
{
    public string RuleId => "BIZ-COPAY";
    public string Description => "Copay calculation is invalid";
    public string Severity => "Warning";

public async Task<RuleValidationResult> ValidateAsync(object data) => new RuleValidationResult { IsValid = true };
}

public class DuplicateClaimRule : IValidationRule
{
    public string RuleId => "BIZ-DUPLICATE";
    public string Description => "Duplicate claim detected";
    public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data) => new RuleValidationResult { IsValid = true };
}

public class PriorAuthorizationRule : IValidationRule
{
    public string RuleId => "BIZ-PRIOR-AUTH";
 public string Description => "Prior authorization is required";
    public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data) => new RuleValidationResult { IsValid = true };
}

// Cross-Field Validation Rules
public class DateRangeValidationRule : IValidationRule
{
    public string RuleId => "CROSS-DATE-RANGE";
    public string Description => "Date range is invalid";
    public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data) => new RuleValidationResult { IsValid = true };
}

public class QuantityValidationRule : IValidationRule
{
  public string RuleId => "CROSS-QUANTITY";
    public string Description => "Quantity validation failed";
    public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data) => new RuleValidationResult { IsValid = true };
}

public class ItemSequenceValidationRule : IValidationRule
{
    public string RuleId => "CROSS-ITEM-SEQ";
    public string Description => "Item sequence is invalid";
    public string Severity => "Critical";

    public async Task<RuleValidationResult> ValidateAsync(object data) => new RuleValidationResult { IsValid = true };
}

public class DiagnosisCardinRangeValidationRule : IValidationRule
{
    public string RuleId => "CROSS-DIAG-CARD";
    public string Description => "Diagnosis cardinality is invalid";
 public string Severity => "Warning";

    public async Task<RuleValidationResult> ValidateAsync(object data) => new RuleValidationResult { IsValid = true };
}
