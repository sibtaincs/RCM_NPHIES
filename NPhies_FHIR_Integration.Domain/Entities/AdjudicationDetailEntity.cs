using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPhies_FHIR_Integration.Domain.Entities
{
    /// <summary>
    /// Adjudication Detail Entity
    /// Represents item-level adjudication results from NPHIES
    /// Maps to FHIR ClaimResponse.adjudication
    /// </summary>
    [Table("AdjudicationDetail")]
  public class AdjudicationDetailEntity : BaseEntity
    {
[Key]
        public int Id { get; set; }

        /// <summary>
     /// Reference to ClaimResponse
        /// </summary>
        [Required]
  [ForeignKey("ClaimResponse")]
        public int ClaimResponseId { get; set; }

        /// <summary>
   /// Sequence number from original claim item
        /// </summary>
     [Required]
        public int ItemSequence { get; set; }

        /// <summary>
        /// Adjudication code (e.g., submitted, approved, denied)
        /// </summary>
        [Required]
        [StringLength(50)]
        public string AdjudicationCode { get; set; }

     /// <summary>
        /// Adjudication category (e.g., submitted, eligible, benefit)
        /// </summary>
  [StringLength(50)]
        public string AdjudicationCategory { get; set; }

        /// <summary>
 /// Amount adjudicated
  /// </summary>
      [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        /// <summary>
    /// Percentage applied (e.g., coinsurance)
        /// </summary>
        [Column(TypeName = "decimal(5,2)")]
        public decimal? Percentage { get; set; }

        /// <summary>
        /// Reason for adjudication decision
 /// </summary>
        [StringLength(500)]
        public string Reason { get; set; }

        /// <summary>
 /// Adjudication status (approved, partial, denied)
   /// </summary>
    [Required]
   [StringLength(50)]
        public string Status { get; set; }

        /// <summary>
      /// Benefit type applied
        /// </summary>
        [StringLength(100)]
        public string BenefitType { get; set; }

        /// <summary>
     /// Whether this item was subject to deductible
        /// </summary>
 public bool IsSubjectToDeductible { get; set; }

        /// <summary>
        /// Deductible amount applied
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal DeductibleApplied { get; set; }

  /// <summary>
   /// Whether this item was subject to coinsurance
        /// </summary>
        public bool IsSubjectToCoinsurance { get; set; }

        /// <summary>
     /// Coinsurance amount applied
     /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal CoinsuranceApplied { get; set; }

   /// <summary>
      /// Whether this item was subject to out-of-pocket maximum
        /// </summary>
        public bool IsSubjectToOutOfPocket { get; set; }

      /// <summary>
     /// Out-of-pocket amount applied
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal OutOfPocketApplied { get; set; }

   /// <summary>
        /// Network status (in-network, out-of-network, unknown)
 /// </summary>
        [StringLength(50)]
        public string NetworkStatus { get; set; }

        /// <summary>
     /// Allowed amount (what insurance will pay for)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
        public decimal? AllowedAmount { get; set; }

        /// <summary>
        /// Benefit allowance (amount covered by benefit)
        /// </summary>
        [Column(TypeName = "decimal(18,2)")]
   public decimal? BenefitAllowance { get; set; }

     /// <summary>
        /// Patient responsibility
    /// </summary>
        [Column(TypeName = "decimal(18,2)")]
    public decimal PatientResponsibility { get; set; }

     /// <summary>
        /// Additional metadata as JSON
        /// </summary>
        public string MetadataJson { get; set; }

    // Navigation properties
    [ForeignKey("ClaimResponseId")]
        public virtual ClaimResponse ClaimResponse { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
   public string UpdatedBy { get; set; }
    }
}
