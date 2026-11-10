using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPhies_FHIR_Integration.Domain.Entities
{
    /// <summary>
    /// Tracks deductible usage and remaining balance
    /// </summary>
    [Table("DeductibleTracking")]
    public class DeductibleTrackingEntity : BaseEntity
    {
 [Key]
        public int Id { get; set; }

        /// <summary>
        /// Reference to coverage/insurance plan
        /// </summary>
   [Required]
   [ForeignKey("Coverage")]
        public int CoverageId { get; set; }

        /// <summary>
     /// Reference to patient
        /// </summary>
        [Required]
     [ForeignKey("Patient")]
        public int PatientId { get; set; }

        /// <summary>
        /// Deductible type
   /// Values: individual, family, combined
        /// </summary>
        [Required]
    [StringLength(50)]
        public string DeductibleType { get; set; }

        /// <summary>
        /// Deductible amount for this plan year
        /// </summary>
        [Required]
     [Column(TypeName = "decimal(18,2)")]
        public decimal DeductibleAmount { get; set; }

        /// <summary>
        /// Amount applied/used so far this year
        /// </summary>
        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal AmountApplied { get; set; }

    /// <summary>
      /// Remaining deductible balance
     /// </summary>
        [Required]
   [Column(TypeName = "decimal(18,2)")]
        public decimal RemainingBalance { get; set; }

        /// <summary>
        /// Deductible met status
        /// </summary>
   [Required]
        public bool IsDeductibleMet { get; set; }

        /// <summary>
        /// Date deductible was met (if applicable)
        /// </summary>
  public DateTime? DeductibleMetDate { get; set; }

/// <summary>
        /// Plan year start date
        /// </summary>
        [Required]
        public DateTime PlanYearStartDate { get; set; }

        /// <summary>
      /// Plan year end date
        /// </summary>
        [Required]
        public DateTime PlanYearEndDate { get; set; }

        /// <summary>
    /// Services covered under deductible
        /// Values: all, inpatient-only, inpatient-outpatient, excludes-preventive
        /// </summary>
        [StringLength(50)]
      public string CoverageScope { get; set; }

        /// <summary>
/// Whether this is a family deductible
        /// </summary>
 public bool IsFamilyDeductible { get; set; }

        /// <summary>
     /// Deductible calculation status
        /// </summary>
        [StringLength(50)]
        public string Status { get; set; } = "active";

      /// <summary>
  /// Notes about deductible
    /// </summary>
        [StringLength(500)]
        public string Notes { get; set; }

   // Navigation properties
        [ForeignKey("CoverageId")]
        public virtual Coverage Coverage { get; set; }

        [ForeignKey("PatientId")]
        public virtual Patient Patient { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; } = "System";
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }
}
