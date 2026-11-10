using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NPhies_FHIR_Integration.Application.Services.Validation
{
    /// <summary>
    /// NPHIES StructureDefinition Validator
    /// Validates claims and responses against NPHIES StructureDefinition profiles
    /// Implements NPHIES compliance validation rules
    /// </summary>
    public interface INphiesStructureDefinitionValidator
    {
   Task<ValidationResult> ValidateClaimStructureAsync(ClaimValidationDto claim);
        Task<ValidationResult> ValidateClaimResponseStructureAsync(ClaimResponseValidationDto claimResponse);
     Task<ValidationResult> ValidateCoverageStructureAsync(CoverageValidationDto coverage);
     Task<ValidationResult> ValidateBundleStructureAsync(BundleValidationDto bundle);
 Task<List<ValidationError>> GetStructuralErrorsAsync(string resourceType, object resource);
    }

    /// <summary>
 /// Validation result with errors and warnings
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
    public List<ValidationError> Errors { get; set; } = new();
        public List<ValidationWarning> Warnings { get; set; } = new();
public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
        public string ValidationProfile { get; set; } = "NPHIES R4";
        public int ErrorCount => Errors.Count;
        public int WarningCount => Warnings.Count;
    }

    /// <summary>
    /// Validation error details
    /// </summary>
    public class ValidationError
    {
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public string FieldPath { get; set; } = string.Empty;
        public string Severity { get; set; } = "Error"; // Error, Warning, Info
        public string NphiesReference { get; set; } = string.Empty;
    }

  /// <summary>
    /// Validation warning details
    /// </summary>
    public class ValidationWarning
    {
        public string WarningCode { get; set; } = string.Empty;
        public string WarningMessage { get; set; } = string.Empty;
    public string FieldPath { get; set; } = string.Empty;
   public string Recommendation { get; set; } = string.Empty;
 }

    /// <summary>
    /// Claim validation DTO
    /// </summary>
    public class ClaimValidationDto
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
      public string Type { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public string InsurerId { get; set; } = string.Empty;
        public string ProviderId { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public List<ClaimItemValidationDto> Items { get; set; } = new();
        public List<ClaimDiagnosisValidationDto> Diagnoses { get; set; } = new();
    }

  /// <summary>
  /// Claim item validation DTO
    /// </summary>
    public class ClaimItemValidationDto
    {
public int Sequence { get; set; }
        public string ServiceCode { get; set; } = string.Empty;
   public decimal Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string Category { get; set; } = string.Empty;
    }

    /// <summary>
 /// Claim diagnosis validation DTO
    /// </summary>
    public class ClaimDiagnosisValidationDto
    {
    public int Sequence { get; set; }
        public string DiagnosisCode { get; set; } = string.Empty;
        public string DiagnosisType { get; set; } = string.Empty;
    }

    /// <summary>
    /// ClaimResponse validation DTO
 /// </summary>
    public class ClaimResponseValidationDto
    {
    public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
      public string ClaimId { get; set; } = string.Empty;
        public string PatientId { get; set; } = string.Empty;
        public string InsurerId { get; set; } = string.Empty;
        public string Outcome { get; set; } = string.Empty;
        public DateTime Created { get; set; }
        public List<ClaimResponseItemValidationDto> Items { get; set; } = new();
    }

    /// <summary>
    /// ClaimResponse item validation DTO
    /// </summary>
    public class ClaimResponseItemValidationDto
    {
        public int ItemSequence { get; set; }
    public string Decision { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

  /// <summary>
    /// Coverage validation DTO
    /// </summary>
    public class CoverageValidationDto
    {
        public string Id { get; set; } = string.Empty;
 public string Status { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string SubscriberId { get; set; } = string.Empty;
        public string PayorId { get; set; } = string.Empty;
   public DateTime? EffectiveDate { get; set; }
        public DateTime? TerminationDate { get; set; }
    }

    /// <summary>
    /// Bundle validation DTO
    /// </summary>
    public class BundleValidationDto
    {
    public string Id { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public DateTime Created { get; set; }
  public List<BundleEntryValidationDto> Entries { get; set; } = new();
    }

    /// <summary>
    /// Bundle entry validation DTO
  /// </summary>
    public class BundleEntryValidationDto
    {
        public string ResourceType { get; set; } = string.Empty;
        public string ResourceId { get; set; } = string.Empty;
  public object Resource { get; set; }
    }

 /// <summary>
    /// NPHIES Structure Definition Validator Implementation
    /// </summary>
    public class NphiesStructureDefinitionValidator : INphiesStructureDefinitionValidator
    {
        private readonly ILogger<NphiesStructureDefinitionValidator> _logger;

  // NPHIES Claim Status values
        private static readonly HashSet<string> ValidClaimStatuses = new()
        {
            "active", "cancelled", "draft", "entered-in-error"
        };

        // NPHIES Claim Type values
     private static readonly HashSet<string> ValidClaimTypes = new()
      {
        "institutional", "oral", "pharmacy", "professional", "vision"
      };

      // NPHIES ClaimResponse Status values
        private static readonly HashSet<string> ValidClaimResponseStatuses = new()
        {
     "active", "cancelled", "draft", "entered-in-error"
     };

        // NPHIES Outcome values
        private static readonly HashSet<string> ValidOutcomes = new()
     {
      "queued", "complete", "error", "partial"
    };

        // NPHIES Coverage Status values
        private static readonly HashSet<string> ValidCoverageStatuses = new()
        {
   "active", "cancelled", "draft", "entered-in-error"
        };

        // NPHIES Bundle Type values
        private static readonly HashSet<string> ValidBundleTypes = new()
        {
            "document", "message", "transaction", "transaction-response", "batch", "batch-response", "history", "searchset", "collection"
    };

        public NphiesStructureDefinitionValidator(ILogger<NphiesStructureDefinitionValidator> logger)
        {
     _logger = logger;
        }

        /// <summary>
        /// Validates Claim against NPHIES StructureDefinition
/// </summary>
        public async Task<ValidationResult> ValidateClaimStructureAsync(ClaimValidationDto claim)
        {
            try
            {
       _logger.LogInformation("Validating Claim structure against NPHIES StructureDefinition");

         var result = new ValidationResult
         {
        ValidationProfile = "NPHIES Claim R4"
       };

         if (claim == null)
    {
         result.Errors.Add(new ValidationError
      {
            ErrorCode = "NPHIES-CLM-001",
 ErrorMessage = "Claim resource is required",
    FieldPath = "Claim",
                  Severity = "Error",
            NphiesReference = "NPHIES Claim Structure"
   });
         result.IsValid = false;
      return result;
    }

      // Validate Claim ID (mandatory)
       if (string.IsNullOrWhiteSpace(claim.Id))
    {
   result.Errors.Add(new ValidationError
         {
    ErrorCode = "NPHIES-CLM-002",
     ErrorMessage = "Claim ID is required and must not be empty",
    FieldPath = "Claim.id",
           Severity = "Error",
        NphiesReference = "NPHIES Claim.id"
    });
          }

      // Validate Claim Status (mandatory)
 if (string.IsNullOrWhiteSpace(claim.Status))
 {
          result.Errors.Add(new ValidationError
     {
    ErrorCode = "NPHIES-CLM-003",
 ErrorMessage = "Claim Status is required",
             FieldPath = "Claim.status",
   Severity = "Error",
    NphiesReference = "NPHIES Claim.status"
  });
      }
       else if (!ValidClaimStatuses.Contains(claim.Status.ToLower()))
     {
    result.Errors.Add(new ValidationError
        {
       ErrorCode = "NPHIES-CLM-004",
     ErrorMessage = $"Invalid Claim Status '{claim.Status}'. Valid values: {string.Join(", ", ValidClaimStatuses)}",
     FieldPath = "Claim.status",
      Severity = "Error",
          NphiesReference = "NPHIES Claim.status"
});
          }

           // Validate Claim Type (mandatory)
 if (string.IsNullOrWhiteSpace(claim.Type))
          {
        result.Errors.Add(new ValidationError
 {
      ErrorCode = "NPHIES-CLM-005",
  ErrorMessage = "Claim Type is required",
             FieldPath = "Claim.type",
               Severity = "Error",
           NphiesReference = "NPHIES Claim.type"
      });
            }
         else if (!ValidClaimTypes.Contains(claim.Type.ToLower()))
         {
        result.Errors.Add(new ValidationError
          {
         ErrorCode = "NPHIES-CLM-006",
    ErrorMessage = $"Invalid Claim Type '{claim.Type}'. Valid values: {string.Join(", ", ValidClaimTypes)}",
             FieldPath = "Claim.type",
        Severity = "Error",
     NphiesReference = "NPHIES Claim.type"
         });
      }

              // Validate Patient (mandatory)
      if (string.IsNullOrWhiteSpace(claim.PatientId))
       {
      result.Errors.Add(new ValidationError
   {
              ErrorCode = "NPHIES-CLM-007",
  ErrorMessage = "Patient reference is required",
     FieldPath = "Claim.patient",
Severity = "Error",
     NphiesReference = "NPHIES Claim.patient"
       });
   }

     // Validate Insurer (mandatory)
      if (string.IsNullOrWhiteSpace(claim.InsurerId))
    {
  result.Errors.Add(new ValidationError
    {
               ErrorCode = "NPHIES-CLM-008",
        ErrorMessage = "Insurer reference is required",
              FieldPath = "Claim.insurer",
     Severity = "Error",
       NphiesReference = "NPHIES Claim.insurer"
      });
                }

       // Validate Provider (mandatory)
     if (string.IsNullOrWhiteSpace(claim.ProviderId))
{
               result.Errors.Add(new ValidationError
              {
 ErrorCode = "NPHIES-CLM-009",
   ErrorMessage = "Provider reference is required",
         FieldPath = "Claim.provider",
              Severity = "Error",
            NphiesReference = "NPHIES Claim.provider"
       });
      }

  // Validate Created Date (mandatory)
    if (claim.Created == default(DateTime))
                {
       result.Errors.Add(new ValidationError
    {
                ErrorCode = "NPHIES-CLM-010",
     ErrorMessage = "Created date is required",
         FieldPath = "Claim.created",
          Severity = "Error",
   NphiesReference = "NPHIES Claim.created"
         });
        }

   // Validate Items (at least one required)
      if (claim.Items == null || claim.Items.Count == 0)
             {
                result.Errors.Add(new ValidationError
      {
                 ErrorCode = "NPHIES-CLM-011",
         ErrorMessage = "At least one claim item is required",
      FieldPath = "Claim.item",
        Severity = "Error",
        NphiesReference = "NPHIES Claim.item"
          });
         }
           else
                {
  ValidateClaimItems(claim.Items, result);
    }

  // Validate Diagnoses (if present)
  if (claim.Diagnoses != null && claim.Diagnoses.Count > 0)
                {
                ValidateClaimDiagnoses(claim.Diagnoses, result);
                }

       result.IsValid = result.Errors.Count(e => e.Severity == "Error") == 0;
 _logger.LogInformation($"Claim validation completed. Valid: {result.IsValid}, Errors: {result.ErrorCount}, Warnings: {result.WarningCount}");

       return result;
    }
       catch (Exception ex)
       {
    _logger.LogError(ex, "Error validating Claim structure");
     return new ValidationResult
         {
         IsValid = false,
        Errors = new List<ValidationError>
              {
  new ValidationError
                {
    ErrorCode = "NPHIES-CLM-999",
      ErrorMessage = $"Validation error: {ex.Message}",
   Severity = "Error"
     }
           }
           };
     }
      }

      /// <summary>
  /// Validates ClaimResponse against NPHIES StructureDefinition
        /// </summary>
        public async Task<ValidationResult> ValidateClaimResponseStructureAsync(ClaimResponseValidationDto claimResponse)
        {
   try
            {
     _logger.LogInformation("Validating ClaimResponse structure against NPHIES StructureDefinition");

     var result = new ValidationResult
      {
     ValidationProfile = "NPHIES ClaimResponse R4"
           };

 if (claimResponse == null)
       {
      result.Errors.Add(new ValidationError
      {
       ErrorCode = "NPHIES-CLMRESP-001",
        ErrorMessage = "ClaimResponse resource is required",
            Severity = "Error"
   });
  result.IsValid = false;
      return result;
    }

                // Validate ID (mandatory)
         if (string.IsNullOrWhiteSpace(claimResponse.Id))
 {
          result.Errors.Add(new ValidationError
         {
       ErrorCode = "NPHIES-CLMRESP-002",
            ErrorMessage = "ClaimResponse ID is required",
                 FieldPath = "ClaimResponse.id",
          Severity = "Error"
      });
                }

   // Validate Status (mandatory)
          if (string.IsNullOrWhiteSpace(claimResponse.Status))
        {
   result.Errors.Add(new ValidationError
           {
    ErrorCode = "NPHIES-CLMRESP-003",
       ErrorMessage = "ClaimResponse Status is required",
         FieldPath = "ClaimResponse.status",
         Severity = "Error"
       });
              }
    else if (!ValidClaimResponseStatuses.Contains(claimResponse.Status.ToLower()))
     {
     result.Errors.Add(new ValidationError
    {
            ErrorCode = "NPHIES-CLMRESP-004",
  ErrorMessage = $"Invalid ClaimResponse Status '{claimResponse.Status}'",
         FieldPath = "ClaimResponse.status",
        Severity = "Error"
     });
             }

         // Validate Claim ID (mandatory)
       if (string.IsNullOrWhiteSpace(claimResponse.ClaimId))
        {
        result.Errors.Add(new ValidationError
        {
       ErrorCode = "NPHIES-CLMRESP-005",
              ErrorMessage = "Claim ID reference is required",
            FieldPath = "ClaimResponse.claim",
             Severity = "Error"
 });
              }

     // Validate Patient (mandatory)
         if (string.IsNullOrWhiteSpace(claimResponse.PatientId))
          {
          result.Errors.Add(new ValidationError
           {
      ErrorCode = "NPHIES-CLMRESP-006",
             ErrorMessage = "Patient reference is required",
            FieldPath = "ClaimResponse.patient",
 Severity = "Error"
});
     }

       // Validate Insurer (mandatory)
      if (string.IsNullOrWhiteSpace(claimResponse.InsurerId))
            {
  result.Errors.Add(new ValidationError
  {
      ErrorCode = "NPHIES-CLMRESP-007",
       ErrorMessage = "Insurer reference is required",
          FieldPath = "ClaimResponse.insurer",
 Severity = "Error"
       });
              }

    // Validate Outcome (mandatory)
        if (string.IsNullOrWhiteSpace(claimResponse.Outcome))
                {
                    result.Errors.Add(new ValidationError
        {
      ErrorCode = "NPHIES-CLMRESP-008",
           ErrorMessage = "Outcome is required",
      FieldPath = "ClaimResponse.outcome",
       Severity = "Error"
        });
       }
  else if (!ValidOutcomes.Contains(claimResponse.Outcome.ToLower()))
     {
        result.Errors.Add(new ValidationError
          {
            ErrorCode = "NPHIES-CLMRESP-009",
         ErrorMessage = $"Invalid Outcome '{claimResponse.Outcome}'",
           FieldPath = "ClaimResponse.outcome",
     Severity = "Error"
    });
      }

 // Validate Created Date
  if (claimResponse.Created == default(DateTime))
     {
           result.Errors.Add(new ValidationError
     {
 ErrorCode = "NPHIES-CLMRESP-010",
                ErrorMessage = "Created date is required",
          FieldPath = "ClaimResponse.created",
           Severity = "Error"
           });
  }

      result.IsValid = result.Errors.Count(e => e.Severity == "Error") == 0;
 return result;
            }
            catch (Exception ex)
            {
      _logger.LogError(ex, "Error validating ClaimResponse structure");
       return new ValidationResult
         {
     IsValid = false,
        Errors = new List<ValidationError>
   {
               new ValidationError
       {
       ErrorCode = "NPHIES-CLMRESP-999",
  ErrorMessage = $"Validation error: {ex.Message}",
         Severity = "Error"
        }
 }
    };
         }
        }

   /// <summary>
        /// Validates Coverage against NPHIES StructureDefinition
  /// </summary>
        public async Task<ValidationResult> ValidateCoverageStructureAsync(CoverageValidationDto coverage)
    {
            try
    {
    _logger.LogInformation("Validating Coverage structure against NPHIES StructureDefinition");

     var result = new ValidationResult
{
           ValidationProfile = "NPHIES Coverage R4"
 };

                if (coverage == null)
        {
       result.IsValid = false;
      return result;
      }

                // Validate ID
        if (string.IsNullOrWhiteSpace(coverage.Id))
     {
         result.Errors.Add(new ValidationError
          {
       ErrorCode = "NPHIES-COV-001",
     ErrorMessage = "Coverage ID is required",
      FieldPath = "Coverage.id",
            Severity = "Error"
   });
          }

      // Validate Status
     if (string.IsNullOrWhiteSpace(coverage.Status))
 {
       result.Errors.Add(new ValidationError
         {
              ErrorCode = "NPHIES-COV-002",
      ErrorMessage = "Coverage Status is required",
    FieldPath = "Coverage.status",
        Severity = "Error"
 });
          }
                else if (!ValidCoverageStatuses.Contains(coverage.Status.ToLower()))
      {
       result.Errors.Add(new ValidationError
           {
              ErrorCode = "NPHIES-COV-003",
              ErrorMessage = $"Invalid Coverage Status '{coverage.Status}'",
           FieldPath = "Coverage.status",
       Severity = "Error"
         });
    }

     // Validate Type
     if (string.IsNullOrWhiteSpace(coverage.Type))
                {
 result.Errors.Add(new ValidationError
         {
    ErrorCode = "NPHIES-COV-004",
     ErrorMessage = "Coverage Type is required",
          FieldPath = "Coverage.type",
       Severity = "Error"
          });
    }

     // Validate Subscriber
              if (string.IsNullOrWhiteSpace(coverage.SubscriberId))
 {
     result.Errors.Add(new ValidationError
         {
         ErrorCode = "NPHIES-COV-005",
     ErrorMessage = "Subscriber reference is required",
FieldPath = "Coverage.subscriber",
         Severity = "Error"
   });
         }

      // Validate Payor
     if (string.IsNullOrWhiteSpace(coverage.PayorId))
      {
      result.Errors.Add(new ValidationError
   {
       ErrorCode = "NPHIES-COV-006",
    ErrorMessage = "At least one Payor is required",
   FieldPath = "Coverage.payor",
     Severity = "Error"
        });
      }

         result.IsValid = result.Errors.Count(e => e.Severity == "Error") == 0;
      return result;
            }
  catch (Exception ex)
       {
         _logger.LogError(ex, "Error validating Coverage structure");
      return new ValidationResult { IsValid = false };
   }
        }

        /// <summary>
        /// Validates Bundle against NPHIES StructureDefinition
      /// </summary>
        public async Task<ValidationResult> ValidateBundleStructureAsync(BundleValidationDto bundle)
        {
 try
         {
        _logger.LogInformation("Validating Bundle structure against NPHIES StructureDefinition");

      var result = new ValidationResult
     {
    ValidationProfile = "NPHIES Bundle R4"
       };

             if (bundle == null)
          {
        result.IsValid = false;
    return result;
           }

                // Validate Bundle Type
        if (string.IsNullOrWhiteSpace(bundle.Type))
        {
           result.Errors.Add(new ValidationError
  {
       ErrorCode = "NPHIES-BNDL-001",
    ErrorMessage = "Bundle Type is required",
   FieldPath = "Bundle.type",
        Severity = "Error"
      });
    }
                else if (!ValidBundleTypes.Contains(bundle.Type.ToLower()))
       {
  result.Errors.Add(new ValidationError
   {
             ErrorCode = "NPHIES-BNDL-002",
          ErrorMessage = $"Invalid Bundle Type '{bundle.Type}'",
          FieldPath = "Bundle.type",
         Severity = "Error"
        });
    }

                // Validate Entries
         if (bundle.Entries == null || bundle.Entries.Count == 0)
       {
       result.Errors.Add(new ValidationError
        {
  ErrorCode = "NPHIES-BNDL-003",
      ErrorMessage = "Bundle must contain at least one entry",
        FieldPath = "Bundle.entry",
             Severity = "Error"
        });
           }

             result.IsValid = result.Errors.Count(e => e.Severity == "Error") == 0;
      return result;
}
            catch (Exception ex)
     {
 _logger.LogError(ex, "Error validating Bundle structure");
         return new ValidationResult { IsValid = false };
        }
 }

        /// <summary>
        /// Gets structural errors for a resource
        /// </summary>
        public async Task<List<ValidationError>> GetStructuralErrorsAsync(string resourceType, object resource)
        {
            var errors = new List<ValidationError>();

    try
    {
  _logger.LogInformation($"Checking structural errors for {resourceType}");

     // Implementation would be resource-specific
         // This is a placeholder for extensibility

          return errors;
            }
      catch (Exception ex)
    {
    _logger.LogError(ex, "Error getting structural errors");
        return new List<ValidationError>();
            }
     }

// Helper methods

  private void ValidateClaimItems(List<ClaimItemValidationDto> items, ValidationResult result)
        {
            for (int i = 0; i < items.Count; i++)
       {
 var item = items[i];

             if (item.Sequence == 0)
    {
             result.Errors.Add(new ValidationError
       {
 ErrorCode = "NPHIES-CLMITEM-001",
   ErrorMessage = "Claim item sequence is required and must be greater than 0",
        FieldPath = $"Claim.item[{i}].sequence",
                Severity = "Error"
          });
         }

  if (string.IsNullOrWhiteSpace(item.ServiceCode))
      {
     result.Errors.Add(new ValidationError
            {
 ErrorCode = "NPHIES-CLMITEM-002",
     ErrorMessage = "Service code is required",
             FieldPath = $"Claim.item[{i}].productOrService",
           Severity = "Error"
    });
                }

           if (item.Quantity <= 0)
      {
        result.Warnings.Add(new ValidationWarning
          {
    WarningCode = "NPHIES-CLMITEM-003",
   WarningMessage = "Quantity should be greater than 0",
     FieldPath = $"Claim.item[{i}].quantity"
         });
     }

if (item.UnitPrice <= 0)
     {
     result.Errors.Add(new ValidationError
        {
    ErrorCode = "NPHIES-CLMITEM-004",
    ErrorMessage = "Unit price must be greater than 0",
            FieldPath = $"Claim.item[{i}].unitPrice",
   Severity = "Error"
        });
       }
            }
        }

     private void ValidateClaimDiagnoses(List<ClaimDiagnosisValidationDto> diagnoses, ValidationResult result)
 {
   for (int i = 0; i < diagnoses.Count; i++)
          {
       var diagnosis = diagnoses[i];

     if (string.IsNullOrWhiteSpace(diagnosis.DiagnosisCode))
           {
            result.Errors.Add(new ValidationError
            {
        ErrorCode = "NPHIES-CLMDX-001",
         ErrorMessage = "Diagnosis code is required",
 FieldPath = $"Claim.diagnosis[{i}].diagnosisCodeableConcept",
                    Severity = "Error"
    });
     }

            if (diagnosis.Sequence < 1)
        {
        result.Errors.Add(new ValidationError
      {
      ErrorCode = "NPHIES-CLMDX-002",
         ErrorMessage = "Diagnosis sequence must be greater than 0",
           FieldPath = $"Claim.diagnosis[{i}].sequence",
            Severity = "Error"
        });
       }
    }
        }
    }
}
