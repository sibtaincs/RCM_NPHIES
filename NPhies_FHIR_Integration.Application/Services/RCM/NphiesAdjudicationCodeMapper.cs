using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace NPhies_FHIR_Integration.Application.Services.RCM
{
    /// <summary>
    /// NPHIES Adjudication Code Mapper
    /// Maps internal adjudication decisions to NPHIES-approved codes
    /// Implements NPHIES adjudication code requirements
    /// </summary>
    public interface INphiesAdjudicationCodeMapper
    {
   Task<AdjudicationCodeMappingResult> MapDenialReasonAsync(string internalReason);
        Task<AdjudicationCodeMappingResult> MapAdjudicationDecisionAsync(string internalDecision);
    Task<List<AdjudicationMapping>> GetAllMappingsAsync();
 Task<PartialApprovalMapping> MapPartialApprovalAsync(decimal requestedAmount, decimal approvedAmount, string reason);
        Task<string> GetNphiesCodeAsync(string internalCode, string codeType);
        Task<bool> IsValidNphiesCodeAsync(string nphiesCode, string codeType);
    }

    /// <summary>
    /// Adjudication code mapping result
    /// </summary>
    public class AdjudicationCodeMappingResult
    {
        public bool Success { get; set; }
        public string InternalCode { get; set; } = string.Empty;
     public string NphiesCode { get; set; } = string.Empty;
        public string CodeDescription { get; set; } = string.Empty;
  public string Category { get; set; } = string.Empty; // Approval, Denial, Partial
        public List<string> SubCodes { get; set; } = new();
     public string NphiesReference { get; set; } = string.Empty;
    }

    /// <summary>
    /// Adjudication mapping DTO
    /// </summary>
    public class AdjudicationMapping
    {
 public string InternalCode { get; set; } = string.Empty;
  public string NphiesCode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty; // Critical, Major, Minor
    }

    /// <summary>
    /// Partial approval mapping DTO
    /// </summary>
    public class PartialApprovalMapping
    {
        public decimal RequestedAmount { get; set; }
        public decimal ApprovedAmount { get; set; }
        public decimal DeniedAmount { get; set; }
  public string ApprovalPercentage { get; set; } = string.Empty;
   public string NphiesCode { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public List<string> ReasonCodes { get; set; } = new();
    }

    /// <summary>
    /// NPHIES Adjudication Code Mapper Implementation
  /// </summary>
    public class NphiesAdjudicationCodeMapper : INphiesAdjudicationCodeMapper
    {
        private readonly ILogger<NphiesAdjudicationCodeMapper> _logger;

   // NPHIES Approval Codes
        private static readonly Dictionary<string, AdjudicationMapping> ApprovalCodes = new()
    {
    { "APPROVED", new AdjudicationMapping 
      { 
             InternalCode = "APPROVED", 
       NphiesCode = "complete", 
          Description = "Claim fully approved and payable",
     Category = "Approval",
           Severity = "Minor"
            }},
          { "APPROVED_WITH_CONDITIONS", new AdjudicationMapping 
            { 
                InternalCode = "APPROVED_WITH_CONDITIONS", 
                NphiesCode = "complete", 
          Description = "Claim approved with conditions",
                Category = "Approval",
       Severity = "Major"
       }},
        };

        // NPHIES Partial Approval Codes
 private static readonly Dictionary<string, AdjudicationMapping> PartialApprovalCodes = new()
        {
   { "PARTIAL", new AdjudicationMapping 
            { 
InternalCode = "PARTIAL", 
         NphiesCode = "partial", 
           Description = "Claim partially approved",
         Category = "Partial",
   Severity = "Major"
            }},
      { "PARTIAL_WITH_DEDUCTIBLE", new AdjudicationMapping 
     { 
          InternalCode = "PARTIAL_WITH_DEDUCTIBLE", 
      NphiesCode = "partial", 
     Description = "Partial approval after deductible",
           Category = "Partial",
          Severity = "Major"
     }},
  { "PARTIAL_WITH_COPAY", new AdjudicationMapping 
        { 
                InternalCode = "PARTIAL_WITH_COPAY", 
                NphiesCode = "partial", 
     Description = "Partial approval after copay",
          Category = "Partial",
        Severity = "Major"
            }},
        };

        // NPHIES Denial Codes
   private static readonly Dictionary<string, AdjudicationMapping> DenialCodes = new()
     {
 { "NOT_COVERED", new AdjudicationMapping 
            { 
       InternalCode = "NOT_COVERED", 
                NphiesCode = "error", 
 Description = "Service not covered under plan",
   Category = "Denial",
                Severity = "Critical"
      }},
            { "BENEFITS_EXHAUSTED", new AdjudicationMapping 
          { 
    InternalCode = "BENEFITS_EXHAUSTED", 
       NphiesCode = "error", 
       Description = "Benefits exhausted for this service",
    Category = "Denial",
      Severity = "Critical"
            }},
       { "AUTHORIZATION_REQUIRED", new AdjudicationMapping 
            { 
           InternalCode = "AUTHORIZATION_REQUIRED", 
    NphiesCode = "error", 
      Description = "Prior authorization required",
Category = "Denial",
                Severity = "Critical"
 }},
   { "DUPLICATE", new AdjudicationMapping 
     { 
         InternalCode = "DUPLICATE", 
       NphiesCode = "error", 
                Description = "Duplicate claim detected",
                Category = "Denial",
      Severity = "Major"
            }},
            { "INVALID_PROVIDER", new AdjudicationMapping 
   { 
                InternalCode = "INVALID_PROVIDER", 
    NphiesCode = "error", 
      Description = "Provider not authorized",
      Category = "Denial",
      Severity = "Critical"
    }},
            { "INVALID_MEMBER", new AdjudicationMapping 
       { 
        InternalCode = "INVALID_MEMBER", 
                NphiesCode = "error", 
  Description = "Member not eligible",
      Category = "Denial",
            Severity = "Critical"
         }},
  { "INVALID_SERVICE_DATE", new AdjudicationMapping 
            { 
     InternalCode = "INVALID_SERVICE_DATE", 
       NphiesCode = "error", 
          Description = "Service date outside coverage period",
     Category = "Denial",
         Severity = "Major"
            }},
 { "OUT_OF_NETWORK", new AdjudicationMapping 
 { 
      InternalCode = "OUT_OF_NETWORK", 
     NphiesCode = "error", 
                Description = "Out-of-network provider",
         Category = "Denial",
            Severity = "Major"
            }},
       { "MEDICAL_NECESSITY", new AdjudicationMapping 
            { 
                InternalCode = "MEDICAL_NECESSITY", 
            NphiesCode = "error", 
           Description = "Fails medical necessity review",
   Category = "Denial",
      Severity = "Critical"
       }},
  };

        // Sub-codes for more granular reporting
        private static readonly Dictionary<string, List<string>> SubCodeMap = new()
{
            { "NOT_COVERED", new List<string> { "EXC001", "EXC002", "EXC003" } },
    { "BENEFITS_EXHAUSTED", new List<string> { "BEN001", "BEN002" } },
     { "AUTHORIZATION_REQUIRED", new List<string> { "AUTH001", "AUTH002" } },
    { "MEDICAL_NECESSITY", new List<string> { "MED001", "MED002", "MED003" } },
        };

        public NphiesAdjudicationCodeMapper(ILogger<NphiesAdjudicationCodeMapper> logger)
        {
_logger = logger;
      }

     /// <summary>
        /// Maps internal denial reason to NPHIES code
     /// </summary>
        public async Task<AdjudicationCodeMappingResult> MapDenialReasonAsync(string internalReason)
     {
        try
            {
 _logger.LogInformation($"Mapping denial reason: {internalReason}");

           var result = new AdjudicationCodeMappingResult
      {
  InternalCode = internalReason,
    Category = "Denial"
    };

     if (string.IsNullOrWhiteSpace(internalReason))
                {
         result.Success = false;
           return result;
          }

       if (DenialCodes.TryGetValue(internalReason, out var mapping))
         {
    result.Success = true;
         result.NphiesCode = mapping.NphiesCode;
      result.CodeDescription = mapping.Description;
       result.NphiesReference = $"NPHIES-{mapping.NphiesCode.ToUpper()}";

  // Add sub-codes if available
  if (SubCodeMap.TryGetValue(internalReason, out var subCodes))
         {
               result.SubCodes = subCodes;
           }
    }
           else
     {
 result.Success = false;
        _logger.LogWarning($"No mapping found for denial reason: {internalReason}");
       }

    return result;
     }
catch (Exception ex)
            {
             _logger.LogError(ex, "Error mapping denial reason");
           return new AdjudicationCodeMappingResult { Success = false };
            }
        }

      /// <summary>
    /// Maps internal adjudication decision to NPHIES code
        /// </summary>
        public async Task<AdjudicationCodeMappingResult> MapAdjudicationDecisionAsync(string internalDecision)
        {
     try
            {
        _logger.LogInformation($"Mapping adjudication decision: {internalDecision}");

           var result = new AdjudicationCodeMappingResult
 {
     InternalCode = internalDecision
 };

           if (string.IsNullOrWhiteSpace(internalDecision))
        {
         result.Success = false;
        return result;
        }

         var decisionLower = internalDecision.ToUpper();

          // Check approval codes
                if (ApprovalCodes.TryGetValue(decisionLower, out var approvalMapping))
   {
        result.Success = true;
  result.NphiesCode = approvalMapping.NphiesCode;
     result.CodeDescription = approvalMapping.Description;
        result.Category = "Approval";
            result.NphiesReference = $"NPHIES-{approvalMapping.NphiesCode.ToUpper()}";
              return result;
            }

     // Check partial codes
       if (PartialApprovalCodes.TryGetValue(decisionLower, out var partialMapping))
 {
         result.Success = true;
         result.NphiesCode = partialMapping.NphiesCode;
result.CodeDescription = partialMapping.Description;
          result.Category = "Partial";
        result.NphiesReference = $"NPHIES-{partialMapping.NphiesCode.ToUpper()}";
      return result;
     }

    // Check denial codes
              if (DenialCodes.TryGetValue(decisionLower, out var denialMapping))
                {
        result.Success = true;
         result.NphiesCode = denialMapping.NphiesCode;
         result.CodeDescription = denialMapping.Description;
     result.Category = "Denial";
                result.NphiesReference = $"NPHIES-{denialMapping.NphiesCode.ToUpper()}";
    return result;
    }

       result.Success = false;
            _logger.LogWarning($"No mapping found for decision: {internalDecision}");
     return result;
            }
            catch (Exception ex)
            {
 _logger.LogError(ex, "Error mapping adjudication decision");
             return new AdjudicationCodeMappingResult { Success = false };
            }
        }

        /// <summary>
      /// Gets all adjudication mappings
        /// </summary>
   public async Task<List<AdjudicationMapping>> GetAllMappingsAsync()
    {
          try
  {
       var allMappings = new List<AdjudicationMapping>();
            allMappings.AddRange(ApprovalCodes.Values);
           allMappings.AddRange(PartialApprovalCodes.Values);
      allMappings.AddRange(DenialCodes.Values);
         return allMappings;
        }
            catch (Exception ex)
            {
         _logger.LogError(ex, "Error getting all mappings");
                return new List<AdjudicationMapping>();
            }
        }

        /// <summary>
        /// Maps partial approval with amount breakdown
        /// </summary>
        public async Task<PartialApprovalMapping> MapPartialApprovalAsync(decimal requestedAmount, decimal approvedAmount, string reason)
        {
    try
         {
        _logger.LogInformation($"Mapping partial approval: Requested={requestedAmount}, Approved={approvedAmount}");

  var mapping = new PartialApprovalMapping
           {
        RequestedAmount = requestedAmount,
  ApprovedAmount = approvedAmount,
          DeniedAmount = requestedAmount - approvedAmount,
       Reason = reason
  };

    // Calculate approval percentage
                if (requestedAmount > 0)
              {
      var percentage = (approvedAmount / requestedAmount) * 100;
            mapping.ApprovalPercentage = $"{percentage:F2}%";
            }

     // Get NPHIES code for partial approval
var partialMapping = await MapAdjudicationDecisionAsync("PARTIAL");
      if (partialMapping.Success)
             {
            mapping.NphiesCode = partialMapping.NphiesCode;
         }

        // Get reason codes
    if (!string.IsNullOrWhiteSpace(reason))
            {
        var reasonMapping = await MapDenialReasonAsync(reason);
          if (reasonMapping.Success)
           {
           mapping.ReasonCodes.Add(reasonMapping.NphiesCode);
          mapping.ReasonCodes.AddRange(reasonMapping.SubCodes);
            }
        }

return mapping;
            }
 catch (Exception ex)
      {
     _logger.LogError(ex, "Error mapping partial approval");
   return new PartialApprovalMapping { Reason = reason };
     }
   }

        /// <summary>
        /// Gets NPHIES code for internal code
        /// </summary>
        public async Task<string> GetNphiesCodeAsync(string internalCode, string codeType)
  {
        try
 {
                _logger.LogInformation($"Getting NPHIES code for: {internalCode} ({codeType})");

       Dictionary<string, AdjudicationMapping> codeDict = codeType switch
       {
   "approval" => ApprovalCodes,
         "partial" => PartialApprovalCodes,
    "denial" => DenialCodes,
       _ => new Dictionary<string, AdjudicationMapping>()
    };

    if (codeDict.TryGetValue(internalCode, out var mapping))
      {
          return mapping.NphiesCode;
       }

                return string.Empty;
            }
            catch (Exception ex)
            {
        _logger.LogError(ex, "Error getting NPHIES code");
    return string.Empty;
            }
        }

        /// <summary>
   /// Validates if code is valid NPHIES code
   /// </summary>
        public async Task<bool> IsValidNphiesCodeAsync(string nphiesCode, string codeType)
    {
      try
     {
     var validCodes = new[] { "complete", "partial", "error", "queued" };
             return validCodes.Contains(nphiesCode.ToLower());
   }
     catch (Exception ex)
         {
                _logger.LogError(ex, "Error validating NPHIES code");
 return false;
            }
  }
    }
}
