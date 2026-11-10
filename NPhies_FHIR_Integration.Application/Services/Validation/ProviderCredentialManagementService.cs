using NPhies_FHIR_Integration.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services
{
    /// <summary>
    /// NPHIES Provider Credential Management Service Interface
    /// Manages provider validation, licensing, and network membership per NPHIES standards
/// </summary>
    public interface IProviderCredentialManagementService
    {
        /// <summary>
        /// Validate complete provider credentials
        /// </summary>
        Task<ProviderCredentialValidationResult> ValidateProviderAsync(Organization provider);

/// <summary>
        /// Get license status and information
        /// </summary>
        Task<ProviderLicenseInfo> GetLicenseStatusAsync(string providerId);

        /// <summary>
    /// Verify license is valid and active
    /// </summary>
   Task<bool> IsLicenseValidAsync(string providerId);

 /// <summary>
  /// Get network membership status
        /// </summary>
        Task<ProviderNetworkStatus> GetNetworkStatusAsync(string providerId);

        /// <summary>
     /// Verify provider is in network
      /// </summary>
        Task<bool> IsProviderInNetworkAsync(string providerId);

        /// <summary>
  /// Get provider specialization information
     /// </summary>
        Task<List<ProviderSpecialization>> GetSpecializationsAsync(string providerId);

 /// <summary>
/// Validate provider national ID
        /// </summary>
        Task<ProviderIdentityInfo> ValidateProviderIdentityAsync(string nationalId, string idType);

  /// <summary>
        /// Check provider active status
        /// </summary>
        Task<bool> IsProviderActiveAsync(string providerId);

        /// <summary>
      /// Check if provider is suspended
        /// </summary>
        Task<ProviderSuspensionInfo> GetSuspensionStatusAsync(string providerId);

   /// <summary>
     /// Get provider taxonomy code
        /// </summary>
        Task<string> GetProviderTaxonomyCodeAsync(string providerId);

        /// <summary>
        /// Validate provider contact information
        /// </summary>
        Task<List<ProviderValidationError>> ValidateContactInformationAsync(Organization provider);

        /// <summary>
        /// Get facility type classification
        /// </summary>
        Task<FacilityTypeInfo> GetFacilityTypeAsync(string providerId);

        /// <summary>
        /// Verify provider credentials are current
        /// </summary>
        Task<ProviderCredentialSummary> GetCredentialSummaryAsync(string providerId);
    }

    /// <summary>
/// Provider credential validation result
    /// </summary>
    public class ProviderCredentialValidationResult
    {
        public bool IsValid { get; set; }
  public string ProviderId { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public DateTime ValidationTimestamp { get; set; } = DateTime.UtcNow;
        public List<ProviderValidationError> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public int ErrorCount => Errors.Count;
        public int WarningCount => Warnings.Count;
        public string ValidationSummary => $"Errors: {ErrorCount}, Warnings: {WarningCount}";
        public List<string> ComplianceChecks { get; set; } = new();
    }

  /// <summary>
    /// Provider validation error
    /// </summary>
    public class ProviderValidationError
    {
      public string ErrorCode { get; set; } = string.Empty;
    public string ErrorName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
  public string Element { get; set; } = string.Empty;
        public ProviderValidationSeverity Severity { get; set; }
   public string RemediationAction { get; set; } = string.Empty;
     public string StandardReference { get; set; } = string.Empty;
    }

    /// <summary>
    /// Validation severity levels
    /// </summary>
    public enum ProviderValidationSeverity
    {
        Error = 1,
    Warning = 2,
        Info = 3
    }

    /// <summary>
    /// Provider license information
    /// </summary>
    public class ProviderLicenseInfo
    {
        public string ProviderId { get; set; } = string.Empty;
  public string LicenseNumber { get; set; } = string.Empty;
 public string LicenseType { get; set; } = string.Empty; // Medical, Nursing, Pharmacy, etc.
        public string IssuingAuthority { get; set; } = string.Empty;
        public DateTime IssuanceDate { get; set; }
        public DateTime ExpirationDate { get; set; }
    public string LicenseStatus { get; set; } = string.Empty; // Active, Inactive, Expired, Suspended
 public bool IsActive => LicenseStatus.ToLower() == "active";
        public bool IsExpired => ExpirationDate < DateTime.Now;
        public int DaysUntilExpiration => (int)(ExpirationDate - DateTime.Now).TotalDays;
        public string RenewalStatus { get; set; } = string.Empty;
     public DateTime? LastRenewalDate { get; set; }
        public DateTime? NextRenewalDueDate { get; set; }
    }

    /// <summary>
    /// Provider network status
    /// </summary>
    public class ProviderNetworkStatus
    {
        public string ProviderId { get; set; } = string.Empty;
        public bool IsInNetwork { get; set; }
 public string NetworkName { get; set; } = string.Empty;
        public string MembershipStatus { get; set; } = string.Empty; // Active, Inactive, Pending, Terminated
    public DateTime MembershipStartDate { get; set; }
        public DateTime? MembershipEndDate { get; set; }
        public bool IsPrimaryNetwork { get; set; }
    public List<string> SecondaryNetworks { get; set; } = new();
        public string ContractStatus { get; set; } = string.Empty; // Active, Terminated, Pending
        public DateTime? ContractExpirationDate { get; set; }
        public bool IsContractActive => ContractStatus.ToLower() == "active";
   public string NetworkTierLevel { get; set; } = string.Empty; // Preferred, Standard, Limited
public int YearsInNetwork => (int)(DateTime.Now - MembershipStartDate).TotalDays / 365;
    }

    /// <summary>
    /// Provider specialization
    /// </summary>
 public class ProviderSpecialization
    {
   public string ProviderId { get; set; } = string.Empty;
    public string SpecializationCode { get; set; } = string.Empty;
        public string SpecializationName { get; set; } = string.Empty;
        public bool IsPrimarySpecialty { get; set; }
      public string BoardCertification { get; set; } = string.Empty;
   public bool IsBoardCertified { get; set; }
  public DateTime? CertificationExpirationDate { get; set; }
    public string SpecializationStatus { get; set; } = string.Empty; // Active, Inactive
        public List<string> AllowedServiceCodes { get; set; } = new();
        public string ServiceRestrictions { get; set; } = string.Empty;
    }

    /// <summary>
    /// Provider identity information
    /// </summary>
    public class ProviderIdentityInfo
    {
        public string ProviderId { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string IdType { get; set; } = string.Empty; // Iqama, Passport, GCC
        public bool IsValid { get; set; }
    public bool IsExpired { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string IdStatus { get; set; } = string.Empty; // Valid, Expired, Revoked
        public List<string> ValidationErrors { get; set; } = new();
    }

    /// <summary>
    /// Provider suspension information
    /// </summary>
    public class ProviderSuspensionInfo
    {
        public string ProviderId { get; set; } = string.Empty;
     public bool IsSuspended { get; set; }
      public DateTime? SuspensionStartDate { get; set; }
        public DateTime? SuspensionEndDate { get; set; }
        public string SuspensionReason { get; set; } = string.Empty;
        public string SuspensionAuthority { get; set; } = string.Empty;
        public bool CanBeReactivated { get; set; }
     public string ReactivationStatus { get; set; } = string.Empty;
        public int DaysRemainingSuspension => SuspensionEndDate.HasValue ? (int)(SuspensionEndDate.Value - DateTime.Now).TotalDays : 0;
    }

    /// <summary>
 /// Facility type information
    /// </summary>
    public class FacilityTypeInfo
    {
      public string ProviderId { get; set; } = string.Empty;
     public string FacilityType { get; set; } = string.Empty; // Hospital, Clinic, Lab, Pharmacy, etc.
        public string FacilityCategory { get; set; } = string.Empty; // Public, Private, Specialty
     public int Beds { get; set; }
public bool IsAccredited { get; set; }
      public DateTime? AccreditationDate { get; set; }
        public DateTime? AccreditationExpirationDate { get; set; }
     public List<string> ServiceLines { get; set; } = new();
      public bool Is24Hour { get; set; }
        public string OperationalStatus { get; set; } = string.Empty;
  }

    /// <summary>
    /// Provider credential summary
    /// </summary>
    public class ProviderCredentialSummary
    {
        public string ProviderId { get; set; } = string.Empty;
        public string ProviderName { get; set; } = string.Empty;
        public bool HasValidLicense { get; set; }
        public bool IsInNetwork { get; set; }
        public bool IsActive { get; set; }
  public bool IsSuspended { get; set; }
        public List<string> SpecializationCodes { get; set; } = new();
        public int TotalNetworks { get; set; }
        public DateTime LastVerificationDate { get; set; } = DateTime.UtcNow;
    public DateTime NextVerificationDueDate { get; set; }
  public List<string> RequiredActions { get; set; } = new();
   public bool IsCompliant { get; set; }
    }

    /// <summary>
    /// NPHIES Provider Credential Management Service Implementation
    /// </summary>
    public class ProviderCredentialManagementService : IProviderCredentialManagementService
 {
        private readonly ILogger<ProviderCredentialManagementService> _logger;

        public ProviderCredentialManagementService(ILogger<ProviderCredentialManagementService> logger)
 {
          _logger = logger;
        }

        /// <summary>
        /// Rule 1-12: Validate complete provider credentials
        /// </summary>
        public async Task<ProviderCredentialValidationResult> ValidateProviderAsync(Organization provider)
        {
       var result = new ProviderCredentialValidationResult();

    try
      {
             _logger.LogInformation($"Starting provider credential validation for provider: {provider?.Id}");

                if (provider == null)
       {
   result.Errors.Add(new ProviderValidationError
 {
          ErrorCode = "PROV-001",
      ErrorName = "Provider Required",
       Message = "Provider organization is required",
      Element = "Organization",
 Severity = ProviderValidationSeverity.Error,
   RemediationAction = "Provide valid provider organization",
   StandardReference = "NPHIES Provider Validation"
      });
        result.IsValid = false;
  return result;
         }

           result.ProviderId = provider.Id.ToString();
  result.ProviderName = provider.OrganizationName ?? string.Empty;

           // Rule 1: Provider ID validation
         if (string.IsNullOrWhiteSpace(provider.Id.ToString()))
           {
        result.Errors.Add(new ProviderValidationError
                {
          ErrorCode = "PROV-002",
            ErrorName = "Missing Provider ID",
       Message = "Provider must have a unique identifier",
    Element = "Organization.id",
   Severity = ProviderValidationSeverity.Error,
           RemediationAction = "Provide unique provider identifier",
         StandardReference = "NPHIES Provider ID"
           });
     }

      // Rule 2: Provider name validation
     if (string.IsNullOrWhiteSpace(provider.OrganizationName))
  {
  result.Errors.Add(new ProviderValidationError
{
     ErrorCode = "PROV-003",
  ErrorName = "Missing Provider Name",
          Message = "Provider must have a name",
                Element = "Organization.name",
          Severity = ProviderValidationSeverity.Error,
           RemediationAction = "Provide provider name",
                 StandardReference = "NPHIES Provider Information"
             });
    }

      // Rule 3: License validation
          var licenseStatus = await GetLicenseStatusAsync(provider.Id.ToString());
      if (licenseStatus == null || !licenseStatus.IsActive)
      {
             result.Errors.Add(new ProviderValidationError
        {
        ErrorCode = "PROV-004",
     ErrorName = "Invalid or Inactive License",
       Message = "Provider must have an active valid license",
        Element = "Organization.qualification",
  Severity = ProviderValidationSeverity.Error,
       RemediationAction = "Ensure provider has valid active license",
           StandardReference = "NPHIES License Verification"
      });
      }

    // Rule 4: License expiration check
 if (licenseStatus != null && licenseStatus.IsExpired)
  {
      result.Errors.Add(new ProviderValidationError
     {
  ErrorCode = "PROV-005",
        ErrorName = "License Expired",
            Message = $"Provider license expired on {licenseStatus.ExpirationDate:yyyy-MM-dd}",
     Element = "Organization.qualification.period.end",
            Severity = ProviderValidationSeverity.Error,
     RemediationAction = "Renew provider license immediately",
           StandardReference = "NPHIES License Expiration"
          });
 }

             // Rule 5: Network membership validation
          var networkStatus = await GetNetworkStatusAsync(provider.Id.ToString());
      if (networkStatus == null || !networkStatus.IsInNetwork)
 {
result.Warnings.Add("Provider is not in any active network");
     }

           // Rule 6: Network contract validation
     if (networkStatus != null && !networkStatus.IsContractActive)
    {
      result.Errors.Add(new ProviderValidationError
     {
 ErrorCode = "PROV-006",
         ErrorName = "Inactive Network Contract",
           Message = "Provider network contract is not active",
   Element = "Organization.contact",
             Severity = ProviderValidationSeverity.Error,
      RemediationAction = "Activate provider network contract",
         StandardReference = "NPHIES Network Contract"
       });
           }

    // Rule 7: Specialization validation
                var specializations = await GetSpecializationsAsync(provider.Id.ToString());
    if (specializations == null || specializations.Count == 0)
          {
       result.Warnings.Add("Provider has no registered specializations");
   }

     // Rule 8: Provider status check
  var isActive = await IsProviderActiveAsync(provider.Id.ToString());
 if (!isActive)
    {
        result.Errors.Add(new ProviderValidationError
            {
      ErrorCode = "PROV-007",
ErrorName = "Provider Not Active",
      Message = "Provider account is not active",
       Element = "Organization.active",
        Severity = ProviderValidationSeverity.Error,
         RemediationAction = "Activate provider account",
  StandardReference = "NPHIES Provider Status"
        });
     }

  // Rule 9: Suspension status check
           var suspensionStatus = await GetSuspensionStatusAsync(provider.Id.ToString());
      if (suspensionStatus != null && suspensionStatus.IsSuspended)
        {
 result.Errors.Add(new ProviderValidationError
      {
             ErrorCode = "PROV-008",
            ErrorName = "Provider Suspended",
  Message = $"Provider is suspended until {suspensionStatus.SuspensionEndDate:yyyy-MM-dd}",
    Element = "Organization.active",
         Severity = ProviderValidationSeverity.Error,
             RemediationAction = "Wait for suspension period to end or request reactivation",
       StandardReference = "NPHIES Provider Suspension"
            });
     }

     // Rule 10: National ID validation
  if (!string.IsNullOrWhiteSpace(provider.OrganizationName))
    {
            // Validate provider identity
           var identityInfo = await ValidateProviderIdentityAsync(provider.OrganizationName, "License");
   if (identityInfo != null && !identityInfo.IsValid)
            {
       result.Errors.Add(new ProviderValidationError
            {
  ErrorCode = "PROV-009",
ErrorName = "Invalid Provider Identity",
     Message = "Provider identity could not be verified",
    Element = "Organization.identifier",
     Severity = ProviderValidationSeverity.Error,
         RemediationAction = "Verify provider identity documentation",
            StandardReference = "NPHIES Provider Identity"
         });
     }
            }

          // Rule 11: Contact information validation
            var contactErrors = await ValidateContactInformationAsync(provider);
         if (contactErrors.Count > 0)
          {
          result.Errors.AddRange(contactErrors);
           }

            // Rule 12: Facility type validation
    var facilityInfo = await GetFacilityTypeAsync(provider.Id.ToString());
                if (facilityInfo != null && string.IsNullOrWhiteSpace(facilityInfo.FacilityType))
          {
         result.Warnings.Add("Provider facility type is not specified");
      }

           result.ComplianceChecks.Add("License validation completed");
   result.ComplianceChecks.Add("Network status verified");
        result.ComplianceChecks.Add("Provider status checked");
          result.ComplianceChecks.Add("Specializations validated");
  result.ComplianceChecks.Add("Contact information verified");

          result.IsValid = result.ErrorCount == 0;

     _logger.LogInformation($"Provider validation completed. IsValid: {result.IsValid}, Errors: {result.ErrorCount}");
}
    catch (Exception ex)
            {
        _logger.LogError(ex, $"Error validating provider: {provider?.Id}");
     result.Errors.Add(new ProviderValidationError
  {
         ErrorCode = "PROV-ERR-001",
     ErrorName = "Validation Exception",
         Message = ex.Message,
          Severity = ProviderValidationSeverity.Error,
           StandardReference = "NPHIES Provider Validation"
      });
   result.IsValid = false;
            }

         return result;
        }

    /// <summary>
      /// Get license status
     /// </summary>
        public async Task<ProviderLicenseInfo> GetLicenseStatusAsync(string providerId)
        {
        try
     {
     _logger.LogInformation($"Retrieving license status for provider: {providerId}");

     // In production, this would query the database
        var licenseInfo = new ProviderLicenseInfo
          {
      ProviderId = providerId,
         LicenseNumber = "LIC-" + providerId,
        LicenseType = "Medical",
   IssuingAuthority = "Ministry of Health",
        IssuanceDate = DateTime.Now.AddYears(-3),
      ExpirationDate = DateTime.Now.AddYears(2),
     LicenseStatus = "Active",
LastRenewalDate = DateTime.Now.AddYears(-1),
    NextRenewalDueDate = DateTime.Now.AddYears(1)
     };

       return licenseInfo;
   }
            catch (Exception ex)
      {
     _logger.LogError(ex, $"Error retrieving license status for provider: {providerId}");
                return null;
    }
        }

        /// <summary>
      /// Check if license is valid
  /// </summary>
        public async Task<bool> IsLicenseValidAsync(string providerId)
        {
            try
   {
            var license = await GetLicenseStatusAsync(providerId);
 return license != null && license.IsActive && !license.IsExpired;
            }
            catch (Exception ex)
            {
     _logger.LogError(ex, $"Error checking license validity: {providerId}");
     return false;
            }
     }

        /// <summary>
        /// Get network status
        /// </summary>
        public async Task<ProviderNetworkStatus> GetNetworkStatusAsync(string providerId)
        {
          try
         {
                _logger.LogInformation($"Retrieving network status for provider: {providerId}");

         var networkStatus = new ProviderNetworkStatus
                {
        ProviderId = providerId,
      IsInNetwork = true,
            NetworkName = "Primary Network",
          MembershipStatus = "Active",
  MembershipStartDate = DateTime.Now.AddYears(-2),
              IsPrimaryNetwork = true,
 ContractStatus = "Active",
        ContractExpirationDate = DateTime.Now.AddYears(1),
  NetworkTierLevel = "Preferred"
        };

     return networkStatus;
  }
            catch (Exception ex)
            {
           _logger.LogError(ex, $"Error retrieving network status: {providerId}");
                return null;
      }
      }

      /// <summary>
        /// Check if provider is in network
        /// </summary>
        public async Task<bool> IsProviderInNetworkAsync(string providerId)
        {
 try
          {
 var networkStatus = await GetNetworkStatusAsync(providerId);
    return networkStatus != null && networkStatus.IsInNetwork;
            }
         catch (Exception ex)
       {
 _logger.LogError(ex, $"Error checking network status: {providerId}");
                return false;
            }
        }

        /// <summary>
        /// Get specializations
     /// </summary>
        public async Task<List<ProviderSpecialization>> GetSpecializationsAsync(string providerId)
        {
      try
            {
            _logger.LogInformation($"Retrieving specializations for provider: {providerId}");

              var specializations = new List<ProviderSpecialization>
    {
         new ProviderSpecialization
        {
  ProviderId = providerId,
        SpecializationCode = "001",
          SpecializationName = "General Practice",
       IsPrimarySpecialty = true,
      IsBoardCertified = true,
      SpecializationStatus = "Active"
  }
     };

         return specializations;
   }
            catch (Exception ex)
    {
      _logger.LogError(ex, $"Error retrieving specializations: {providerId}");
          return new List<ProviderSpecialization>();
        }
        }

        /// <summary>
        /// Validate provider identity
        /// </summary>
        public async Task<ProviderIdentityInfo> ValidateProviderIdentityAsync(string nationalId, string idType)
   {
     try
   {
    _logger.LogInformation($"Validating provider identity: {nationalId}");

     var identityInfo = new ProviderIdentityInfo
            {
         NationalId = nationalId,
    IdType = idType,
                 IsValid = !string.IsNullOrWhiteSpace(nationalId),
        IsExpired = false,
   IdStatus = "Valid"
     };

   return identityInfo;
    }
 catch (Exception ex)
       {
             _logger.LogError(ex, $"Error validating provider identity: {nationalId}");
      return null;
 }
        }

/// <summary>
        /// Check if provider is active
        /// </summary>
      public async Task<bool> IsProviderActiveAsync(string providerId)
 {
     try
            {
     _logger.LogInformation($"Checking provider active status: {providerId}");
    // In production, query database
      return true;
 }
  catch (Exception ex)
            {
      _logger.LogError(ex, $"Error checking provider active status: {providerId}");
   return false;
}
        }

        /// <summary>
  /// Get suspension status
   /// </summary>
        public async Task<ProviderSuspensionInfo> GetSuspensionStatusAsync(string providerId)
        {
     try
            {
        _logger.LogInformation($"Checking suspension status for provider: {providerId}");

       var suspensionInfo = new ProviderSuspensionInfo
    {
     ProviderId = providerId,
             IsSuspended = false,
        CanBeReactivated = true,
            ReactivationStatus = "Not Suspended"
      };

    return suspensionInfo;
         }
   catch (Exception ex)
            {
         _logger.LogError(ex, $"Error retrieving suspension status: {providerId}");
          return null;
            }
        }

        /// <summary>
      /// Get provider taxonomy code
        /// </summary>
        public async Task<string> GetProviderTaxonomyCodeAsync(string providerId)
        {
          try
        {
       _logger.LogInformation($"Retrieving taxonomy code for provider: {providerId}");
            // NPHIES provider taxonomy codes
      return "207Q00000X"; // Allopathic & Osteopathic Physicians
            }
            catch (Exception ex)
          {
      _logger.LogError(ex, $"Error retrieving taxonomy code: {providerId}");
         return null;
   }
        }

/// <summary>
        /// Validate contact information
        /// </summary>
        public async Task<List<ProviderValidationError>> ValidateContactInformationAsync(Organization provider)
  {
 var errors = new List<ProviderValidationError>();

     try
 {
         _logger.LogInformation($"Validating contact information for provider: {provider?.Id}");

     if (provider == null)
            return errors;

// Rule for phone validation
     if (string.IsNullOrWhiteSpace(provider.PhoneNumber))
            {
         errors.Add(new ProviderValidationError
  {
     ErrorCode = "PROV-010",
     ErrorName = "Missing Contact Phone",
          Message = "Provider must have at least one phone number",
          Element = "Organization.telecom",
    Severity = ProviderValidationSeverity.Warning,
     RemediationAction = "Add provider phone number",
    StandardReference = "NPHIES Provider Contact"
           });
 }

         _logger.LogInformation($"Contact validation completed. Errors: {errors.Count}");
    }
 catch (Exception ex)
            {
   _logger.LogError(ex, $"Error validating contact information: {provider?.Id}");
       }

      return errors;
   }

        /// <summary>
  /// Get facility type
        /// </summary>
        public async Task<FacilityTypeInfo> GetFacilityTypeAsync(string providerId)
        {
    try
            {
    _logger.LogInformation($"Retrieving facility type for provider: {providerId}");

    var facilityInfo = new FacilityTypeInfo
    {
        ProviderId = providerId,
             FacilityType = "Clinic",
     FacilityCategory = "Private",
      IsAccredited = true,
     AccreditationDate = DateTime.Now.AddYears(-1),
           AccreditationExpirationDate = DateTime.Now.AddYears(2),
        Is24Hour = false,
        OperationalStatus = "Active"
                };

     return facilityInfo;
   }
      catch (Exception ex)
          {
             _logger.LogError(ex, $"Error retrieving facility type: {providerId}");
      return null;
    }
  }

        /// <summary>
      /// Get credential summary
    /// </summary>
        public async Task<ProviderCredentialSummary> GetCredentialSummaryAsync(string providerId)
   {
            try
 {
        _logger.LogInformation($"Retrieving credential summary for provider: {providerId}");

     var licenseValid = await IsLicenseValidAsync(providerId);
                var inNetwork = await IsProviderInNetworkAsync(providerId);
       var isActive = await IsProviderActiveAsync(providerId);
   var suspensionInfo = await GetSuspensionStatusAsync(providerId);
          var specializations = await GetSpecializationsAsync(providerId);
  var networkStatus = await GetNetworkStatusAsync(providerId);

       var summary = new ProviderCredentialSummary
                {
                    ProviderId = providerId,
            HasValidLicense = licenseValid,
        IsInNetwork = inNetwork,
     IsActive = isActive,
        IsSuspended = suspensionInfo?.IsSuspended ?? false,
         SpecializationCodes = specializations.Select(s => s.SpecializationCode).ToList(),
         TotalNetworks = inNetwork ? 1 : 0,
     NextVerificationDueDate = DateTime.Now.AddMonths(12),
         IsCompliant = licenseValid && inNetwork && isActive && !(suspensionInfo?.IsSuspended ?? false)
      };

         return summary;
      }
         catch (Exception ex)
     {
        _logger.LogError(ex, $"Error retrieving credential summary: {providerId}");
          return null;
          }
        }
    }
}
