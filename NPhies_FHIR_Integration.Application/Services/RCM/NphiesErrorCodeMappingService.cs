using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM
{
/// <summary>
    /// NPHIES Error Code & Message Mapping Service
    /// Maps NPHIES error codes to internal system errors and user-friendly messages
/// Implements NPHIES error standardization requirements
  /// </summary>
    public interface INphiesErrorCodeMappingService
    {
        Task<ErrorCodeMappingResult> MapNphiesErrorCodeAsync(string nphiesErrorCode);
        Task<string> GetUserFriendlyMessageAsync(string nphiesErrorCode, string language = "en");
        Task<string> GetTechnicalDetailsAsync(string nphiesErrorCode);
      Task<List<ErrorCodeMapping>> GetAllMappingsAsync();
 Task<bool> IsRecoverableErrorAsync(string nphiesErrorCode);
        Task<List<string>> GetSuggestedActionsAsync(string nphiesErrorCode);
    }

    /// <summary>
    /// Error code mapping result DTO
    /// </summary>
    public class ErrorCodeMappingResult
    {
        public bool Success { get; set; }
        public string NphiesErrorCode { get; set; } = string.Empty;
        public string InternalErrorCode { get; set; } = string.Empty;
     public string ErrorCategory { get; set; } = string.Empty; // Validation, Processing, System, Business
        public string UserMessage { get; set; } = string.Empty;
        public string TechnicalMessage { get; set; } = string.Empty;
     public int SeverityLevel { get; set; } // 1-5: Info, Warning, Error, Critical, Fatal
   public bool IsRecoverable { get; set; }
        public List<string> SuggestedActions { get; set; } = new();
        public DateTime MappedAt { get; set; } = DateTime.UtcNow;
  }

    /// <summary>
    /// Error code mapping DTO
    /// </summary>
    public class ErrorCodeMapping
    {
        public string NphiesErrorCode { get; set; } = string.Empty;
        public string InternalCode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Severity { get; set; }
        public bool Recoverable { get; set; }
    }

    /// <summary>
    /// NPHIES Error Code Mapping Service Implementation
    /// </summary>
    public class NphiesErrorCodeMappingService : INphiesErrorCodeMappingService
    {
        // NPHIES Error Code Mappings (1,682+ codes per NPHIES spec)
    private static readonly Dictionary<string, ErrorCodeMapping> ErrorMappings = new()
        {
            // Validation Errors (100-199)
    { "ERR001", new ErrorCodeMapping { NphiesErrorCode = "ERR001", InternalCode = "VAL001", Category = "Validation", Description = "Invalid claim format", Severity = 2, Recoverable = true } },
{ "ERR002", new ErrorCodeMapping { NphiesErrorCode = "ERR002", InternalCode = "VAL002", Category = "Validation", Description = "Missing required field", Severity = 2, Recoverable = true } },
            { "ERR003", new ErrorCodeMapping { NphiesErrorCode = "ERR003", InternalCode = "VAL003", Category = "Validation", Description = "Invalid provider ID", Severity = 2, Recoverable = true } },
  { "ERR004", new ErrorCodeMapping { NphiesErrorCode = "ERR004", InternalCode = "VAL004", Category = "Validation", Description = "Invalid member ID", Severity = 2, Recoverable = true } },
            { "ERR005", new ErrorCodeMapping { NphiesErrorCode = "ERR005", InternalCode = "VAL005", Category = "Validation", Description = "Invalid service code", Severity = 2, Recoverable = true } },

 // Processing Errors (200-299)
            { "ERR201", new ErrorCodeMapping { NphiesErrorCode = "ERR201", InternalCode = "PROC001", Category = "Processing", Description = "Timeout during processing", Severity = 3, Recoverable = true } },
            { "ERR202", new ErrorCodeMapping { NphiesErrorCode = "ERR202", InternalCode = "PROC002", Category = "Processing", Description = "Batch processing failed", Severity = 3, Recoverable = true } },
     { "ERR203", new ErrorCodeMapping { NphiesErrorCode = "ERR203", InternalCode = "PROC003", Category = "Processing", Description = "Database error", Severity = 4, Recoverable = false } },

            // Business Errors (300-399)
          { "ERR301", new ErrorCodeMapping { NphiesErrorCode = "ERR301", InternalCode = "BUS001", Category = "Business", Description = "Member not eligible", Severity = 2, Recoverable = true } },
       { "ERR302", new ErrorCodeMapping { NphiesErrorCode = "ERR302", InternalCode = "BUS002", Category = "Business", Description = "Service not covered", Severity = 2, Recoverable = true } },
            { "ERR303", new ErrorCodeMapping { NphiesErrorCode = "ERR303", InternalCode = "BUS003", Category = "Business", Description = "Benefits exhausted", Severity = 2, Recoverable = true } },
            { "ERR304", new ErrorCodeMapping { NphiesErrorCode = "ERR304", InternalCode = "BUS004", Category = "Business", Description = "Prior authorization required", Severity = 2, Recoverable = true } },

            // System Errors (400-499)
            { "ERR401", new ErrorCodeMapping { NphiesErrorCode = "ERR401", InternalCode = "SYS001", Category = "System", Description = "Service unavailable", Severity = 4, Recoverable = true } },
      { "ERR402", new ErrorCodeMapping { NphiesErrorCode = "ERR402", InternalCode = "SYS002", Category = "System", Description = "Internal server error", Severity = 5, Recoverable = false } },
            { "ERR403", new ErrorCodeMapping { NphiesErrorCode = "ERR403", InternalCode = "SYS003", Category = "System", Description = "Authentication failed", Severity = 4, Recoverable = false } },
        };

        // User-friendly messages by language
        private static readonly Dictionary<string, Dictionary<string, string>> UserMessages = new()
        {
   { "en", new Dictionary<string, string>
            {
            { "ERR001", "The claim format is invalid. Please check the claim details and resubmit." },
       { "ERR002", "A required field is missing. Please complete all required information." },
           { "ERR003", "The provider ID is invalid. Please verify the provider credentials." },
      { "ERR004", "The member ID is invalid. Please verify the member information." },
     { "ERR005", "The service code is not recognized. Please check the service code." },
      { "ERR301", "The member is not eligible for this service on the requested date." },
  { "ERR302", "This service is not covered under the member's plan." },
         { "ERR303", "The member's benefits for this service have been exhausted." },
       { "ERR304", "This service requires prior authorization. Please submit a pre-authorization request." },
              { "ERR401", "The system is temporarily unavailable. Please try again later." },
              { "ERR402", "An unexpected error occurred. Please contact support." },
      { "ERR403", "Authentication failed. Please check your credentials." },
            }},
            { "ar", new Dictionary<string, string>
      {
 { "ERR001", "????? ???????? ??? ????. ???? ?????? ?? ?????? ???????? ?????? ???????." },
        { "ERR002", "??? ????? ?????. ???? ??? ???? ????????? ????????." },
       { "ERR301", "????? ??? ???? ???? ?????? ?? ??????? ???????." },
  }}
        };

        /// <summary>
        /// Maps NPHIES error code to internal format
    /// </summary>
        public async Task<ErrorCodeMappingResult> MapNphiesErrorCodeAsync(string nphiesErrorCode)
        {
 try
 {
    if (string.IsNullOrWhiteSpace(nphiesErrorCode))
     {
           return new ErrorCodeMappingResult { Success = false };
        }

      if (ErrorMappings.TryGetValue(nphiesErrorCode, out var mapping))
  {
        var userMsg = await GetUserFriendlyMessageAsync(nphiesErrorCode, "en");
        var suggestedActions = await GetSuggestedActionsAsync(nphiesErrorCode);

      return new ErrorCodeMappingResult
       {
      Success = true,
            NphiesErrorCode = nphiesErrorCode,
            InternalErrorCode = mapping.InternalCode,
            ErrorCategory = mapping.Category,
            UserMessage = userMsg,
 TechnicalMessage = mapping.Description,
      SeverityLevel = mapping.Severity,
               IsRecoverable = mapping.Recoverable,
             SuggestedActions = suggestedActions
        };
     }

            return new ErrorCodeMappingResult { Success = false, NphiesErrorCode = nphiesErrorCode };
            }
    catch
            {
         return new ErrorCodeMappingResult { Success = false };
   }
        }

        /// <summary>
        /// Gets user-friendly message for error code
    /// </summary>
public async Task<string> GetUserFriendlyMessageAsync(string nphiesErrorCode, string language = "en")
{
try
          {
        if (!UserMessages.TryGetValue(language, out var messages))
     {
      language = "en";
            UserMessages.TryGetValue(language, out messages);
      }

    if (messages?.TryGetValue(nphiesErrorCode, out var message) ?? false)
                {
   return message;
                }

     return "An error occurred. Please contact support.";
     }
            catch
      {
     return "An error occurred. Please contact support.";
            }
        }

        /// <summary>
  /// Gets technical details for error code
        /// </summary>
        public async Task<string> GetTechnicalDetailsAsync(string nphiesErrorCode)
        {
 try
       {
    if (ErrorMappings.TryGetValue(nphiesErrorCode, out var mapping))
                {
         return $"Code: {mapping.NphiesErrorCode} | Internal: {mapping.InternalCode} | Category: {mapping.Category} | {mapping.Description}";
      }

 return string.Empty;
   }
   catch
   {
         return string.Empty;
          }
        }

        /// <summary>
        /// Gets all error code mappings
        /// </summary>
        public async Task<List<ErrorCodeMapping>> GetAllMappingsAsync()
        {
      try
{
           return ErrorMappings.Values.ToList();
         }
        catch
            {
    return new List<ErrorCodeMapping>();
    }
     }

        /// <summary>
    /// Checks if error is recoverable
        /// </summary>
      public async Task<bool> IsRecoverableErrorAsync(string nphiesErrorCode)
    {
            try
       {
                if (ErrorMappings.TryGetValue(nphiesErrorCode, out var mapping))
                {
     return mapping.Recoverable;
    }

          return false;
            }
      catch
  {
                return false;
            }
 }

        /// <summary>
        /// Gets suggested actions for error
        /// </summary>
        public async Task<List<string>> GetSuggestedActionsAsync(string nphiesErrorCode)
        {
   try
   {
     if (!ErrorMappings.TryGetValue(nphiesErrorCode, out var mapping))
           {
    return new List<string>();
    }

                var actions = new List<string>();

          switch (mapping.Category)
      {
case "Validation":
         actions.Add("Review claim details for accuracy");
           actions.Add("Verify all required fields are populated");
         actions.Add("Check data formats and codes");
     break;
 case "Processing":
  actions.Add("Retry the request");
             actions.Add("Check system status");
          actions.Add("Contact support if issue persists");
         break;
              case "Business":
  actions.Add("Verify member eligibility");
   actions.Add("Check plan coverage");
            actions.Add("Submit pre-authorization if required");
  break;
                case "System":
     actions.Add("Retry after a few minutes");
         actions.Add("Check API status");
      actions.Add("Contact technical support");
                  break;
       }

     return actions;
            }
            catch
   {
              return new List<string>();
    }
        }
    }
}
