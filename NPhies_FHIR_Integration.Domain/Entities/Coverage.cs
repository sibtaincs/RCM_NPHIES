namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// Coverage entity - represents insurance policy information
/// FHIR Resource: Coverage
/// </summary>
public class Coverage : BaseEntity
{
    /// <summary>
    /// Policy/Insurance plan number
    /// </summary>
 public string PolicyNumber { get; set; } = string.Empty;

    /// <summary>
    /// Member ID under the insurance policy
    /// </summary>
    public string MemberID { get; set; } = string.Empty;

    /// <summary>
    /// Coverage identifier system (e.g., "http://payer.com/memberid")
    /// </summary>
    public string? CoverageIdentifierSystem { get; set; }

 /// <summary>
/// Coverage identifier value (usually same as MemberID)
    /// </summary>
    public string? CoverageIdentifierValue { get; set; }

    /// <summary>
    /// Coverage type code (e.g., "EHCPOL" for employee health policy)
    /// System: http://nphies.sa/terminology/CodeSystem/coverage-type
    /// </summary>
    public string CoverageType { get; set; } = string.Empty;

    /// <summary>
    /// Coverage type system URL
    /// </summary>
    public string? CoverageTypeSystem { get; set; }

    /// <summary>
    /// Insurance plan code (e.g., "51627400")
    /// </summary>
    public string? PlanCode { get; set; }

    /// <summary>
  /// Insurance plan coding system
    /// </summary>
public string? PlanCodeSystem { get; set; }

    /// <summary>
    /// Coverage class information
  /// </summary>
    public string? CoverageClass { get; set; }

    /// <summary>
    /// Coverage status: active, inactive, expired
    /// </summary>
    public string Status { get; set; } = "active";

    /// <summary>
    /// Relationship of subscriber to beneficiary (e.g., "self", "spouse", "child")
    /// System: http://terminology.hl7.org/CodeSystem/subscriber-relationship
    /// </summary>
    public string SubscriberRelationship { get; set; } = "self";

    /// <summary>
    /// Subscriber ID (from Coverage.subscriberId)
    /// </summary>
    public string? SubscriberId { get; set; }

    /// <summary>
    /// Subscriber patient ID (reference to the patient who is the policy subscriber)
    /// May be different from the beneficiary patient (PatientId) in cases where coverage is for dependents
  /// Example: Father is subscriber, child is beneficiary
    /// </summary>
    public string? SubscriberPatientId { get; set; }

    /// <summary>
    /// Dependent indicator (e.g., "1", "2" for dependent number)
    /// </summary>
    public string? Dependent { get; set; }

    /// <summary>
    /// Existing RelationToSubscriber - keeping for backward compatibility
    /// </summary>
    public string RelationToSubscriber { get; set; } = string.Empty;

    /// <summary>
    /// Policy holder organization ID
    /// </summary>
    public string? PolicyHolderOrganizationId { get; set; }

    /// <summary>
    /// MRN of the policy subscriber (if different from patient)
    /// </summary>
    public string SubscriberMRN { get; set; } = string.Empty;

    /// <summary>
 /// Subrogation flag (whether recovery is pursued from responsible parties)
    /// </summary>
    public bool Subrogation { get; set; } = false;

    /// <summary>
    /// Maximum copay amount
    /// </summary>
    public decimal? MaxCopay { get; set; }

    /// <summary>
    /// Maximum copay currency (e.g., "SAR")
    /// </summary>
    public string? MaxCopayCurrency { get; set; }

    /// <summary>
    /// Coinsurance percentage value (e.g., 20 for 20%)
    /// </summary>
    public decimal? CoinsurancePercentValue { get; set; }

    /// <summary>
    /// Coverage class type (e.g., "class", "plan", "group") - primary
    /// </summary>
    public string? CoverageClassType { get; set; }

    /// <summary>
  /// Coverage class value (e.g., "CLASS VIP", "GRH/20265096")
    /// </summary>
public string? CoverageClassValue { get; set; }

    // Foreign Keys
    /// <summary>
    /// Reference to the patient (member) ID
    /// </summary>
    public string PatientId { get; set; } = string.Empty;

    /// <summary>
    /// Reference to the insurer organization ID
    /// </summary>
    public string InsurerId { get; set; } = string.Empty;

    // Navigation Properties
    /// <summary>
    /// The patient covered by this policy
    /// </summary>
    public Patient? Patient { get; set; }

    /// <summary>
    /// The patient who is the policy subscriber (policy holder)
    /// May be the same as Patient (when SubscriberRelationship = "self")
    /// or different (when coverage is for spouse, child, or other dependent)
    /// Per NPHIES FHIR IG: Coverage.subscriber references the Patient who holds the policy
    /// </summary>
    public Patient? SubscriberPatient { get; set; }

    /// <summary>
    /// The insurance company providing this coverage
    /// </summary>
    public Organization? Insurer { get; set; }

    // Coverage Date Range
    /// <summary>
    /// Coverage start date
    /// </summary>
    public DateTime CoverageStartDate { get; set; }

    /// <summary>
    /// Coverage end date
    /// </summary>
    public DateTime CoverageEndDate { get; set; }

    // Financial Information
    /// <summary>
    /// Annual deductible amount in currency units
    /// </summary>
    public decimal AnnualDeductible { get; set; }

    /// <summary>
    /// Amount of deductible already met
    /// </summary>
    public decimal DeductibleMet { get; set; }

    /// <summary>
    /// Co-payment amount required per visit
    /// </summary>
    public decimal Copay { get; set; }

    /// <summary>
    /// Coinsurance percentage (e.g., 20 for 20%)
    /// </summary>
    public decimal CoinsurancePercent { get; set; }

    /// <summary>
    /// Out-of-pocket maximum for the year
    /// </summary>
    public decimal OutOfPocketMax { get; set; }

    // Navigation Properties for Related Requests
    /// <summary>
    /// Collection of eligibility requests for this coverage
    /// </summary>
    public ICollection<CoverageEligibilityRequest> EligibilityRequests { get; set; } = new List<CoverageEligibilityRequest>();

  /// <summary>
    /// Collection of claims under this coverage
    /// </summary>
    public ICollection<Claim> Claims { get; set; } = new List<Claim>();

    /// <summary>
    /// Check if coverage is active on a specific date
    /// </summary>
    public bool IsActiveOn(DateTime date)
  {
        return Status == "active" && date >= CoverageStartDate && date <= CoverageEndDate;
    }

    /// <summary>
    /// Calculate remaining deductible
    /// </summary>
    public decimal GetRemainingDeductible()
 {
        return Math.Max(0, AnnualDeductible - DeductibleMet);
    }

    /// <summary>
    /// Check if annual deductible has been met
    /// </summary>
    public bool IsDeductibleMet()
    {
return DeductibleMet >= AnnualDeductible;
    }
}
