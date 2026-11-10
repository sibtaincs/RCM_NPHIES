namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// CoverageEligibilityResponse entity - represents a response to an eligibility check request
/// FHIR Resource: CoverageEligibilityResponse
/// </summary>
public class CoverageEligibilityResponse : BaseEntity
{
    /// <summary>
    /// Unique response identifier (UUID)
 /// Derived from FHIR Bundle ID
    /// </summary>
    public string ResponseUUID { get; set; } = string.Empty;

 /// <summary>
    /// Request identifier that this response is for
    /// </summary>
    public string RequestId { get; set; } = string.Empty;

    /// <summary>
    /// Request identifier system (e.g., "http://pr-fhir.com.sa/CoverageEligibilityRequest")
    /// </summary>
    public string? RequestIdentifierSystem { get; set; }

    /// <summary>
    /// Request identifier value
    /// </summary>
    public string? RequestIdentifierValue { get; set; }

    /// <summary>
    /// Response identifier system (e.g., from MessageHeader response)
    /// </summary>
    public string? ResponseIdentifierSystem { get; set; }

    /// <summary>
    /// Response identifier value (from MessageHeader response)
    /// </summary>
    public string? ResponseIdentifierValue { get; set; }

    // Reference to Request
    /// <summary>
    /// Reference to the eligibility request ID
    /// </summary>
    public string EligibilityRequestId { get; set; } = string.Empty;

    /// <summary>
    /// The eligibility request this is a response to (1:1 relationship)
    /// </summary>
 public CoverageEligibilityRequest? EligibilityRequest { get; set; }

    // Message Tracking
    /// <summary>
    /// Reference to message header for this response
 /// </summary>
    public string MessageHeaderId { get; set; } = string.Empty;

    /// <summary>
    /// The message header for this response
    /// </summary>
    public MessageHeader? MessageHeader { get; set; }

  // Response Status
    /// <summary>
  /// Response status: "active" (resource is active)
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// Response outcome: "complete" (success), "error", "partial"
    /// System: http://hl7.org/fhir/remittance-outcome
    /// </summary>
  public string Outcome { get; set; } = "complete";

    /// <summary>
    /// Processing status after receiving response
    /// </summary>
    public string ProcessingStatus { get; set; } = string.Empty;

    /// <summary>
    /// Response purpose (e.g., "validation", "benefits")
    /// </summary>
    public string? ResponsePurpose { get; set; }

    /// <summary>
    /// Disposition text explaining the determination (e.g., "Eligible subject to...")
    /// </summary>
    public string? Disposition { get; set; }

    /// <summary>
    /// Site eligibility code (e.g., "eligible", "ineligible")
/// </summary>
    public string? SiteEligibility { get; set; }

    /// <summary>
    /// Site eligibility system (e.g., "http://nphies.sa/terminology/CodeSystem/siteEligibility")
    /// </summary>
    public string? SiteEligibilitySystem { get; set; }

    // Response Dates
 /// <summary>
    /// When the response was created (ISO 8601 format)
    /// </summary>
    public DateTime ResponseCreatedAt { get; set; }

    /// <summary>
/// When the response was received in our system
    /// </summary>
    public DateTime ResponseReceivedAt { get; set; }

    /// <summary>
    /// Serviced date from response
    /// </summary>
    public DateTime? ServicedDate { get; set; }

    /// <summary>
    /// Benefit period start date (e.g., "2025-06-30T00:00:00+03:00")
    /// </summary>
    public DateTime? BenefitPeriodStart { get; set; }

    /// <summary>
    /// Benefit period end date (e.g., "2026-06-30T00:00:00+03:00")
    /// </summary>
    public DateTime? BenefitPeriodEnd { get; set; }

    // Coverage Status Information
    /// <summary>
    /// Eligibility status: "active", "inactive", "pending"
    /// </summary>
    public string EligibilityStatus { get; set; } = string.Empty;

    /// <summary>
    /// Whether the coverage is in force (active) on the service date
    /// </summary>
    public bool IsInForce { get; set; }

    // Service Period
    /// <summary>
    /// Service period start date from request
    /// </summary>
    public DateTime ServicedPeriodStart { get; set; }

    /// <summary>
 /// Service period end date from request
    /// </summary>
    public DateTime ServicedPeriodEnd { get; set; }

    // Organization References
    /// <summary>
    /// Reference to insurer organization ID
    /// </summary>
    public string InsurerId { get; set; } = string.Empty;

    /// <summary>
    /// The insurance company providing the response
    /// </summary>
    public Organization? Insurer { get; set; }

