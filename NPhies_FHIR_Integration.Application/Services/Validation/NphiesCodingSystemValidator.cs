using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NPhies_FHIR_Integration.Application.Services.Validation
{
    /// <summary>
    /// NPHIES Coding Systems Validator
    /// Validates all coding systems against NPHIES approved standards
    /// Implements NPHIES terminology validation
    /// </summary>
    public interface INphiesCodingSystemValidator
    {
        Task<CodingValidationResult> ValidateServiceCodeAsync(string code);
        Task<CodingValidationResult> ValidateDiagnosisCodeAsync(string code);
        Task<CodingValidationResult> ValidateProcedureCodeAsync(string code);
        Task<CodingValidationResult> ValidateProductCodeAsync(string code);
        Task<CodingValidationResult> ValidateClaimTypeAsync(string claimType);
        Task<List<CodingError>> ValidateAllCodesInClaimAsync(ClaimCodeValidationDto claim);
        Task<bool> IsCodeInNphiesSystemAsync(string codeSystem, string code);
    }

    /// <summary>
    /// Coding validation result
    /// </summary>
    public class CodingValidationResult
    {
        public bool IsValid { get; set; }
        public string Code { get; set; } = string.Empty;
        public string CodeSystem { get; set; } = string.Empty;
    public string CodeSystemName { get; set; } = string.Empty;
     public string Description { get; set; } = string.Empty;
public List<CodingError> Errors { get; set; } = new();
 public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Coding error details
    /// </summary>
    public class CodingError
    {
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string InvalidCode { get; set; } = string.Empty;
  public string ExpectedSystem { get; set; } = string.Empty;
        public string CodeType { get; set; } = string.Empty;
    }

    /// <summary>
    /// Claim codes for validation
    /// </summary>
  public class ClaimCodeValidationDto
    {
        public string ClaimType { get; set; } = string.Empty;
        public List<string> ServiceCodes { get; set; } = new();
      public List<string> DiagnosisCodes { get; set; } = new();
      public List<string> ProcedureCodes { get; set; } = new();
    }

    /// <summary>
    /// NPHIES Coding Systems Validator Implementation
    /// </summary>
    public class NphiesCodingSystemValidator : INphiesCodingSystemValidator
    {
        private readonly ILogger<NphiesCodingSystemValidator> _logger;

        // NPHIES Approved Code Systems
      private static readonly Dictionary<string, string> NphiesCodeSystems = new()
  {
   { "service", "http://hl7.org/fhir/ValueSet/service-uscls" },
       { "diagnosis", "http://hl7.org/fhir/sid/icd-10" },
         { "procedure", "http://hl7.org/fhir/sid/icd-10-pcs" },
   { "claimType", "http://hl7.org/fhir/CodeSystem/claim-type" },
    { "patient-relationship", "http://hl7.org/fhir/CodeSystem/subscriber-relationship" }
        };

        // NPHIES Approved Service Codes (CPT)
        private static readonly HashSet<string> ApprovedServiceCodes = new()
        {
            // Office visits
    "99213", "99214", "99215", "99203", "99204", "99205",
         // Psychotherapy
     "90834", "90837", "90847",
     // Lab tests
     "80053", "80055", "80057",
       // Imaging
            "70450", "70460", "71020", "71030",
         // E&M codes
            "99283", "99284", "99285",
            // Preventive care
            "99387", "99397", "99401", "99402"
 };

        // NPHIES Approved Diagnosis Codes (ICD-10)
        private static readonly HashSet<string> ApprovedDiagnosisCodes = new()
        {
  // Common diagnoses
  "E11.9",   // Type 2 diabetes
     "I10",     // Hypertension
       "J44.9",   // COPD
        "M79.3",   // Paniculitis
     "F41.1",   // Generalized anxiety
            "E78.5",   // Hyperlipidemia
 "M25.5",   // Joint pain
  "N18.3",   // Chronic kidney disease
        "I25.10",  // Coronary artery disease
  "F32.9"    // Depression
        };

        // NPHIES Valid Claim Types
        private static readonly HashSet<string> ValidClaimTypes = new()
        {
            "institutional", "oral", "pharmacy", "professional", "vision"
        };

        // NPHIES Valid Product Categories
   private static readonly HashSet<string> ValidProductCodes = new()
      {
  "1", "2", "3", "4", "5", "6", "7", "8", "9", "10"
        };

public NphiesCodingSystemValidator(ILogger<NphiesCodingSystemValidator> logger)
        {
 _logger = logger;
   }

    /// <summary>
        /// Validates service code against NPHIES approved list
        /// </summary>
        public async Task<CodingValidationResult> ValidateServiceCodeAsync(string code)
        {
         try
     {
     _logger.LogInformation($"Validating service code: {code}");

  var result = new CodingValidationResult
      {
         Code = code,
   CodeSystem = NphiesCodeSystems["service"],
            CodeSystemName = "CPT / Healthcare Services"
        };

      if (string.IsNullOrWhiteSpace(code))
        {
    result.IsValid = false;
   result.Errors.Add(new CodingError
     {
       ErrorCode = "NPHIES-SVC-001",
           ErrorMessage = "Service code is required",
        InvalidCode = code,
     CodeType = "Service"
         });
     return result;
         }

       // Validate format (should be numeric for CPT codes)
       if (!Regex.IsMatch(code, @"^\d{5}$"))
       {
   result.Warnings.Add($"Service code '{code}' does not match standard 5-digit CPT format");
    }

         if (ApprovedServiceCodes.Contains(code))
     {
           result.IsValid = true;
           result.Description = GetServiceDescription(code);
    }
                else
     {
             result.IsValid = false;
        result.Errors.Add(new CodingError
        {
         ErrorCode = "NPHIES-SVC-002",
       ErrorMessage = $"Service code '{code}' is not in NPHIES approved list",
       InvalidCode = code,
        ExpectedSystem = NphiesCodeSystems["service"],
     CodeType = "Service"
           });
                }

      return result;
    }
    catch (Exception ex)
            {
_logger.LogError(ex, "Error validating service code");
  return new CodingValidationResult
      {
           IsValid = false,
   Code = code,
          Errors = new List<CodingError> { new() { ErrorMessage = ex.Message } }
        };
     }
      }

        /// <summary>
        /// Validates diagnosis code against NPHIES approved ICD-10 list
        /// </summary>
        public async Task<CodingValidationResult> ValidateDiagnosisCodeAsync(string code)
   {
     try
            {
   _logger.LogInformation($"Validating diagnosis code: {code}");

      var result = new CodingValidationResult
    {
        Code = code,
   CodeSystem = NphiesCodeSystems["diagnosis"],
      CodeSystemName = "ICD-10-SA / WHO"
             };

         if (string.IsNullOrWhiteSpace(code))
            {
   result.IsValid = false;
          result.Errors.Add(new CodingError
         {
 ErrorCode = "NPHIES-DX-001",
       ErrorMessage = "Diagnosis code is required",
         InvalidCode = code,
  CodeType = "Diagnosis"
   });
 return result;
       }

        // Validate ICD-10 format
        if (!IsValidIcd10Format(code))
        {
                result.IsValid = false;
        result.Errors.Add(new CodingError
    {
            ErrorCode = "NPHIES-DX-002",
            ErrorMessage = $"Diagnosis code '{code}' does not match ICD-10 format (expected: Letter + 2 digits + optional . + 1-2 digits)",
 InvalidCode = code,
           ExpectedSystem = NphiesCodeSystems["diagnosis"],
     CodeType = "Diagnosis"
            });
        return result;
           }

           if (ApprovedDiagnosisCodes.Contains(code))
           {
   result.IsValid = true;
                    result.Description = GetDiagnosisDescription(code);
    }
         else
                {
      // Allow with warning for unlisted but properly formatted ICD-10 codes
     result.IsValid = true;
     result.Warnings.Add($"Diagnosis code '{code}' not found in sample approved list but has valid ICD-10 format");
       }

     return result;
       }
  catch (Exception ex)
            {
          _logger.LogError(ex, "Error validating diagnosis code");
       return new CodingValidationResult { IsValid = false, Code = code };
    }
   }

        /// <summary>
        /// Validates procedure code
        /// </summary>
     public async Task<CodingValidationResult> ValidateProcedureCodeAsync(string code)
        {
     try
            {
      _logger.LogInformation($"Validating procedure code: {code}");

     var result = new CodingValidationResult
         {
     Code = code,
 CodeSystem = NphiesCodeSystems["procedure"],
   CodeSystemName = "ICD-10-PCS"
     };

    if (string.IsNullOrWhiteSpace(code))
       {
              result.IsValid = false;
          result.Errors.Add(new CodingError
        {
        ErrorCode = "NPHIES-PROC-001",
          ErrorMessage = "Procedure code is required",
  CodeType = "Procedure"
        });
          return result;
            }

         // Validate ICD-10-PCS format (7 alphanumeric characters)
     if (!IsValidIcd10PcsFormat(code))
      {
    result.IsValid = false;
           result.Errors.Add(new CodingError
     {
     ErrorCode = "NPHIES-PROC-002",
  ErrorMessage = $"Procedure code '{code}' does not match ICD-10-PCS format (expected: 7 alphanumeric characters)",
    InvalidCode = code,
         CodeType = "Procedure"
 });
      }
     else
{
           result.IsValid = true;
       }

         return result;
            }
    catch (Exception ex)
            {
      _logger.LogError(ex, "Error validating procedure code");
       return new CodingValidationResult { IsValid = false, Code = code };
   }
    }

      /// <summary>
        /// Validates product code
        /// </summary>
        public async Task<CodingValidationResult> ValidateProductCodeAsync(string code)
   {
   try
            {
       _logger.LogInformation($"Validating product code: {code}");

      var result = new CodingValidationResult
     {
          Code = code,
    CodeSystem = "http://terminology.hl7.org/CodeSystem/ex-benefitcategory",
        CodeSystemName = "Benefit Category"
       };

 if (string.IsNullOrWhiteSpace(code))
      {
     result.IsValid = false;
            result.Errors.Add(new CodingError
  {
        ErrorCode = "NPHIES-PROD-001",
       ErrorMessage = "Product code is required",
 CodeType = "Product"
    });
         return result;
    }

     if (ValidProductCodes.Contains(code))
    {
             result.IsValid = true;
       result.Description = GetProductDescription(code);
                }
            else
     {
           result.IsValid = false;
           result.Errors.Add(new CodingError
   {
        ErrorCode = "NPHIES-PROD-002",
      ErrorMessage = $"Product code '{code}' is not recognized in NPHIES benefit categories",
                 InvalidCode = code,
               CodeType = "Product"
         });
                }

        return result;
            }
catch (Exception ex)
     {
    _logger.LogError(ex, "Error validating product code");
      return new CodingValidationResult { IsValid = false, Code = code };
            }
        }

     /// <summary>
        /// Validates claim type
        /// </summary>
        public async Task<CodingValidationResult> ValidateClaimTypeAsync(string claimType)
        {
  try
{
                _logger.LogInformation("Validating claim type");

     var result = new CodingValidationResult
                {
    CodeSystem = NphiesCodeSystems["claimType"],
  CodeSystemName = "Claim Type"
                };

                if (string.IsNullOrWhiteSpace(claimType))
                {
           result.IsValid = false;
                    result.Errors.Add(new CodingError
       {
     ErrorCode = "NPHIES-CLT-001",
    ErrorMessage = "Claim type is required",
      CodeType = "Claim Type"
              });
        return result;
      }

             if (ValidClaimTypes.Contains(claimType.ToLower()))
        {
    result.IsValid = true;
            result.Code = claimType;
    }
        else
             {
      result.IsValid = false;
      result.Errors.Add(new CodingError
      {
           ErrorCode = "NPHIES-CLT-002",
         ErrorMessage = $"Claim type '{claimType}' is not recognized. Valid types: {string.Join(", ", ValidClaimTypes)}",
         InvalidCode = claimType,
          CodeType = "Claim Type"
           });
      }

     return result;
    }
    catch (Exception ex)
       {
              _logger.LogError(ex, "Error validating claim type");
      return new CodingValidationResult { IsValid = false };
    }
        }

        /// <summary>
        /// Validates all codes in a claim
     /// </summary>
     public async Task<List<CodingError>> ValidateAllCodesInClaimAsync(ClaimCodeValidationDto claim)
        {
  var allErrors = new List<CodingError>();

try
          {
         _logger.LogInformation("Validating all codes in claim");

           if (claim == null) return allErrors;

                // Validate claim type
        var claimTypeResult = await ValidateClaimTypeAsync(claim.ClaimType);
        allErrors.AddRange(claimTypeResult.Errors);

   // Validate service codes
        if (claim.ServiceCodes != null)
    {
          foreach (var code in claim.ServiceCodes)
       {
          if (!string.IsNullOrWhiteSpace(code))
       {
   var serviceResult = await ValidateServiceCodeAsync(code);
          allErrors.AddRange(serviceResult.Errors);
         }
      }
       }

    // Validate diagnosis codes
           if (claim.DiagnosisCodes != null)
       {
        foreach (var code in claim.DiagnosisCodes)
           {
        if (!string.IsNullOrWhiteSpace(code))
        {
    var dxResult = await ValidateDiagnosisCodeAsync(code);
     allErrors.AddRange(dxResult.Errors);
       }
   }
                }

           // Validate procedure codes
   if (claim.ProcedureCodes != null)
       {
     foreach (var code in claim.ProcedureCodes)
   {
            if (!string.IsNullOrWhiteSpace(code))
          {
     var procResult = await ValidateProcedureCodeAsync(code);
       allErrors.AddRange(procResult.Errors);
      }
      }
    }

              return allErrors;
            }
 catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating codes in claim");
       return allErrors;
            }
        }

     /// <summary>
        /// Checks if code is in NPHIES system
        /// </summary>
        public async Task<bool> IsCodeInNphiesSystemAsync(string codeSystem, string code)
        {
            try
            {
             return codeSystem switch
     {
    "service" => ApprovedServiceCodes.Contains(code),
          "diagnosis" => IsValidIcd10Format(code),
            "procedure" => IsValidIcd10PcsFormat(code),
      "product" => ValidProductCodes.Contains(code),
        "claimType" => ValidClaimTypes.Contains(code),
         _ => false
};
    }
         catch (Exception ex)
            {
          _logger.LogError(ex, "Error checking code in NPHIES system");
     return false;
            }
      }

        // Helper methods

     private bool IsValidIcd10Format(string code)
        {
    if (string.IsNullOrWhiteSpace(code)) return false;
     // ICD-10 format: Letter + 2 digits + . + 0-2 digits/letters
  return Regex.IsMatch(code, @"^[A-Z]\d{2}(\.\d{1,2}|\.?[A-Z])?$");
  }

        private bool IsValidIcd10PcsFormat(string code)
      {
            if (string.IsNullOrWhiteSpace(code)) return false;
// ICD-10-PCS format: 7 alphanumeric characters
            return code.Length == 7 && Regex.IsMatch(code, @"^[0-9A-Z]{7}$");
     }

        private string GetServiceDescription(string code) => code switch
        {
            "99213" => "Office visit - established patient, low complexity",
        "99214" => "Office visit - established patient, moderate complexity",
            "99215" => "Office visit - established patient, high complexity",
       "90834" => "Psychotherapy - 45 minutes",
          "80053" => "Comprehensive metabolic panel",
   "70450" => "CT head - without contrast",
          _ => "Healthcare service"
        };

      private string GetDiagnosisDescription(string code) => code switch
 {
"E11.9" => "Type 2 diabetes mellitus without complications",
         "I10" => "Essential (primary) hypertension",
        "J44.9" => "Unspecified COPD",
      "M79.3" => "Panniculitis, unspecified",
       "F41.1" => "Generalized anxiety disorder",
            _ => "Diagnosis"
        };

   private string GetProductDescription(string code) => code switch
        {
    "1" => "Medical care services",
     "2" => "Surgical procedures",
 "3" => "Consultation services",
     "4" => "Diagnostic laboratory",
      "5" => "Diagnostic imaging",
    _ => "Benefit category"
        };
    }
}
