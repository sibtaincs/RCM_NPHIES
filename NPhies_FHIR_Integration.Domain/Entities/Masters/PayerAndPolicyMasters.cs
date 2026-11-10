using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// PayerMaster - Insurance companies/payers
/// </summary>
[Table("PayerMaster")]
public class PayerMaster : BaseEntity
{
    [Required]
    [StringLength(100)]
    public string PayerId { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string PayerName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? PayerNameArabic { get; set; }

    [StringLength(50)]
    public string? PayerType { get; set; }

    [StringLength(100)]
    public string? LicenseNumber { get; set; }

    [StringLength(100)]
    public string? NphiesPayerId { get; set; }

    [StringLength(50)]
    public string? NphiesConnectionStatus { get; set; }

    [StringLength(500)]
  public string? NphiesApiEndpoint { get; set; }

    public bool IsNphiesMember { get; set; }

    [StringLength(200)]
    public string? ContactEmail { get; set; }

    [StringLength(50)]
    public string? ContactPhone { get; set; }

    [StringLength(200)]
    public string? Website { get; set; }

    [StringLength(500)]
    public string? AddressLine1 { get; set; }

  [StringLength(500)]
    public string? AddressLine2 { get; set; }

    [StringLength(100)]
    public string? City { get; set; }

    [StringLength(100)]
public string? State { get; set; }

    [StringLength(100)]
    public string? Country { get; set; }

    [StringLength(20)]
    public string? PostalCode { get; set; }

    public DateTime? ContractStartDate { get; set; }

    public DateTime? ContractEndDate { get; set; }

    public bool IsNphiesIntegrated { get; set; }

 [StringLength(500)]
    public string? SupportedClaimTypes { get; set; }

    [StringLength(500)]
    public string? SupportedEligibilityTypes { get; set; }

    [StringLength(50)]
    public string? PaymentCycle { get; set; }

    public int? AverageTurnaroundDays { get; set; }

    public int MaxClaimsPerDay { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaxClaimAmount { get; set; }

    [StringLength(3)]
    public string CurrencyCode { get; set; } = "SAR";

    [StringLength(1000)]
    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    [StringLength(100)]
    public string? CreatedBy { get; set; }

  [StringLength(100)]
    public string? ModifiedBy { get; set; }

    // Navigation
    public virtual ICollection<PayerPolicyMaster> Policies { get; set; } = new List<PayerPolicyMaster>();
}

/// <summary>
/// PayerPolicyMaster - Insurance policies offered by payer
/// </summary>
[Table("PayerPolicyMaster")]
public class PayerPolicyMaster : BaseEntity
{
    [Required]
    [ForeignKey("Payer")]
    public string PayerMasterId { get; set; } = string.Empty;

    [Required]
    [StringLength(100)]
    public string PolicyCode { get; set; } = string.Empty;

    [Required]
    [StringLength(255)]
    public string PolicyName { get; set; } = string.Empty;

    [StringLength(255)]
    public string? PolicyNameArabic { get; set; }

    [StringLength(1000)]
    public string? PolicyDescription { get; set; }

    [StringLength(100)]
    public string? PolicyType { get; set; }

    [StringLength(100)]
    public string? CoverageType { get; set; }

    [StringLength(100)]
    public string? CoverageLevel { get; set; }

    [StringLength(100)]
  public string? NetworkType { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AnnualPremium { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PremiumAmount { get; set; }

    [StringLength(50)]
    public string? PremiumFrequency { get; set; }

    [StringLength(3)]
    public string CurrencyCode { get; set; } = "SAR";

    [Column(TypeName = "decimal(18,2)")]
    public decimal AnnualDeductible { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaxOutOfPocket { get; set; }

    [Column(TypeName = "decimal(18,2)")]
  public decimal? OutOfPocketMax { get; set; }

    [Column(TypeName = "decimal(18,2)")]
public decimal? Copay { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CopaymentAmount { get; set; }

    [Column(TypeName = "decimal(5,2)")]
    public decimal? CoinsurancePercentage { get; set; }

    [Column(TypeName = "decimal(18,2)")]
  public decimal? CoverageLimitPerVisit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? CoverageLimitPerYear { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? PreAuthRequiredForAmount { get; set; }

    public bool RequiresPriorAuth { get; set; }

    [Required]
    public DateTime EffectiveFromDate { get; set; }

    public DateTime? EffectiveToDate { get; set; }

    public DateTime? PolicyStartDate { get; set; }

    public DateTime? PolicyEndDate { get; set; }

    public bool IsPolicyActive { get; set; } = true;

    [StringLength(1000)]
    public string? Notes { get; set; }

    [StringLength(100)]
  public string? CreatedBy { get; set; }

    [StringLength(100)]
    public string? ModifiedBy { get; set; }

    // Navigation
    [ForeignKey("PayerMasterId")]
    public virtual PayerMaster? Payer { get; set; }

    public virtual ICollection<PolicyBenefitCoverage> BenefitCoverages { get; set; } = new List<PolicyBenefitCoverage>();
}

/// <summary>
/// PolicyBenefitCoverage - Benefits covered under each policy
/// </summary>
[Table("PolicyBenefitCoverage")]
public class PolicyBenefitCoverage : BaseEntity
{
 [Required]
    [ForeignKey("Policy")]
    public string PolicyMasterId { get; set; } = string.Empty;

    [ForeignKey("ServiceCode")]
    public string? ServiceCodeMasterId { get; set; }

    [StringLength(100)]
    public string? ServiceCategory { get; set; }

  [StringLength(100)]
 public string? BenefitType { get; set; }

    [Column(TypeName = "decimal(5,2)")]
 public decimal CoveragePercentage { get; set; } = 100;

 [Column(TypeName = "decimal(18,2)")]
    public decimal? MaxCoverageAmount { get; set; }

    public bool RequiresPreAuth { get; set; }

    public bool RequiresReferral { get; set; }

    public int? PreAuthValidityDays { get; set; }

    public int? CoverageLimitPerYear { get; set; }

 public int? CoverageLimitPerLifetime { get; set; }

    public bool IsExcluded { get; set; }

    [StringLength(500)]
    public string? ExclusionReason { get; set; }

    public bool IsWaitingPeriodApplicable { get; set; }

 public int? WaitingPeriodDays { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

[StringLength(100)]
    public string? ModifiedBy { get; set; }

    // Navigation
    [ForeignKey("PolicyMasterId")]
    public virtual PayerPolicyMaster? Policy { get; set; }

    [ForeignKey("ServiceCodeMasterId")]
 public virtual ServiceCodeMaster? ServiceCode { get; set; }
}

/// <summary>
/// ClaimSubmissionRules - Rules for claim submission per payer/policy
/// </summary>
[Table("ClaimSubmissionRules")]
public class ClaimSubmissionRules : BaseEntity
{
    [ForeignKey("Payer")]
    public string? PayerMasterId { get; set; }

    [ForeignKey("Policy")]
  public string? PolicyMasterId { get; set; }

    [Required]
[StringLength(255)]
    public string RuleName { get; set; } = string.Empty;

    [StringLength(100)]
    public string? RuleType { get; set; }

    [StringLength(1000)]
    public string? RuleCondition { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? MaxClaimAmount { get; set; }

    public int? MaxItemsPerClaim { get; set; }

    public bool RequiresInvoice { get; set; }

 public bool RequiresMedicalReport { get; set; }

    public bool RequiresPhotos { get; set; }

    public int? MaxDaysForSubmission { get; set; }

  public bool IsActive { get; set; } = true;

 public int Priority { get; set; } = 100;

  [StringLength(100)]
    public string? CreatedBy { get; set; }

    [StringLength(100)]
  public string? ModifiedBy { get; set; }

 // Navigation
  [ForeignKey("PayerMasterId")]
    public virtual PayerMaster? Payer { get; set; }

    [ForeignKey("PolicyMasterId")]
    public virtual PayerPolicyMaster? Policy { get; set; }
}
