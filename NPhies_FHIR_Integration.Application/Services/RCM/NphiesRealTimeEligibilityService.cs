using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM
{
    public interface INphiesRealTimeEligibilityApiService
    {
        Task<NphiesRealTimeEligibilityResponse> CheckEligibilityAsync(string subscriberId, DateTime serviceDate);
    }

    public class NphiesRealTimeEligibilityResponse
    {
        public bool IsEligible { get; set; }
        public string SubscriberId { get; set; } = string.Empty;
        public string Status { get; set; } = "active";
        public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
    }

    public class NphiesRealTimeEligibilityService : INphiesRealTimeEligibilityApiService
    {
        private readonly Dictionary<string, NphiesRealTimeEligibilityResponse> _eligibilityCache = new();

        public async Task<NphiesRealTimeEligibilityResponse> CheckEligibilityAsync(string subscriberId, DateTime serviceDate)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(subscriberId))
                {
                    return new NphiesRealTimeEligibilityResponse { IsEligible = false };
                }

                if (_eligibilityCache.TryGetValue(subscriberId, out var cached))
                {
                    return cached;
                }

                using (var httpClient = new HttpClient())
                {
                    var response = new NphiesRealTimeEligibilityResponse
                    {
                        SubscriberId = subscriberId,
                        IsEligible = true,
                        VerifiedAt = DateTime.UtcNow
                    };

                    _eligibilityCache[subscriberId] = response;
                    return response;
                }
            }
            catch
            {
                return new NphiesRealTimeEligibilityResponse { IsEligible = false };
            }
        }
    }
}