    /// <summary>
    /// Reference to patient ID
    /// </summary>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>
    /// The patient for whom coverage is being reported (echoed from request)
    /// </summary>
    public Patient? Patient { get; set; }

    /// <summary>
 /// Reference to coverage ID
 /// </summary>
    public string CoverageId { get; set; } = string.Empty;

    /// <summary>
    /// The coverage being reported
 /// </summary>
    public Coverage? Coverage { get; set; }

  // Network Status
    /// <summary>
    /// Whether provider is in-network
    /// </summary>
    public bool IsInNetwork { get; set; }

    /// <summary>
    /// Network status: "in-network", "out-of-network", "unknown"
    /// </summary>
    public string NetworkStatus { get; set; } = "unknown";

    /// <summary>
    /// Network name or identifier
  /// </summary>
    public string NetworkName { get; set; } = string.Empty;

    // Services Information
    /// <summary>
    /// JSON array of covered service types
    /// </summary>
    public string CoveredServicesJson { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of excluded service types
    /// </summary>
    public string ExcludedServicesJson { get; set; } = string.Empty;

    /// <summary>
    /// JSON array of service limitations
    /// </summary>
    public string LimitationsJson { get; set; } = string.Empty;

    // Benefits Information
    /// <summary>
    /// Collection of benefit balances/categories
    /// </summary>
    public ICollection<BenefitBalance> BenefitBalances { get; set; } = new List<BenefitBalance>();

    // Error Information
 /// <summary>
    /// Collection of errors if response outcome is "error"
    /// </summary>
    public ICollection<EligibilityError> Errors { get; set; } = new List<EligibilityError>();

    // Raw FHIR Storage
    /// <summary>
    /// Complete FHIR response bundle JSON (for audit/compliance)
    /// </summary>
    public string FhirResponseContent { get; set; } = string.Empty;

    /// <summary>
    /// Explanation of benefits text
    /// </summary>
    public string ExplanationOfBenefits { get; set; } = string.Empty;

    // Methods
    /// <summary>
    /// Check if response is successful (outcome complete and no critical errors)
    /// </summary>
    public bool IsSuccessful => Outcome == "complete" && !Errors.Any(e => e.IsCritical);

    /// <summary>
    /// Check if response has errors
    /// </summary>
    public bool HasErrors => Errors.Count > 0;

    /// <summary>
  /// Check if response has critical errors
    /// </summary>
    public bool HasCriticalErrors => Errors.Any(e => e.IsCritical);

    /// <summary>
    /// Get list of covered services
    /// </summary>
    public List<string> GetCoveredServices()
    {
   try
        {
      return System.Text.Json.JsonSerializer.Deserialize<List<string>>(CoveredServicesJson) ?? new List<string>();
        }
        catch
        {
   return new List<string>();
      }
    }

    /// <summary>
    /// Get list of excluded services
    /// </summary>
    public List<string> GetExcludedServices()
    {
        try
  {
     return System.Text.Json.JsonSerializer.Deserialize<List<string>>(ExcludedServicesJson) ?? new List<string>();
        }
        catch
     {
            return new List<string>();
      }
    }

    /// <summary>
    /// Get list of limitations
    /// </summary>
    public List<string> GetLimitations()
    {
        try
        {
   return System.Text.Json.JsonSerializer.Deserialize<List<string>>(LimitationsJson) ?? new List<string>();
        }
  catch
   {
            return new List<string>();
        }
    }

    /// <summary>
    /// Get total annual deductible across all benefit categories
    /// </summary>
    public decimal GetTotalAnnualDeductible()
    {
   var deductibleBenefits = BenefitBalances
    .SelectMany(b => b.Benefits)
   .Where(b => b.BenefitType == "deductible");
      
        return deductibleBenefits.Sum(b => b.AllowedAmount);
    }

 /// <summary>
 /// Get remaining annual deductible
    /// </summary>
    public decimal GetRemainingDeductible()
    {
  var deductibleBenefits = BenefitBalances
            .SelectMany(b => b.Benefits)
 .Where(b => b.BenefitType == "deductible");
            
        return deductibleBenefits.Sum(b => b.GetRemainingAmount());
    }

    /// <summary>
    /// Check if specific service category is covered
    /// </summary>
public bool IsCategoryActive(string category)
    {
        return BenefitBalances.Any(b => b.Category == category);
    }

    /// <summary>
    /// Get benefit balance for a specific category
    /// </summary>
    public BenefitBalance? GetBenefitBalance(string category)
    {
        return BenefitBalances.FirstOrDefault(b => b.Category == category);
    }
}
