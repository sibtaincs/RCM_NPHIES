using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM
{
    /// <summary>
    /// NPHIES Provider Network Management Service
    /// Manages provider network status and in-network verification
    /// Implements NPHIES network validation requirements
    /// </summary>
    public interface INphiesProviderNetworkService
    {
    Task<NetworkVerificationResponse> VerifyProviderNetworkAsync(string providerId);
        Task<NetworkStatusResponse> GetNetworkStatusAsync(string providerId);
   Task<bool> IsInNetworkAsync(string providerId, string insurerId);
 Task<ProviderCredentialsResponse> GetProviderCredentialsAsync(string providerId);
        Task<List<string>> GetNetworkDirectoryAsync(string insurerId);
        Task<NetworkStatistics> GetNetworkStatisticsAsync();
    }

    /// <summary>
    /// Network verification response DTO
    /// </summary>
    public class NetworkVerificationResponse
    {
    public bool Success { get; set; }
        public string ProviderId { get; set; } = string.Empty;
        public bool IsInNetwork { get; set; }
    public string NetworkStatus { get; set; } = string.Empty; // active, inactive, probation, terminated
        public DateTime VerifiedAt { get; set; } = DateTime.UtcNow;
  public string Message { get; set; } = string.Empty;
    }

    /// <summary>
  /// Network status response DTO
    /// </summary>
    public class NetworkStatusResponse
    {
        public string ProviderId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
 public DateTime? EffectiveDate { get; set; }
        public DateTime? TerminationDate { get; set; }
        public List<string> NetworkAffiliations { get; set; } = new();
      public bool CredentialsValid { get; set; }
public DateTime? CredentialsExpirationDate { get; set; }
    }

/// <summary>
    /// Provider credentials response DTO
    /// </summary>
 public class ProviderCredentialsResponse
    {
        public string ProviderId { get; set; } = string.Empty;
  public string ProviderName { get; set; } = string.Empty;
public string ProviderType { get; set; } = string.Empty; // Individual, Facility, Group
        public string LicenseNumber { get; set; } = string.Empty;
        public bool CredentialsVerified { get; set; }
public DateTime? ExpirationDate { get; set; }
   public string CredentialStatus { get; set; } = string.Empty;
    }

    /// <summary>
    /// Network statistics DTO
  /// </summary>
    public class NetworkStatistics
    {
     public int TotalProviders { get; set; }
        public int ActiveProviders { get; set; }
        public int InactiveProviders { get; set; }
        public int CredentialsExpiringSoon { get; set; }
        public int NetworkChanges { get; set; }
        public DateTime ReportDate { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// NPHIES Provider Network Management Service Implementation
 /// </summary>
    public class NphiesProviderNetworkService : INphiesProviderNetworkService
    {
        private readonly Dictionary<string, NetworkVerificationResponse> _networkVerifications = new();
        private readonly Dictionary<string, ProviderCredentialsResponse> _providerCredentials = new();
 private readonly Dictionary<string, List<string>> _networkAffiliations = new();

        // Sample provider network data (would come from NPHIES in production)
   private static readonly Dictionary<string, NetworkStatusResponse> ProviderNetworkStatus = new()
        {
          { "PROV001", new NetworkStatusResponse { ProviderId = "PROV001", Status = "active", CredentialsValid = true } },
            { "PROV002", new NetworkStatusResponse { ProviderId = "PROV002", Status = "active", CredentialsValid = true } },
            { "PROV003", new NetworkStatusResponse { ProviderId = "PROV003", Status = "inactive", CredentialsValid = false } },
        };

        /// <summary>
    /// Verifies provider network status
        /// </summary>
        public async Task<NetworkVerificationResponse> VerifyProviderNetworkAsync(string providerId)
        {
 try
   {
 if (string.IsNullOrWhiteSpace(providerId))
    {
      return new NetworkVerificationResponse { Success = false };
          }

   if (_networkVerifications.TryGetValue(providerId, out var cached))
    {
     return cached;
}

    var isInNetwork = ProviderNetworkStatus.TryGetValue(providerId, out var status);

  var response = new NetworkVerificationResponse
            {
Success = true,
ProviderId = providerId,
   IsInNetwork = isInNetwork && status?.Status == "active",
      NetworkStatus = isInNetwork ? status.Status : "unknown",
        VerifiedAt = DateTime.UtcNow,
   Message = isInNetwork ? "Provider is in network" : "Provider not found in network"
       };

  _networkVerifications[providerId] = response;
       return response;
            }
   catch
    {
       return new NetworkVerificationResponse { Success = false };
     }
   }

/// <summary>
    /// Gets network status for provider
        /// </summary>
        public async Task<NetworkStatusResponse> GetNetworkStatusAsync(string providerId)
  {
            try
 {
     if (ProviderNetworkStatus.TryGetValue(providerId, out var status))
      {
 return status;
       }

  return new NetworkStatusResponse { ProviderId = providerId, Status = "unknown" };
   }
            catch
        {
      return null;
     }
        }

  /// <summary>
 /// Checks if provider is in-network for insurer
        /// </summary>
        public async Task<bool> IsInNetworkAsync(string providerId, string insurerId)
        {
       try
            {
      var verification = await VerifyProviderNetworkAsync(providerId);

    if (!verification.Success || !verification.IsInNetwork)
          {
       return false;
           }

      // Check specific insurer affiliation
    if (_networkAffiliations.TryGetValue(providerId, out var insurers))
    {
  return insurers.Contains(insurerId);
            }

     return false;
         }
     catch
        {
          return false;
 }
        }

 /// <summary>
        /// Gets provider credentials
        /// </summary>
        public async Task<ProviderCredentialsResponse> GetProviderCredentialsAsync(string providerId)
        {
    try
 {
    if (_providerCredentials.TryGetValue(providerId, out var credentials))
     {
              return credentials;
   }

  var creds = new ProviderCredentialsResponse
   {
    ProviderId = providerId,
           ProviderName = $"Provider {providerId}",
     ProviderType = "Facility",
            LicenseNumber = $"LIC{providerId}",
         CredentialsVerified = true,
       ExpirationDate = DateTime.UtcNow.AddYears(1),
         CredentialStatus = "active"
        };

       _providerCredentials[providerId] = creds;
    return creds;
         }
    catch
       {
       return null;
       }
        }

        /// <summary>
        /// Gets network directory for insurer
     /// </summary>
        public async Task<List<string>> GetNetworkDirectoryAsync(string insurerId)
        {
       try
   {
     var activeProviders = ProviderNetworkStatus
 .Where(p => p.Value.Status == "active")
    .Select(p => p.Key)
  .ToList();

     return activeProviders;
        }
 catch
            {
   return new List<string>();
            }
        }

  /// <summary>
 /// Gets network statistics
        /// </summary>
        public async Task<NetworkStatistics> GetNetworkStatisticsAsync()
        {
          try
    {
      var stats = new NetworkStatistics
    {
       TotalProviders = ProviderNetworkStatus.Count,
     ActiveProviders = ProviderNetworkStatus.Values.Count(p => p.Status == "active"),
  InactiveProviders = ProviderNetworkStatus.Values.Count(p => p.Status == "inactive"),
      CredentialsExpiringSoon = _providerCredentials.Values.Count(c => 
          c.ExpirationDate.HasValue && (c.ExpirationDate.Value - DateTime.UtcNow).TotalDays < 30),
     ReportDate = DateTime.UtcNow
       };

        return stats;
   }
     catch
    {
    return new NetworkStatistics();
    }
        }
    }
}
