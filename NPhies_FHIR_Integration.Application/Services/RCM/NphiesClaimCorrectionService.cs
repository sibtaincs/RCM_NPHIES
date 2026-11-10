using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM
{
    /// <summary>
    /// NPHIES Claim Correction & Resubmission Service
    /// Manages claim corrections and resubmissions per NPHIES rules
    /// Implements NPHIES claim correction requirements
    /// </summary>
    public interface INphiesClaimCorrectionService
    {
Task<ClaimCorrectionResponse> SubmitClaimCorrectionAsync(ClaimCorrectionDto correction);
Task<ClaimCorrectionStatus> GetCorrectionStatusAsync(string correctionId);
        Task<List<ClaimCorrectionDto>> GetClaimCorrectionsAsync(string originalClaimId);
    Task<bool> ValidateCorrectionAsync(ClaimCorrectionDto correction);
        Task<ClaimResubmissionResponse> ResubmitClaimAsync(string correctionId);
Task<List<string>> GetAllowedCorrectionFieldsAsync(string claimType);
        Task<ClaimCorrectionStatistics> GetCorrectionStatisticsAsync(string providerId);
    }

    /// <summary>
    /// Claim correction DTO
    /// </summary>
    public class ClaimCorrectionDto
    {
        public string OriginalClaimId { get; set; } = string.Empty;
        public string NphiesClaimId { get; set; } = string.Empty;
     public string CorrectionReason { get; set; } = string.Empty;
        public Dictionary<string, string> FieldsToCorrect { get; set; } = new();
        public string CorrectionDetails { get; set; } = string.Empty;
     public List<string> SupportingDocuments { get; set; } = new();
        public DateTime CorrectionDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// Claim correction response DTO
    /// </summary>
    public class ClaimCorrectionResponse
    {
        public bool Success { get; set; }
        public string CorrectionId { get; set; } = string.Empty;
        public string OriginalClaimId { get; set; } = string.Empty;
   public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "submitted"; // submitted, accepted, processing, completed, rejected
        public string Message { get; set; } = string.Empty;
  }

    /// <summary>
    /// Claim correction status DTO
    /// </summary>
 public class ClaimCorrectionStatus
    {
   public string CorrectionId { get; set; } = string.Empty;
    public string OriginalClaimId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
     public DateTime? CompletedAt { get; set; }
     public int TotalAttempts { get; set; }
   public bool Accepted { get; set; }
        public string RejectionReason { get; set; } = string.Empty;
    }

    /// <summary>
    /// Claim resubmission response DTO
    /// </summary>
    public class ClaimResubmissionResponse
    {
      public bool Success { get; set; }
        public string NewClaimId { get; set; } = string.Empty;
        public string CorrectedClaimId { get; set; } = string.Empty;
     public DateTime ResubmittedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "submitted";
    }

    /// <summary>
    /// Claim correction statistics DTO
    /// </summary>
    public class ClaimCorrectionStatistics
    {
        public string ProviderId { get; set; } = string.Empty;
        public int TotalCorrections { get; set; }
 public int AcceptedCorrections { get; set; }
        public int RejectedCorrections { get; set; }
        public int SuccessfulResubmissions { get; set; }
   public double AcceptanceRate { get; set; }
      public DateTime ReportDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// NPHIES Claim Correction Service Implementation
    /// </summary>
    public class NphiesClaimCorrectionService : INphiesClaimCorrectionService
    {
        private readonly Dictionary<string, ClaimCorrectionResponse> _corrections = new();
        private readonly Dictionary<string, ClaimCorrectionStatus> _correctionStatuses = new();
        private readonly List<ClaimCorrectionDto> _allCorrections = new();

        // Allowed fields for correction by claim type
        private static readonly Dictionary<string, List<string>> AllowedCorrectionFields = new()
        {
   { "medical", new List<string> { "DiagnosisCodes", "ServiceCodes", "Amount", "ServiceDate", "Modifiers" } },
            { "dental", new List<string> { "ToothNumber", "ServiceCodes", "Amount", "ServiceDate" } },
      { "pharmacy", new List<string> { "NDCCode", "Quantity", "DaysSupply", "Amount" } },
        };

        /// <summary>
        /// Submits a claim correction
        /// </summary>
     public async Task<ClaimCorrectionResponse> SubmitClaimCorrectionAsync(ClaimCorrectionDto correction)
        {
        try
         {
      if (correction == null)
     {
       return new ClaimCorrectionResponse { Success = false, Message = "Correction is required" };
}

    var correctionId = GenerateCorrectionId();

    var response = new ClaimCorrectionResponse
        {
  Success = true,
          CorrectionId = correctionId,
             OriginalClaimId = correction.OriginalClaimId,
     SubmittedAt = DateTime.UtcNow,
     Status = "submitted",
         Message = "Claim correction submitted successfully"
       };

 _corrections[correctionId] = response;

         _correctionStatuses[correctionId] = new ClaimCorrectionStatus
   {
   CorrectionId = correctionId,
       OriginalClaimId = correction.OriginalClaimId,
         Status = "submitted",
             SubmittedAt = DateTime.UtcNow,
              TotalAttempts = 1
    };

            _allCorrections.Add(correction);

         return response;
  }
            catch
 {
                return new ClaimCorrectionResponse { Success = false, Message = "Error submitting correction" };
   }
        }

        /// <summary>
        /// Gets correction status
        /// </summary>
      public async Task<ClaimCorrectionStatus> GetCorrectionStatusAsync(string correctionId)
   {
            try
            {
          if (string.IsNullOrWhiteSpace(correctionId))
  {
           return null;
     }

       if (_correctionStatuses.TryGetValue(correctionId, out var status))
             {
           return status;
  }

  return null;
       }
         catch
   {
                return null;
            }
        }

 /// <summary>
        /// Gets all corrections for a claim
     /// </summary>
        public async Task<List<ClaimCorrectionDto>> GetClaimCorrectionsAsync(string originalClaimId)
      {
            try
            {
          var corrections = _allCorrections
      .Where(c => c.OriginalClaimId == originalClaimId)
              .OrderByDescending(c => c.CorrectionDate)
          .ToList();

                return corrections;
            }
            catch
       {
                return new List<ClaimCorrectionDto>();
            }
      }

     /// <summary>
      /// Validates a correction
     /// </summary>
        public async Task<bool> ValidateCorrectionAsync(ClaimCorrectionDto correction)
        {
            try
     {
       if (correction == null)
            {
    return false;
                }

             if (string.IsNullOrWhiteSpace(correction.OriginalClaimId))
        {
       return false;
  }

   if (correction.FieldsToCorrect == null || correction.FieldsToCorrect.Count == 0)
        {
         return false;
              }

                return true;
       }
  catch
{
   return false;
            }
        }

        /// <summary>
        /// Resubmits a corrected claim
        /// </summary>
        public async Task<ClaimResubmissionResponse> ResubmitClaimAsync(string correctionId)
        {
            try
            {
     if (string.IsNullOrWhiteSpace(correctionId))
            {
      return new ClaimResubmissionResponse { Success = false };
                }

   var status = await GetCorrectionStatusAsync(correctionId);
      if (status == null)
        {
           return new ClaimResubmissionResponse { Success = false };
   }

     var newClaimId = GenerateNewClaimId();

         var response = new ClaimResubmissionResponse
    {
      Success = true,
NewClaimId = newClaimId,
    CorrectedClaimId = correctionId,
     ResubmittedAt = DateTime.UtcNow,
             Status = "submitted"
    };

            status.Status = "completed";
          status.CompletedAt = DateTime.UtcNow;

      return response;
  }
         catch
       {
       return new ClaimResubmissionResponse { Success = false };
  }
        }

        /// <summary>
   /// Gets allowed correction fields for claim type
        /// </summary>
        public async Task<List<string>> GetAllowedCorrectionFieldsAsync(string claimType)
        {
            try
            {
    if (AllowedCorrectionFields.TryGetValue(claimType.ToLower(), out var fields))
         {
      return fields;
                }

 return new List<string>();
            }
            catch
        {
        return new List<string>();
   }
        }

        /// <summary>
        /// Gets correction statistics
        /// </summary>
        public async Task<ClaimCorrectionStatistics> GetCorrectionStatisticsAsync(string providerId)
  {
            try
 {
                var stats = new ClaimCorrectionStatistics
      {
           ProviderId = providerId,
         TotalCorrections = _allCorrections.Count,
      ReportDate = DateTime.UtcNow
       };

      var acceptedCount = _correctionStatuses.Values.Count(s => s.Accepted);
                var rejectedCount = _correctionStatuses.Values.Count(s => !s.Accepted && s.Status == "completed");

      stats.AcceptedCorrections = acceptedCount;
   stats.RejectedCorrections = rejectedCount;

if (stats.TotalCorrections > 0)
                {
        stats.AcceptanceRate = (double)stats.AcceptedCorrections / stats.TotalCorrections;
   }

       return stats;
            }
    catch
            {
       return new ClaimCorrectionStatistics { ProviderId = providerId };
            }
        }

        private string GenerateCorrectionId()
        {
   return $"CORR{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }

        private string GenerateNewClaimId()
        {
            return $"CLM{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }
    }
}
