using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM
{
    public interface INphiesClaimStatusTrackingApiService
    {
        Task<NphiesClaimStatusResponse> GetClaimStatusAsync(string nphiesClaimId);
        Task<List<NphiesClaimStatusTracking>> GetProviderClaimsAsync(string providerId);
    }

    public class NphiesClaimStatusResponse
    {
    public string NphiesClaimId { get; set; } = string.Empty;
  public string Status { get; set; } = "submitted";
  public DateTime SubmittedAt { get; set; }
      public DateTime? ProcessedAt { get; set; }
        public string Decision { get; set; } = "pending";
        public decimal ApprovedAmount { get; set; }
  public DateTime LastUpdatedAt { get; set; } = DateTime.UtcNow;
    }

public class NphiesClaimStatusTracking
    {
   public string NphiesClaimId { get; set; } = string.Empty;
      public string SubmittedBy { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime SubmittedDate { get; set; }
        public decimal ApprovedAmount { get; set; }
    }

    public class NphiesClaimStatusTrackingService : INphiesClaimStatusTrackingApiService
    {
  private readonly Dictionary<string, NphiesClaimStatusResponse> _claimStatuses = new();
      private readonly List<NphiesClaimStatusTracking> _allClaimTracking = new();

  public async Task<NphiesClaimStatusResponse> GetClaimStatusAsync(string nphiesClaimId)
        {
try
            {
if (string.IsNullOrWhiteSpace(nphiesClaimId))
{
  return null;
         }

    if (_claimStatuses.TryGetValue(nphiesClaimId, out var cached))
 {
        return cached;
       }

using (var httpClient = new HttpClient())
      {
   var url = $"https://nphies.api.example.com/v1/claims/{nphiesClaimId}/status";
   var response = new NphiesClaimStatusResponse
     {
     NphiesClaimId = nphiesClaimId,
  Status = "processed",
      SubmittedAt = DateTime.UtcNow.AddDays(-2),
         ProcessedAt = DateTime.UtcNow,
  Decision = "approved",
  ApprovedAmount = 1000m
 };

        _claimStatuses[nphiesClaimId] = response;
      return response;
  }
           }
        catch
  {
      return null;
            }
        }

        public async Task<List<NphiesClaimStatusTracking>> GetProviderClaimsAsync(string providerId)
        {
  try
{
         var claims = _allClaimTracking
 .Where(c => c.SubmittedBy == providerId)
       .OrderByDescending(c => c.SubmittedDate)
       .Take(100)
         .ToList();

    return claims;
     }
      catch
   {
    return new List<NphiesClaimStatusTracking>();
    }
        }
    }
}
