using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPhies_FHIR_Integration.Domain.Entities
{
    /// <summary>
    /// Tracks benefit calculations and determinations for claims
    /// </summary>
    [Table("BenefitCalculation")]
    public class BenefitCalculationEntity : BaseEntity
    {
    [Key]
        public int Id { get; set; }

        /// <summary>
        /// Reference to claim being processed
        /// </summary>
        [Required]
        [ForeignKey("Claim")]
        public int ClaimId { get; set; }

  /// <summary>
        /// Reference to claim item (if item-level calculation)
        /// </summary>
        [ForeignKey("ClaimItem")]
        public int? ClaimItemId { get; set; }

 /// <summary>
        /// Reference to coverage/benefit plan
      /// </summary>
        [Required]
        [ForeignKey("Coverage")]
  public int CoverageId { get; set; }

        /// <summary>
        /// Benefit type
        /// Values: inpatient, outpatient, emergency, diagnostic, durable-medical-equipment, pharmacy
        /// </summary>
    [Required]
        [StringLength(50)]
  public string BenefitType { get; set; }

        /// <summary>
 /// Submitted amount from claim
        /// </summary>
[Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal SubmittedAmount { get; set; }

        /// <summary>
        /// Allowed/Eligible amount per fee schedule
        /// </summary>
        [Required]
      [Column(TypeName = "decimal(18,2)")]
        public decimal AllowedAmount { get; set; }

        /// <summary>
        /// Deductible applied
        /// </summary>
[Column(TypeName = "decimal(18,2)")]
     public decimal DeductibleApplied { get; set; }

  /// <summary>
   /// Coinsurance percentage (e.g., 0.20 for 20%)
 /// </summary>
     [Column(TypeName = "decimal(5,2)")]
   public decimal CoinsurancePercentage { get; set; }

        /// <summary>
        /// Coinsurance amount
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal CoinsuranceAmount { get; set; }

        /// <summary>
        /// Copay amount
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
     public decimal CopayAmount { get; set; }

        /// <summary>
        /// Out-of-pocket maximum limit
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal OutOfPocketMax { get; set; }

        /// <summary>
    /// Out-of-pocket applied so far (year-to-date)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal OutOfPocketApplied { get; set; }

        /// <summary>
        /// Annual maximum for this benefit
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
     public decimal? AnnualMaximum { get; set; }

        /// <summary>
     /// Annual maximum used so far (year-to-date)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AnnualMaximumUsed { get; set; }

        /// <summary>
        /// Net benefit payable (after all deductions)
  /// </summary>
    [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal BenefitPayable { get; set; }

        /// <summary>
        /// Patient responsibility (what patient owes)
   /// </summary>
        [Required]
 [Column(TypeName = "decimal(18,2)")]
        public decimal PatientResponsibility { get; set; }

        /// <summary>
     /// Calculation status
        /// Values: calculated, pending-verification, approved, denied, appealed
        /// </summary>
        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "calculated";

        /// <summary>
        /// Whether benefits were available/eligible
        /// </summary>
        public bool IsBenefitEligible { get; set; }

        /// <summary>
        /// Calculation timestamp
   /// </summary>
  [Required]
        public DateTime CalculationDate { get; set; } = DateTime.UtcNow;

     /// <summary>
      /// Notes about calculation
        /// </summary>
 [StringLength(500)]
        public string CalculationNotes { get; set; }

        // Navigation properties
 [ForeignKey("ClaimId")]
        public virtual Claim Claim { get; set; }

        [ForeignKey("ClaimItemId")]
        public virtual ClaimItem ClaimItem { get; set; }

        [ForeignKey("CoverageId")]
        public virtual Coverage Coverage { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = "System";
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }
}
