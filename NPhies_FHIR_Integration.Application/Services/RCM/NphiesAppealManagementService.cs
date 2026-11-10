using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM
{
    public interface INphiesAppealManagementService
    {
        Task<AppealSubmissionResponse> SubmitAppealAsync(AppealSubmissionDto appeal);
        Task<AppealStatusResponse> GetAppealStatusAsync(string appealId);
        Task<List<NphiesAppealRecord>> GetClaimAppealsAsync(string claimId);
  Task<bool> ValidateAppealAsync(AppealSubmissionDto appeal);
Task<AppealDecisionResponse> GetAppealDecisionAsync(string appealId);
        Task<AppealStatistics> GetAppealStatisticsAsync(string providerId);
        Task<List<string>> GetAppealDeadlineAsync(string claimId);
    }

    public class AppealSubmissionDto
    {
      public string ClaimId { get; set; } = string.Empty;
        public string NphiesClaimId { get; set; } = string.Empty;
        public string AppealReason { get; set; } = string.Empty;
        public string ClinicalJustification { get; set; } = string.Empty;
        public List<string> SupportingDocuments { get; set; } = new();
        public string ProviderComments { get; set; } = string.Empty;
        public DateTime SubmissionDate { get; set; } = DateTime.UtcNow;
    }

    public class AppealSubmissionResponse
 {
      public bool Success { get; set; }
  public string AppealId { get; set; } = string.Empty;
        public string ClaimId { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "submitted";
        public string Message { get; set; } = string.Empty;
        public DateTime DeadlineDate { get; set; }
    }

    public class AppealStatusResponse
{
        public string AppealId { get; set; } = string.Empty;
        public string ClaimId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
      public DateTime SubmittedAt { get; set; }
 public DateTime? DecidedAt { get; set; }
        public DateTime DeadlineDate { get; set; }
   public int DaysRemaining { get; set; }
    public string Decision { get; set; } = string.Empty;
    }

    public class NphiesAppealRecord
    {
  public string AppealId { get; set; } = string.Empty;
      public string ClaimId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
   public string Decision { get; set; } = string.Empty;
    }

    public class AppealDecisionResponse
    {
        public string AppealId { get; set; } = string.Empty;
        public string Decision { get; set; } = string.Empty;
      public DateTime DecisionDate { get; set; }
        public string DecisionReason { get; set; } = string.Empty;
        public decimal ApprovedAmount { get; set; }
        public List<string> AppealConditions { get; set; } = new();
        public bool FurtherAppealAllowed { get; set; }
  }

    public class AppealStatistics
    {
        public string ProviderId { get; set; } = string.Empty;
        public int TotalAppeals { get; set; }
   public int ApprovedAppeals { get; set; }
        public int DeniedAppeals { get; set; }
        public int PartiallyApprovedAppeals { get; set; }
        public int PendingAppeals { get; set; }
        public double ApprovalRate { get; set; }
        public DateTime ReportDate { get; set; } = DateTime.UtcNow;
  }

    public class NphiesAppealManagementService : INphiesAppealManagementService
    {
        private readonly Dictionary<string, AppealSubmissionResponse> _appeals = new();
     private readonly Dictionary<string, AppealStatusResponse> _appealStatuses = new();
   private readonly Dictionary<string, AppealDecisionResponse> _appealDecisions = new();
    private readonly List<NphiesAppealRecord> _allAppeals = new();
   private const int APPEAL_DEADLINE_DAYS = 30;

        public async Task<AppealSubmissionResponse> SubmitAppealAsync(AppealSubmissionDto appeal)
        {
try
         {
    if (appeal == null)
    {
               return new AppealSubmissionResponse { Success = false, Message = "Appeal is required" };
      }

          var appealId = GenerateAppealId();
     var deadlineDate = DateTime.UtcNow.AddDays(APPEAL_DEADLINE_DAYS);

       var response = new AppealSubmissionResponse
                {
          Success = true,
        AppealId = appealId,
         ClaimId = appeal.ClaimId,
         SubmittedAt = DateTime.UtcNow,
        Status = "submitted",
           Message = "Appeal submitted successfully",
        DeadlineDate = deadlineDate
        };

              _appeals[appealId] = response;

 _appealStatuses[appealId] = new AppealStatusResponse
          {
         AppealId = appealId,
         ClaimId = appeal.ClaimId,
           Status = "submitted",
 SubmittedAt = DateTime.UtcNow,
   DeadlineDate = deadlineDate,
      DaysRemaining = APPEAL_DEADLINE_DAYS,
  Decision = "pending"
    };

  _allAppeals.Add(new NphiesAppealRecord
        {
       AppealId = appealId,
         ClaimId = appeal.ClaimId,
           Status = "submitted",
      SubmittedAt = DateTime.UtcNow
       });

           return response;
            }
            catch
    {
       return new AppealSubmissionResponse { Success = false, Message = "Error submitting appeal" };
            }
        }

   public async Task<AppealStatusResponse> GetAppealStatusAsync(string appealId)
        {
            try
        {
           if (string.IsNullOrWhiteSpace(appealId))
        {
           return null;
         }

    if (_appealStatuses.TryGetValue(appealId, out var status))
                {
     status.DaysRemaining = (int)(status.DeadlineDate - DateTime.UtcNow).TotalDays;
        return status;
         }

 return null;
        }
            catch
            {
       return null;
         }
    }

      public async Task<List<NphiesAppealRecord>> GetClaimAppealsAsync(string claimId)
  {
    try
  {
       var appeals = _allAppeals
           .Where(a => a.ClaimId == claimId)
                .OrderByDescending(a => a.SubmittedAt)
           .ToList();

   return appeals;
   }
            catch
         {
           return new List<NphiesAppealRecord>();
      }
  }

        public async Task<bool> ValidateAppealAsync(AppealSubmissionDto appeal)
        {
  try
            {
       if (appeal == null || string.IsNullOrWhiteSpace(appeal.ClaimId) || string.IsNullOrWhiteSpace(appeal.AppealReason))
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

        public async Task<AppealDecisionResponse> GetAppealDecisionAsync(string appealId)
        {
     try
      {
        if (string.IsNullOrWhiteSpace(appealId))
          {
      return null;
              }

   if (_appealDecisions.TryGetValue(appealId, out var decision))
      {
         return decision;
             }

                return null;
            }
            catch
            {
     return null;
      }
   }

        public async Task<AppealStatistics> GetAppealStatisticsAsync(string providerId)
  {
            try
            {
  var stats = new AppealStatistics
    {
           ProviderId = providerId,
        TotalAppeals = _allAppeals.Count,
       ReportDate = DateTime.UtcNow
           };

       var approvedCount = _appealDecisions.Values.Count(d => d.Decision == "approved");
     var deniedCount = _appealDecisions.Values.Count(d => d.Decision == "denied");
     var partiallyApprovedCount = _appealDecisions.Values.Count(d => d.Decision == "partially_approved");

         stats.ApprovedAppeals = approvedCount;
     stats.DeniedAppeals = deniedCount;
         stats.PartiallyApprovedAppeals = partiallyApprovedCount;
           stats.PendingAppeals = _appealStatuses.Values.Count(s => s.Decision == "pending");

   if (stats.TotalAppeals > 0)
      {
      stats.ApprovalRate = (double)(approvedCount + partiallyApprovedCount) / stats.TotalAppeals;
        }

 return stats;
            }
            catch
 {
    return new AppealStatistics { ProviderId = providerId };
       }
        }

    public async Task<List<string>> GetAppealDeadlineAsync(string claimId)
        {
            try
            {
        var deadlineInfo = new List<string>
 {
           $"Appeal deadline: {APPEAL_DEADLINE_DAYS} days from denial",
   $"Submit supporting documentation with appeal",
            $"Appeals must include clinical justification",
       $"Decision typically provided within 30 days"
 };

  return deadlineInfo;
     }
            catch
            {
  return new List<string>();
      }
        }

        private string GenerateAppealId()
        {
  return $"APP{DateTime.UtcNow:yyyyMMddHHmmss}{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }
    }
}
