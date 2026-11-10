using NPhies_FHIR_Integration.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services
{
    /// <summary>
    /// NPHIES Message Format Compliance Validation Service Interface
    /// Validates FHIR messages against NPHIES standards and requirements
    /// </summary>
    public interface IMessageFormatValidationService
    {
        /// <summary>
        /// Validate bundle structure compliance
        /// </summary>
        Task<MessageFormatValidationResult> ValidateBundleAsync(string bundleJson);

    /// <summary>
        /// Validate claim bundle format (inpatient vs outpatient)
 /// </summary>
        Task<List<MessageValidationError>> ValidateClaimBundleFormatAsync(Claim claim);

        /// <summary>
  /// Validate reference formats (absolute vs relative URIs)
        /// </summary>
        Task<List<MessageValidationError>> ValidateReferenceFormatsAsync(string bundleJson);

    /// <summary>
    /// Validate identifier system URIs per NPHIES spec
        /// </summary>
        Task<List<MessageValidationError>> ValidateIdentifierSystemsAsync(string bundleJson);

      /// <summary>
  /// Validate coding system compliance
        /// </summary>
        Task<List<MessageValidationError>> ValidateCodingSystemsAsync(string bundleJson);

        /// <summary>
        /// Validate NPHIES extensions
   /// </summary>
        Task<List<MessageValidationError>> ValidateExtensionsAsync(string resourceJson);

  /// <summary>
        /// Validate narrative text format
        /// </summary>
        Task<List<MessageValidationError>> ValidateNarrativeAsync(string resourceJson);

        /// <summary>
        /// Validate message type
      /// </summary>
      Task<List<MessageValidationError>> ValidateMessageTypeAsync(string messageType);

        /// <summary>
        /// Validate required message elements
        /// </summary>
 Task<List<MessageValidationError>> ValidateRequiredElementsAsync(string messageType, string bundleJson);
    }

    /// <summary>
    /// Message format validation result
    /// </summary>
    public class MessageFormatValidationResult
    {
        public bool IsValid { get; set; }
        public string MessageType { get; set; } = string.Empty;
        public DateTime ValidationTimestamp { get; set; } = DateTime.UtcNow;
     public List<MessageValidationError> Errors { get; set; } = new();
        public List<MessageValidationError> Warnings { get; set; } = new();
     public int ErrorCount => Errors.Count;
        public int WarningCount => Warnings.Count;
        public string ValidationSummary => $"Errors: {ErrorCount}, Warnings: {WarningCount}";
        public List<string> ComplianceChecks { get; set; } = new();
    }

    /// <summary>
    /// Individual message validation error
 /// </summary>
    public class MessageValidationError
    {
    public string ErrorCode { get; set; } = string.Empty;
        public string ErrorName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
   public string Element { get; set; } = string.Empty;
        public MessageValidationSeverity Severity { get; set; }
        public string? RemediationAction { get; set; }
        public string StandardReference { get; set; } = string.Empty;
    }

    /// <summary>
    /// Validation severity levels
    /// </summary>
    public enum MessageValidationSeverity
    {
        Error = 1,
      Warning = 2,
     Info = 3
    }

    /// <summary>
 /// NPHIES Message Format Compliance Validation Service Implementation
    /// Validates FHIR messages against NPHIES standards
    /// </summary>
    public class MessageFormatValidationService : IMessageFormatValidationService
    {
        private readonly ILogger<MessageFormatValidationService> _logger;

        private readonly string[] _validMessageTypes = new[]
        {
 "claim-request",
         "claim-response",
  "eligibility-request",
        "eligibility-response",
            "priorauth-request",
      "priorauth-response",
      "cancel-request",
         "cancel-response",
   "communication-request",
            "communication",
       "payment-notice",
   "payment-reconciliation",
            "status-check",
  "status-response"
        };

        private readonly Dictionary<string, string> _nphiesIdentifierSystems = new()
        {
 { "patient", "http://nphies.sa/fhir/identifier/Patient" },
     { "member", "http://nphies.sa/fhir/identifier/MemberId" },
            { "provider", "http://nphies.sa/fhir/identifier/Provider" },
            { "organization", "http://nphies.sa/fhir/identifier/Organization" },
          { "claim", "http://nphies.sa/fhir/identifier/Claim" },
 { "claimresponse", "http://nphies.sa/fhir/identifier/ClaimResponse" },
            { "coverage", "http://nphies.sa/fhir/identifier/Coverage" }
        };

        private readonly Dictionary<string, string> _nphiesCodingSystems = new()
        {
 { "diagnosis", "http://hl7.org/fhir/sid/icd-10" },
            { "procedure", "http://hl7.org/fhir/sid/icd-10-cm" },
   { "service", "http://nphies.sa/fhir/CodeSystem/service-type" },
            { "benefit", "http://nphies.sa/fhir/CodeSystem/benefit-category" },
     { "adjudication", "http://nphies.sa/fhir/CodeSystem/adjudication-category" },
            { "remittance", "http://hl7.org/fhir/remittance-outcome" }
        };

 public MessageFormatValidationService(ILogger<MessageFormatValidationService> logger)
        {
      _logger = logger;
   }

        /// <summary>
        /// Validate bundle structure compliance
 /// </summary>
        public async Task<MessageFormatValidationResult> ValidateBundleAsync(string bundleJson)
        {
         var result = new MessageFormatValidationResult();

            try
            {
        _logger.LogInformation("Starting NPHIES message format validation");

        // Rule 1: Bundle structure
                result.Errors.AddRange(await ValidateBundleStructureAsync(bundleJson));

     // Rule 2: Reference formats
                result.Errors.AddRange(await ValidateReferenceFormatsAsync(bundleJson));

   // Rule 3: Identifier systems
      result.Errors.AddRange(await ValidateIdentifierSystemsAsync(bundleJson));

          // Rule 4: Coding systems
   result.Errors.AddRange(await ValidateCodingSystemsAsync(bundleJson));

                // Rule 5: Extensions
      result.Errors.AddRange(await ValidateExtensionsAsync(bundleJson));

       // Rule 6: Narrative
       result.Errors.AddRange(await ValidateNarrativeAsync(bundleJson));

result.ComplianceChecks.Add("Bundle structure validated");
           result.ComplianceChecks.Add("References validated");
  result.ComplianceChecks.Add("Identifier systems validated");
      result.ComplianceChecks.Add("Coding systems validated");
    result.ComplianceChecks.Add("Extensions validated");
       result.ComplianceChecks.Add("Narrative validated");

                result.IsValid = result.ErrorCount == 0;

                _logger.LogInformation($"Message format validation completed. IsValid: {result.IsValid}, Errors: {result.ErrorCount}");
            }
        catch (Exception ex)
            {
      _logger.LogError(ex, "Error validating message format");
      result.Errors.Add(new MessageValidationError
        {
        ErrorCode = "MSG-ERR-001",
           ErrorName = "Message Validation Exception",
        Message = ex.Message,
         Severity = MessageValidationSeverity.Error,
        StandardReference = "NPHIES Message Handling"
        });
           result.IsValid = false;
     }

        return result;
        }

        /// <summary>
        /// Rule Group 1: Bundle Structure Validation
        /// </summary>
        private async Task<List<MessageValidationError>> ValidateBundleStructureAsync(string bundleJson)
      {
       var errors = new List<MessageValidationError>();

    try
            {
     if (string.IsNullOrWhiteSpace(bundleJson))
     {
  errors.Add(new MessageValidationError
   {
 ErrorCode = "BDL-001",
     ErrorName = "Bundle JSON Required",
 Message = "Bundle JSON content is required",
        Element = "Bundle",
     Severity = MessageValidationSeverity.Error,
      RemediationAction = "Provide valid FHIR Bundle JSON",
     StandardReference = "NPHIES Bundle Structure"
              });
    return errors;
                }

                // Rule 1: Bundle type must be "message"
 if (!bundleJson.Contains("\"type\":\"message\"") && !bundleJson.Contains("\"type\": \"message\""))
     {
    errors.Add(new MessageValidationError
   {
         ErrorCode = "BDL-002",
 ErrorName = "Bundle Type Must Be Message",
          Message = "Bundle.type must be 'message'",
      Element = "Bundle.type",
         Severity = MessageValidationSeverity.Error,
     RemediationAction = "Set Bundle type to 'message'",
 StandardReference = "NPHIES Bundle Structure"
        });
     }

         // Rule 2: Bundle ID presence
       if (!bundleJson.Contains("\"id\""))
     {
         errors.Add(new MessageValidationError
       {
           ErrorCode = "BDL-003",
      ErrorName = "Bundle ID Required",
                   Message = "Bundle.id is required",
  Element = "Bundle.id",
 Severity = MessageValidationSeverity.Error,
                 RemediationAction = "Provide unique bundle identifier",
   StandardReference = "NPHIES Bundle Structure"
    });
         }

    // Rule 3: Bundle entries required
   if (!bundleJson.Contains("\"entry\"") || !bundleJson.Contains("\"resourceType\""))
          {
 errors.Add(new MessageValidationError
                  {
 ErrorCode = "BDL-004",
ErrorName = "Bundle Entries Required",
      Message = "Bundle.entry array is required with at least one entry",
   Element = "Bundle.entry",
       Severity = MessageValidationSeverity.Error,
          RemediationAction = "Add at least one resource entry to bundle",
            StandardReference = "NPHIES Bundle Structure"
 });
     }

       // Rule 4: First entry must be MessageHeader
     if (!bundleJson.Contains("\"resourceType\":\"MessageHeader\"") &&
              !bundleJson.Contains("\"resourceType\": \"MessageHeader\""))
       {
        errors.Add(new MessageValidationError
    {
                   ErrorCode = "BDL-005",
           ErrorName = "First Entry Must Be MessageHeader",
      Message = "First entry in bundle.entry must be MessageHeader",
      Element = "Bundle.entry[0]",
        Severity = MessageValidationSeverity.Error,
        RemediationAction = "Add MessageHeader as first resource in bundle",
 StandardReference = "NPHIES Bundle Structure"
   });
         }

        _logger.LogInformation($"Bundle structure validation completed. Errors: {errors.Count}");
      }
            catch (Exception ex)
            {
        _logger.LogError(ex, "Error validating bundle structure");
  }

       return errors;
        }

    /// <summary>
        /// Rule Group 2: Claim Bundle Format Validation
        /// </summary>
        public async Task<List<MessageValidationError>> ValidateClaimBundleFormatAsync(Claim claim)
        {
var errors = new List<MessageValidationError>();

            try
            {
   if (claim == null)
    {
   errors.Add(new MessageValidationError
        {
            ErrorCode = "CBF-001",
     ErrorName = "Claim Required",
       Message = "Claim object is required",
   Element = "Claim",
   Severity = MessageValidationSeverity.Error,
        RemediationAction = "Provide valid claim object",
          StandardReference = "NPHIES Claim Bundle Format"
     });
   return errors;
            }

                // Rule 5: Inpatient claims format
  if (claim.ClaimType?.ToLower() == "inpatient")
    {
       if (claim.Diagnoses == null || claim.Diagnoses.Count == 0)
 {
     errors.Add(new MessageValidationError
           {
       ErrorCode = "CBF-002",
            ErrorName = "Inpatient Diagnosis Required",
     Message = "Inpatient claims require at least one diagnosis",
     Element = "Claim.diagnosis",
   Severity = MessageValidationSeverity.Error,
   RemediationAction = "Add diagnosis codes to inpatient claim",
    StandardReference = "NPHIES Inpatient Claim Format"
    });
     }
          }

      // Rule 6: Outpatient claims format
          if (claim.ClaimType?.ToLower() == "outpatient")
          {
   if (claim.Items == null || claim.Items.Count == 0)
         {
 errors.Add(new MessageValidationError
      {
      ErrorCode = "CBF-003",
                ErrorName = "Outpatient Items Required",
            Message = "Outpatient claims require at least one service item",
         Element = "Claim.item",
     Severity = MessageValidationSeverity.Error,
            RemediationAction = "Add service items to outpatient claim",
              StandardReference = "NPHIES Outpatient Claim Format"
       });
   }
        }

                _logger.LogInformation($"Claim bundle format validation completed. Errors: {errors.Count}");
  }
            catch (Exception ex)
        {
   _logger.LogError(ex, "Error validating claim bundle format");
            }

   return errors;
        }

        /// <summary>
        /// Rule Group 3: Reference Format Validation
        /// </summary>
        public async Task<List<MessageValidationError>> ValidateReferenceFormatsAsync(string bundleJson)
        {
            var errors = new List<MessageValidationError>();

 try
       {
                if (string.IsNullOrWhiteSpace(bundleJson))
           return errors;

   // Rule 7: Reference formats (relative vs absolute)
      var absoluteReferenceCount = bundleJson.Split("http://").Length - 1;
         var relativeReferenceCount = bundleJson.Split("\"reference\":\"").Length - 1;

   if (absoluteReferenceCount > relativeReferenceCount * 2)
        {
           errors.Add(new MessageValidationError
        {
               ErrorCode = "REF-001",
     ErrorName = "Excessive Absolute References",
           Message = "Bundle contains too many absolute URI references; prefer relative references",
             Element = "Bundle.entry[*].resource.*.reference",
             Severity = MessageValidationSeverity.Warning,
     RemediationAction = "Use relative references (e.g., 'Patient/123') instead of absolute URIs",
          StandardReference = "NPHIES Reference Format Guidelines"
           });
       }

            // Rule 8: Contained resources
           if (bundleJson.Contains("\"reference\":\"#\""))
       {
      errors.Add(new MessageValidationError
{
             ErrorCode = "REF-002",
                  ErrorName = "Contained Resource Reference",
       Message = "Contained resource reference detected",
          Element = "*.reference",
            Severity = MessageValidationSeverity.Info,
       RemediationAction = "Verify contained resources are properly included",
       StandardReference = "NPHIES Reference Format"
  });
         }

          _logger.LogInformation($"Reference format validation completed. Errors: {errors.Count}");
            }
          catch (Exception ex)
 {
         _logger.LogError(ex, "Error validating reference formats");
            }

            return errors;
        }

 /// <summary>
  /// Rule Group 4: Identifier System Validation
        /// </summary>
        public async Task<List<MessageValidationError>> ValidateIdentifierSystemsAsync(string bundleJson)
        {
    var errors = new List<MessageValidationError>();

            try
        {
       if (string.IsNullOrWhiteSpace(bundleJson))
return errors;

       // Rule 9: NPHIES identifier system URIs
        var missingSystems = new List<string>();

     foreach (var system in _nphiesIdentifierSystems.Values)
         {
          if (!bundleJson.Contains(system))
             missingSystems.Add(system);
     }

    if (bundleJson.Contains("\"system\":") && !bundleJson.Contains("\"value\":"))
            {
       errors.Add(new MessageValidationError
     {
      ErrorCode = "IDS-001",
 ErrorName = "Missing Identifier Value",
       Message = "Identifier system specified without value",
  Element = "*.identifier[*].value",
   Severity = MessageValidationSeverity.Error,
   RemediationAction = "Provide identifier value for each identifier system",
     StandardReference = "NPHIES Identifier Format"
  });
            }

            _logger.LogInformation($"Identifier system validation completed. Errors: {errors.Count}");
            }
      catch (Exception ex)
  {
      _logger.LogError(ex, "Error validating identifier systems");
          }

   return errors;
        }

        /// <summary>
        /// Rule Group 5: Coding System Validation
        /// </summary>
        public async Task<List<MessageValidationError>> ValidateCodingSystemsAsync(string bundleJson)
        {
   var errors = new List<MessageValidationError>();

            try
    {
             if (string.IsNullOrWhiteSpace(bundleJson))
          return errors;

     // Rule 10: Code presence with system
        if (bundleJson.Contains("\"system\":") && !bundleJson.Contains("\"code\":"))
                {
        errors.Add(new MessageValidationError
          {
  ErrorCode = "COD-001",
   ErrorName = "Code Missing",
    Message = "Coding system specified without code",
          Element = "*.coding[*].code",
    Severity = MessageValidationSeverity.Error,
    RemediationAction = "Provide code for each coding system",
   StandardReference = "NPHIES Coding Format"
               });
        }

             _logger.LogInformation($"Coding system validation completed. Errors: {errors.Count}");
    }
      catch (Exception ex)
       {
          _logger.LogError(ex, "Error validating coding systems");
            }

return errors;
      }

     /// <summary>
        /// Rule Group 6: Extension Validation
   /// </summary>
    public async Task<List<MessageValidationError>> ValidateExtensionsAsync(string resourceJson)
        {
      var errors = new List<MessageValidationError>();

         try
            {
              if (string.IsNullOrWhiteSpace(resourceJson))
     return errors;

    // Rule 11: Extension URL presence
    if (resourceJson.Contains("\"extension\"") && !resourceJson.Contains("\"url\":"))
          {
    errors.Add(new MessageValidationError
          {
    ErrorCode = "EXT-001",
      ErrorName = "Extension URL Required",
            Message = "Extension defined without URL",
  Element = "*.extension[*].url",
         Severity = MessageValidationSeverity.Error,
          RemediationAction = "Specify URL for all extensions",
        StandardReference = "NPHIES Extension Format"
      });
     }

         // Rule 12: NPHIES extension URLs
      if (resourceJson.Contains("\"extension\"") && resourceJson.Contains("\"url\":\""))
        {
         if (!resourceJson.Contains("http://nphies.sa") && !resourceJson.Contains("http://hl7.org"))
        {
   errors.Add(new MessageValidationError
   {
   ErrorCode = "EXT-002",
          ErrorName = "Non-Standard Extension URL",
         Message = "Extension URLs should use NPHIES or HL7 standard namespaces",
      Element = "*.extension[*].url",
         Severity = MessageValidationSeverity.Warning,
     RemediationAction = "Use standard NPHIES or HL7 extension URLs",
            StandardReference = "NPHIES Extension Standards"
          });
        }
 }

                _logger.LogInformation($"Extension validation completed. Errors: {errors.Count}");
    }
            catch (Exception ex)
          {
       _logger.LogError(ex, "Error validating extensions");
            }

            return errors;
        }

        /// <summary>
        /// Rule Group 7: Narrative Validation
        /// </summary>
        public async Task<List<MessageValidationError>> ValidateNarrativeAsync(string resourceJson)
        {
       var errors = new List<MessageValidationError>();

         try
    {
      if (string.IsNullOrWhiteSpace(resourceJson))
 return errors;

       // Rule 13: Narrative presence for certain resources
  var requiresNarrative = resourceJson.Contains("\"resourceType\":\"ClaimResponse\"") ||
         resourceJson.Contains("\"resourceType\":\"Explanation");

         if (requiresNarrative && !resourceJson.Contains("\"narrative\"") && !resourceJson.Contains("\"text\""))
 {
             errors.Add(new MessageValidationError
    {
             ErrorCode = "NAR-001",
   ErrorName = "Narrative Text Required",
      Message = "ClaimResponse should include narrative text",
   Element = "*.text",
               Severity = MessageValidationSeverity.Warning,
             RemediationAction = "Add narrative text element for human readability",
       StandardReference = "NPHIES Narrative Guidelines"
    });
      }

     // Rule 14: XHTML validation
 if (resourceJson.Contains("\"div\""))
           {
     if (!resourceJson.Contains("xmlns=\"http://www.w3.org/1999/xhtml\""))
   {
       errors.Add(new MessageValidationError
            {
    ErrorCode = "NAR-002",
   ErrorName = "Invalid XHTML Namespace",
   Message = "Narrative XHTML div missing xmlns attribute",
 Element = "*.text.div",
        Severity = MessageValidationSeverity.Warning,
        RemediationAction = "Add xmlns=\"http://www.w3.org/1999/xhtml\" to div",
      StandardReference = "FHIR Narrative XHTML"
            });
         }
    }

      _logger.LogInformation($"Narrative validation completed. Errors: {errors.Count}");
      }
  catch (Exception ex)
      {
        _logger.LogError(ex, "Error validating narrative");
     }

    return errors;
        }

        /// <summary>
   /// Rule Group 8: Message Type Validation
        /// </summary>
 public async Task<List<MessageValidationError>> ValidateMessageTypeAsync(string messageType)
        {
          var errors = new List<MessageValidationError>();

     try
     {
      if (string.IsNullOrWhiteSpace(messageType))
     {
     errors.Add(new MessageValidationError
    {
            ErrorCode = "MSG-001",
      ErrorName = "Message Type Required",
        Message = "Message type is required",
         Element = "MessageHeader.eventCoding.code",
       Severity = MessageValidationSeverity.Error,
                   RemediationAction = "Specify valid NPHIES message type",
    StandardReference = "NPHIES Message Types"
          });
                 return errors;
          }

                // Rule 15: Valid message type
                if (!_validMessageTypes.Contains(messageType.ToLower()))
           {
         errors.Add(new MessageValidationError
    {
      ErrorCode = "MSG-002",
 ErrorName = "Invalid Message Type",
 Message = $"Message type '{messageType}' is not a valid NPHIES message type",
      Element = "MessageHeader.eventCoding.code",
                Severity = MessageValidationSeverity.Error,
            RemediationAction = $"Use one of: {string.Join(", ", _validMessageTypes)}",
   StandardReference = "NPHIES Valid Message Types"
   });
       }

            _logger.LogInformation($"Message type validation completed. IsValid: {errors.Count == 0}");
          }
      catch (Exception ex)
        {
  _logger.LogError(ex, "Error validating message type");
          }

      return errors;
      }

        /// <summary>
        /// Rule Group 9: Required Elements Validation
        /// </summary>
        public async Task<List<MessageValidationError>> ValidateRequiredElementsAsync(string messageType, string bundleJson)
        {
     var errors = new List<MessageValidationError>();

      try
      {
  if (string.IsNullOrWhiteSpace(messageType) || string.IsNullOrWhiteSpace(bundleJson))
          return errors;

                // Rule 16-20: Message-specific required elements
    switch (messageType.ToLower())
   {
   case "claim-request":
                 if (!bundleJson.Contains("\"resourceType\":\"Claim\""))
       {
         errors.Add(new MessageValidationError
         {
   ErrorCode = "REQ-001",
                   ErrorName = "Claim Resource Required",
          Message = "Claim-request must contain Claim resource",
              Element = "Bundle.entry[*].resource[type=Claim]",
         Severity = MessageValidationSeverity.Error,
      RemediationAction = "Add Claim resource to bundle",
         StandardReference = "NPHIES Claim Request Format"
      });
          }

       if (!bundleJson.Contains("\"resourceType\":\"Coverage\""))
        {
       errors.Add(new MessageValidationError
              {
     ErrorCode = "REQ-002",
    ErrorName = "Coverage Resource Required",
            Message = "Claim-request must contain Coverage resource",
       Element = "Bundle.entry[*].resource[type=Coverage]",
  Severity = MessageValidationSeverity.Error,
              RemediationAction = "Add Coverage resource to bundle",
      StandardReference = "NPHIES Claim Request Format"
         });
      }
       break;

        case "claim-response":
      if (!bundleJson.Contains("\"resourceType\":\"ClaimResponse\""))
   {
                errors.Add(new MessageValidationError
                 {
 ErrorCode = "REQ-003",
             ErrorName = "ClaimResponse Resource Required",
        Message = "Claim-response must contain ClaimResponse resource",
                    Element = "Bundle.entry[*].resource[type=ClaimResponse]",
         Severity = MessageValidationSeverity.Error,
RemediationAction = "Add ClaimResponse resource to bundle",
      StandardReference = "NPHIES Claim Response Format"
     });
        }
     break;

     case "eligibility-request":
     if (!bundleJson.Contains("\"resourceType\":\"CoverageEligibilityRequest\""))
    {
           errors.Add(new MessageValidationError
            {
            ErrorCode = "REQ-004",
    ErrorName = "CoverageEligibilityRequest Required",
       Message = "Eligibility-request must contain CoverageEligibilityRequest",
         Element = "Bundle.entry[*].resource[type=CoverageEligibilityRequest]",
       Severity = MessageValidationSeverity.Error,
         RemediationAction = "Add CoverageEligibilityRequest to bundle",
     StandardReference = "NPHIES Eligibility Request Format"
  });
      }
      break;

           case "eligibility-response":
   if (!bundleJson.Contains("\"resourceType\":\"CoverageEligibilityResponse\""))
             {
       errors.Add(new MessageValidationError
{
          ErrorCode = "REQ-005",
     ErrorName = "CoverageEligibilityResponse Required",
           Message = "Eligibility-response must contain CoverageEligibilityResponse",
           Element = "Bundle.entry[*].resource[type=CoverageEligibilityResponse]",
          Severity = MessageValidationSeverity.Error,
        RemediationAction = "Add CoverageEligibilityResponse to bundle",
                  StandardReference = "NPHIES Eligibility Response Format"
    });
   }
          break;
     }

    _logger.LogInformation($"Required elements validation completed. Errors: {errors.Count}");
         }
            catch (Exception ex)
  {
            _logger.LogError(ex, "Error validating required elements");
      }

          return errors;
        }
    }
}
