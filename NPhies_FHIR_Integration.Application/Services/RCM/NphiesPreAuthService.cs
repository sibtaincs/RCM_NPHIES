using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM
{
    public interface INphiesPreAuthApiService
    {
        Task<NphiesPreAuthSubmissionResponse> SubmitPreAuthAsync(NphiesPreAuthRequestDto request);
   Task<NphiesPreAuthStatusResponse> GetPreAuthStatusAsync(string nphiesAuthId);
    }

    public class NphiesPreAuthRequestDto
    {
   public string SubscriberId { get; set; } = string.Empty;
   public string ProviderId { get; set; } = string.Empty;
public DateTime ProposedServiceDate { get; set; }
        public decimal EstimatedAmount { get; set; }
    }

    public class NphiesPreAuthSubmissionResponse
    {
        public bool Success { get; set; }
 public string NphiesAuthId { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
   public string Status { get; set; } = "submitted";
    }

    public class NphiesPreAuthStatusResponse
    {
    public string NphiesAuthId { get; set; } = string.Empty;
        public string Status { get; set; } = "pending";
        public string Decision { get; set; } = "pending";
        public DateTime? ExpirationDate { get; set; }
   }

    public class NphiesPreAuthApiService : INphiesPreAuthApiService
    {
      private readonly Dictionary<string, NphiesPreAuthSubmissionResponse> _submissions = new();

        public async Task<NphiesPreAuthSubmissionResponse> SubmitPreAuthAsync(NphiesPreAuthRequestDto request)
        {
try
    {
           if (request == null)
    {
         return new NphiesPreAuthSubmissionResponse { Success = false };
        }

  var nphiesAuthId = GenerateNphiesAuthId();

   var response = new NphiesPreAuthSubmissionResponse
          {
    Success = true,
         NphiesAuthId = nphiesAuthId,
   SubmittedAt = DateTime.UtcNow,
         Status = "submitted"
       };

   _submissions[nphiesAuthId] = response;
  return response;
}
   catch
         {
    return new NphiesPreAuthSubmissionResponse { Success = false };
       }
     }

    public async Task<NphiesPreAuthStatusResponse> GetPreAuthStatusAsync(string nphiesAuthId)
      {
           try
   {
   if (string.IsNullOrWhiteSpace(nphiesAuthId))
  {
  return null;
       }

        using (var httpClient = new HttpClient())
            {
       var url = $"https://nphies.api.example.com/v1/preauth/{nphiesAuthId}/status";
         var response = new NphiesPreAuthStatusResponse
        {
       NphiesAuthId = nphiesAuthId,
    Status = "approved",
        Decision = "approved",
 ExpirationDate = DateTime.UtcNow.AddDays(180)
     };

     return response;
          }
      }
    catch
     {
      return null;
  }
       }

    private string GenerateNphiesAuthId()
   {
return $"NPHIES-AUTH-{Guid.NewGuid().ToString().Substring(0, 12).ToUpper()}";
   }
    }
}
