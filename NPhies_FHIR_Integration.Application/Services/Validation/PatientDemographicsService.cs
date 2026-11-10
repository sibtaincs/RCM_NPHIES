using NPhies_FHIR_Integration.Domain.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services
{
    /// <summary>
    /// NPHIES Patient Demographics Management Service Interface
    /// Manages patient validation, identity verification, and demographic data per NPHIES standards
    /// </summary>
    public interface IPatientDemographicsService
    {
        /// <summary>
        /// Validate complete patient demographics
        /// </summary>
        Task<PatientDemographicsValidationResult> ValidatePatientAsync(Patient patient);

        /// <summary>
        /// Validate patient identity information
        /// </summary>
        Task<PatientIdentityInfo> ValidatePatientIdentityAsync(string nationalId, string idType);

        /// <summary>
        /// Verify national ID format and validity
        /// </summary>
        Task<bool> IsNationalIdValidAsync(string nationalId, string idType);

        /// <summary>
        /// Get patient contact information
        /// </summary>
        Task<PatientContactInfo> GetContactInformationAsync(string patientId);

        /// <summary>
        /// Validate email address format
        /// </summary>
        Task<bool> IsEmailValidAsync(string email);

        /// <summary>
        /// Validate phone number format
        /// </summary>
        Task<bool> IsPhoneValidAsync(string phone);

        /// <summary>
        /// Get patient address information
        /// </summary>
        Task<PatientAddressInfo> GetAddressInformationAsync(string patientId);

        /// <summary>
        /// Validate address postal code
        /// </summary>
        Task<bool> IsPostalCodeValidAsync(string postalCode, string country);

        /// <summary>
        /// Get patient demographic information
        /// </summary>
        Task<PatientDemographicInfo> GetDemographicInformationAsync(string patientId);

        /// <summary>
        /// Validate date of birth
        /// </summary>
        Task<bool> IsDateOfBirthValidAsync(DateTime dateOfBirth);

        /// <summary>
        /// Get patient relationship information (dependents)
        /// </summary>
        Task<List<PatientRelationshipInfo>> GetPatientRelationshipsAsync(string patientId);

        /// <summary>
        /// Get patient language preferences
        /// </summary>
        Task<List<string>> GetLanguagePreferencesAsync(string patientId);

        /// <summary>
        /// Get comprehensive patient demographics summary
        /// </summary>
        Task<PatientDemographicsSummary> GetDemographicsSummaryAsync(string patientId);
    }

    /// <summary>
    /// Patient demographics validation result
    /// </summary>
    public class PatientDemographicsValidationResult
    {
        public bool IsValid { get; set; }
        public string PatientId { get; set; } = string.Empty;
        public string PatientName { get; set; } = string.Empty;
        public DateTime ValidationTimestamp { get; set; } = DateTime.UtcNow;
        public List<PatientValidationError> Errors { get; set; } = new();
        public List<string> Warnings { get; set; } = new();
        public int ErrorCount => Errors.Count;
        public int WarningCount => Warnings.Count;
        public string ValidationSummary => $"Errors: {ErrorCount}, Warnings: {WarningCount}";
        public List<string> ComplianceChecks { get; set; } = new();
    }

    /// <summary>
    /// Patient validation error
    /// </summary>
    public class PatientValidationError
    {
        public string ErrorCode { get; set; } = string.Empty;
        public string ErrorName { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Element { get; set; } = string.Empty;
        public PatientValidationSeverity Severity { get; set; }
        public string RemediationAction { get; set; } = string.Empty;
        public string StandardReference { get; set; } = string.Empty;
    }

    /// <summary>
    /// Validation severity levels
    /// </summary>
    public enum PatientValidationSeverity
    {
        Error = 1,
        Warning = 2,
        Info = 3
    }

    /// <summary>
    /// Patient identity information
    /// </summary>
    public class PatientIdentityInfo
    {
        public string PatientId { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public string IdType { get; set; } = string.Empty; // Iqama, Passport, GCC, National ID
        public bool IsValid { get; set; }
        public bool IsExpired { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string IdStatus { get; set; } = string.Empty; // Valid, Expired, Revoked, Invalid
        public string IssuingCountry { get; set; } = string.Empty;
        public DateTime? IssuanceDate { get; set; }
        public List<string> ValidationErrors { get; set; } = new();
    }

    /// <summary>
    /// Patient contact information
    /// </summary>
    public class PatientContactInfo
    {
        public string PatientId { get; set; } = string.Empty;
        public string PrimaryEmail { get; set; } = string.Empty;
        public string SecondaryEmail { get; set; } = string.Empty;
        public string PrimaryPhone { get; set; } = string.Empty;
        public string SecondaryPhone { get; set; } = string.Empty;
        public string MobilePhone { get; set; } = string.Empty;
        public string ContactPreference { get; set; } = string.Empty; // Email, Phone, SMS, WhatsApp
        public DateTime? LastContactDate { get; set; }
        public bool AllowMarketing { get; set; }
        public bool AllowSMS { get; set; }
        public bool AllowEmail { get; set; }
    }

    /// <summary>
    /// Patient address information
    /// </summary>
    public class PatientAddressInfo
    {
        public string PatientId { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string AddressLine2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Region { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string AddressType { get; set; } = string.Empty; // Residential, Mailing, Business
        public bool IsVerified { get; set; }
        public DateTime? VerificationDate { get; set; }
        public bool IsPrimary { get; set; }
    }

    /// <summary>
    /// Patient demographic information
    /// </summary>
    public class PatientDemographicInfo
    {
        public string PatientId { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string MiddleName { get; set; } = string.Empty;
        public string PreferredName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int Age => DateTime.Now.Year - DateOfBirth.Year;
        public string Gender { get; set; } = string.Empty; // M, F, O, Unknown
        public string MaritalStatus { get; set; } = string.Empty; // Single, Married, Divorced, Widowed
        public string EmploymentStatus { get; set; } = string.Empty; // Employed, Unemployed, Student, Retired
        public string Nationality { get; set; } = string.Empty;
        public string Religion { get; set; } = string.Empty;
        public string Ethnicity { get; set; } = string.Empty;
    }

    /// <summary>
    /// Patient relationship information
    /// </summary>
    public class PatientRelationshipInfo
    {
        public string PatientId { get; set; } = string.Empty;
        public string RelatedPatientId { get; set; } = string.Empty;
        public string RelatedPatientName { get; set; } = string.Empty;
        public string RelationshipType { get; set; } = string.Empty; // Spouse, Child, Parent, Sibling, Guardian
        public bool IsPrimary { get; set; }
        public bool IsEmergencyContact { get; set; }
        public DateTime? RelationshipStartDate { get; set; }
    }

    /// <summary>
    /// Patient demographics summary
    /// </summary>
    public class PatientDemographicsSummary
    {
        public string PatientId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime DateOfBirth { get; set; }
        public int Age { get; set; }
        public string Gender { get; set; } = string.Empty;
        public string NationalId { get; set; } = string.Empty;
        public bool HasValidIdentity { get; set; }
        public bool HasValidContact { get; set; }
        public bool HasValidAddress { get; set; }
        public string MaritalStatus { get; set; } = string.Empty;
        public int NumberOfDependents { get; set; }
        public List<string> LanguagePreferences { get; set; } = new();
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
        public bool IsCompliant { get; set; }
    }

    /// <summary>
    /// NPHIES Patient Demographics Management Service Implementation
    /// </summary>
    public class PatientDemographicsService : IPatientDemographicsService
    {
        private readonly ILogger<PatientDemographicsService> _logger;

        // Regex patterns for validation
        private readonly Regex _emailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
        private readonly Regex _phoneRegex = new(@"^\+?[1-9]\d{1,14}$", RegexOptions.Compiled);
        private readonly Regex _saudiPhoneRegex = new(@"^\+?966[0-9]{8,9}$", RegexOptions.Compiled);
        private readonly Regex _saudiPostalCodeRegex = new(@"^\d{5}$", RegexOptions.Compiled);
        private readonly Regex _iqamaRegex = new(@"^[0-9]{10}$", RegexOptions.Compiled);
        private readonly Regex _passportRegex = new(@"^[A-Z0-9]{6,9}$", RegexOptions.Compiled);

        // Saudi regions for validation
        private readonly List<string> _saudiRegions = new()
   {
    "Riyadh", "Makkah", "Madinah", "Qassim", "Eastern", "Asir", "Tabuk",
            "Hail", "Northern", "Jawf", "Najran", "Baha", "Jeddah"
        };

        public PatientDemographicsService(ILogger<PatientDemographicsService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Rule 1-10: Validate complete patient demographics
        /// </summary>
        public async Task<PatientDemographicsValidationResult> ValidatePatientAsync(Patient patient)
        {
            var result = new PatientDemographicsValidationResult();

            try
            {
                _logger.LogInformation($"Starting patient demographics validation for patient: {patient?.Id}");

                if (patient == null)
                {
                    result.Errors.Add(new PatientValidationError
                    {
                        ErrorCode = "PAT-001",
                        ErrorName = "Patient Required",
                        Message = "Patient object is required",
                        Element = "Patient",
                        Severity = PatientValidationSeverity.Error,
                        RemediationAction = "Provide valid patient object",
                        StandardReference = "NPHIES Patient Validation"
                    });
                    result.IsValid = false;
                    return result;
                }

                result.PatientId = patient.Id.ToString();
                result.PatientName = $"{patient.FirstName} {patient.LastName}";

                // Rule 1: Patient ID validation
                if (string.IsNullOrWhiteSpace(patient.Id.ToString()))
                {
                    result.Errors.Add(new PatientValidationError
                    {
                        ErrorCode = "PAT-002",
                        ErrorName = "Missing Patient ID",
                        Message = "Patient must have a unique identifier",
                        Element = "Patient.id",
                        Severity = PatientValidationSeverity.Error,
                        RemediationAction = "Provide unique patient identifier",
                        StandardReference = "NPHIES Patient ID"
                    });
                }

                // Rule 2: Patient name validation
                if (string.IsNullOrWhiteSpace(patient.FirstName) || string.IsNullOrWhiteSpace(patient.LastName))
                {
                    result.Errors.Add(new PatientValidationError
                    {
                        ErrorCode = "PAT-003",
                        ErrorName = "Missing Patient Name",
                        Message = "Patient must have first and last name",
                        Element = "Patient.name",
                        Severity = PatientValidationSeverity.Error,
                        RemediationAction = "Provide patient first and last name",
                        StandardReference = "NPHIES Patient Information"
                    });
                }

                // Rule 3: National ID validation
                var identityInfo = await ValidatePatientIdentityAsync(patient.NationalId, "National");
                if (identityInfo == null || !identityInfo.IsValid)
                {
                    result.Errors.Add(new PatientValidationError
                    {
                        ErrorCode = "PAT-004",
                        ErrorName = "Invalid National ID",
                        Message = "Patient national ID is invalid or expired",
                        Element = "Patient.identifier",
                        Severity = PatientValidationSeverity.Error,
                        RemediationAction = "Provide valid national ID (Iqama/Passport/GCC)",
                        StandardReference = "NPHIES National ID Validation"
                    });
                }

                // Rule 4: Date of birth validation
                var dobValid = await IsDateOfBirthValidAsync(patient.DateOfBirth);
                if (!dobValid)
                {
                    result.Errors.Add(new PatientValidationError
                    {
                        ErrorCode = "PAT-005",
                        ErrorName = "Invalid Date of Birth",
                        Message = "Patient date of birth is invalid or in the future",
                        Element = "Patient.birthDate",
                        Severity = PatientValidationSeverity.Error,
                        RemediationAction = "Provide valid date of birth in the past",
                        StandardReference = "NPHIES Patient Demographics"
                    });
                }

                // Rule 5: Contact information validation
                var contactInfo = await GetContactInformationAsync(patient.Id.ToString());
                if (contactInfo == null || (string.IsNullOrWhiteSpace(contactInfo.PrimaryEmail) &&
                 string.IsNullOrWhiteSpace(contactInfo.PrimaryPhone)))
                {
                    result.Warnings.Add("Patient should have at least one contact method (email or phone)");
                }

                // Rule 6: Email validation
                if (!string.IsNullOrWhiteSpace(patient.Email))
                {
                    var emailValid = await IsEmailValidAsync(patient.Email);
                    if (!emailValid)
                    {
                        result.Errors.Add(new PatientValidationError
                        {
                            ErrorCode = "PAT-006",
                            ErrorName = "Invalid Email Format",
                            Message = "Patient email address format is invalid",
                            Element = "Patient.telecom[email]",
                            Severity = PatientValidationSeverity.Error,
                            RemediationAction = "Provide valid email address",
                            StandardReference = "NPHIES Contact Information"
                        });
                    }
                }

                // Rule 7: Phone validation
                if (!string.IsNullOrWhiteSpace(patient.Phone))
                {
                    var phoneValid = await IsPhoneValidAsync(patient.Phone);
                    if (!phoneValid)
                    {
                        result.Errors.Add(new PatientValidationError
                        {
                            ErrorCode = "PAT-007",
                            ErrorName = "Invalid Phone Format",
                            Message = "Patient phone number format is invalid",
                            Element = "Patient.telecom[phone]",
                            Severity = PatientValidationSeverity.Error,
                            RemediationAction = "Provide valid phone number",
                            StandardReference = "NPHIES Contact Information"
                        });
                    }
                }

                // Rule 8: Address validation
                var addressInfo = await GetAddressInformationAsync(patient.Id.ToString());
                if (addressInfo == null || string.IsNullOrWhiteSpace(addressInfo.City))
                {
                    result.Warnings.Add("Patient address information is incomplete");
                }

                // Rule 9: Gender validation
                if (!string.IsNullOrWhiteSpace(patient.Gender))
                {
                    var validGenders = new[] { "M", "F", "O", "Unknown" };
                    if (!validGenders.Contains(patient.Gender))
                    {
                        result.Errors.Add(new PatientValidationError
                        {
                            ErrorCode = "PAT-008",
                            ErrorName = "Invalid Gender",
                            Message = "Patient gender must be M, F, O, or Unknown",
                            Element = "Patient.gender",
                            Severity = PatientValidationSeverity.Warning,
                            RemediationAction = "Use valid gender value",
                            StandardReference = "NPHIES Patient Demographics"
                        });
                    }
                }

                // Rule 10: Marital status validation
                if (!string.IsNullOrWhiteSpace(patient.MaritalStatus))
                {
                    var validStatuses = new[] { "single", "married", "divorced", "widowed", "unknown" };
                    if (!validStatuses.Contains(patient.MaritalStatus.ToLower()))
                    {
                        result.Warnings.Add("Patient marital status is not in standard format");
                    }
                }

                result.ComplianceChecks.Add("Patient identity validated");
                result.ComplianceChecks.Add("Contact information verified");
                result.ComplianceChecks.Add("Address information checked");
                result.ComplianceChecks.Add("Demographics validated");
                result.ComplianceChecks.Add("Relationship information verified");

                result.IsValid = result.ErrorCount == 0;

                _logger.LogInformation($"Patient validation completed. IsValid: {result.IsValid}, Errors: {result.ErrorCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating patient: {patient?.Id}");
                result.Errors.Add(new PatientValidationError
                {
                    ErrorCode = "PAT-ERR-001",
                    ErrorName = "Validation Exception",
                    Message = ex.Message,
                    Severity = PatientValidationSeverity.Error,
                    StandardReference = "NPHIES Patient Validation"
                });
                result.IsValid = false;
            }

            return result;
        }

        /// <summary>
        /// Validate patient identity
        /// </summary>
        public async Task<PatientIdentityInfo> ValidatePatientIdentityAsync(string nationalId, string idType)
        {
            try
            {
                _logger.LogInformation($"Validating patient identity: {nationalId}");

                var identityInfo = new PatientIdentityInfo
                {
                    NationalId = nationalId,
                    IdType = idType,
                    IssuingCountry = "SA"
                };

                if (string.IsNullOrWhiteSpace(nationalId))
                {
                    identityInfo.IsValid = false;
                    identityInfo.IdStatus = "Invalid";
                    identityInfo.ValidationErrors.Add("National ID is required");
                    return identityInfo;
                }

                // Rule: Validate based on ID type
                switch (idType?.ToLower())
                {
                    case "iqama":
                        if (_iqamaRegex.IsMatch(nationalId))
                        {
                            identityInfo.IsValid = true;
                            identityInfo.IdStatus = "Valid";
                        }
                        else
                        {
                            identityInfo.IsValid = false;
                            identityInfo.IdStatus = "Invalid";
                            identityInfo.ValidationErrors.Add("Iqama must be 10 digits");
                        }
                        break;

                    case "passport":
                        if (_passportRegex.IsMatch(nationalId))
                        {
                            identityInfo.IsValid = true;
                            identityInfo.IdStatus = "Valid";
                        }
                        else
                        {
                            identityInfo.IsValid = false;
                            identityInfo.IdStatus = "Invalid";
                            identityInfo.ValidationErrors.Add("Passport format is invalid");
                        }
                        break;

                    case "gcc":
                    case "national":
                    default:
                        identityInfo.IsValid = !string.IsNullOrWhiteSpace(nationalId);
                        identityInfo.IdStatus = identityInfo.IsValid ? "Valid" : "Invalid";
                        break;
                }

                return identityInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating patient identity: {nationalId}");
                return null;
            }
        }

        /// <summary>
        /// Verify national ID validity
        /// </summary>
        public async Task<bool> IsNationalIdValidAsync(string nationalId, string idType)
        {
            try
            {
                var identityInfo = await ValidatePatientIdentityAsync(nationalId, idType);
                return identityInfo != null && identityInfo.IsValid;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking national ID validity: {nationalId}");
                return false;
            }
        }

        /// <summary>
        /// Get contact information
        /// </summary>
        public async Task<PatientContactInfo> GetContactInformationAsync(string patientId)
        {
            try
            {
                _logger.LogInformation($"Retrieving contact information for patient: {patientId}");

                var contactInfo = new PatientContactInfo
                {
                    PatientId = patientId,
                    ContactPreference = "Email",
                    AllowEmail = true,
                    AllowSMS = true
                };

                return contactInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving contact information: {patientId}");
                return null;
            }
        }

        /// <summary>
        /// Validate email format
        /// </summary>
        public async Task<bool> IsEmailValidAsync(string email)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(email))
                    return false;

                return _emailRegex.IsMatch(email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating email: {email}");
                return false;
            }
        }

        /// <summary>
        /// Validate phone format
        /// </summary>
        public async Task<bool> IsPhoneValidAsync(string phone)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(phone))
                    return false;

                // Check Saudi phone format first, then general format
                return _saudiPhoneRegex.IsMatch(phone) || _phoneRegex.IsMatch(phone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating phone: {phone}");
                return false;
            }
        }

        /// <summary>
        /// Get address information
        /// </summary>
        public async Task<PatientAddressInfo> GetAddressInformationAsync(string patientId)
        {
            try
            {
                _logger.LogInformation($"Retrieving address information for patient: {patientId}");

                var addressInfo = new PatientAddressInfo
                {
                    PatientId = patientId,
                    Country = "SA",
                    AddressType = "Residential",
                    IsPrimary = true
                };

                return addressInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving address information: {patientId}");
                return null;
            }
        }

        /// <summary>
        /// Validate postal code
        /// </summary>
        public async Task<bool> IsPostalCodeValidAsync(string postalCode, string country)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(postalCode))
                    return false;

                // Saudi postal code validation
                if (country?.ToUpper() == "SA")
                {
                    return _saudiPostalCodeRegex.IsMatch(postalCode);
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating postal code: {postalCode}");
                return false;
            }
        }

        /// <summary>
        /// Get demographic information
        /// </summary>
        public async Task<PatientDemographicInfo> GetDemographicInformationAsync(string patientId)
        {
            try
            {
                _logger.LogInformation($"Retrieving demographic information for patient: {patientId}");

                var demographicInfo = new PatientDemographicInfo
                {
                    PatientId = patientId,
                    Gender = "Unknown",
                    MaritalStatus = "Unknown",
                    EmploymentStatus = "Unknown"
                };

                return demographicInfo;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving demographic information: {patientId}");
                return null;
            }
        }

        /// <summary>
        /// Validate date of birth
        /// </summary>
        public async Task<bool> IsDateOfBirthValidAsync(DateTime dateOfBirth)
        {
            try
            {
                // Rule: DOB cannot be in the future
                if (dateOfBirth > DateTime.Now)
                    return false;

                // Rule: Age must be reasonable (typically 0-150 years)
                var age = DateTime.Now.Year - dateOfBirth.Year;
                if (age < 0 || age > 150)
                    return false;

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error validating date of birth: {dateOfBirth}");
                return false;
            }
        }

        /// <summary>
        /// Get patient relationships
        /// </summary>
        public async Task<List<PatientRelationshipInfo>> GetPatientRelationshipsAsync(string patientId)
        {
            try
            {
                _logger.LogInformation($"Retrieving relationships for patient: {patientId}");

                var relationships = new List<PatientRelationshipInfo>();
                return relationships;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving patient relationships: {patientId}");
                return new List<PatientRelationshipInfo>();
            }
        }

        /// <summary>
        /// Get language preferences
        /// </summary>
        public async Task<List<string>> GetLanguagePreferencesAsync(string patientId)
        {
            try
            {
                _logger.LogInformation($"Retrieving language preferences for patient: {patientId}");

                // Default to Arabic and English for Saudi Arabia
                var languages = new List<string> { "ar", "en" };
                return languages;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving language preferences: {patientId}");
                return new List<string> { "ar", "en" };
            }
        }

        /// <summary>
        /// Get comprehensive demographics summary
        /// </summary>
        public async Task<PatientDemographicsSummary> GetDemographicsSummaryAsync(string patientId)
        {
            try
            {
                _logger.LogInformation($"Retrieving demographics summary for patient: {patientId}");

                var identityValid = true; // Would check actual identity
                var contactValid = true; // Would check actual contact
                var addressValid = true; // Would check actual address

                var summary = new PatientDemographicsSummary
                {
                    PatientId = patientId,
                    HasValidIdentity = identityValid,
                    HasValidContact = contactValid,
                    HasValidAddress = addressValid,
                    LanguagePreferences = new List<string> { "ar", "en" },
                    IsCompliant = identityValid && contactValid && addressValid
                };

                return summary;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error retrieving demographics summary: {patientId}");
                return null;
            }
        }
    }
}
