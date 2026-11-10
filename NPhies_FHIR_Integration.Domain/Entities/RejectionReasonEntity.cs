using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPhies_FHIR_Integration.Domain.Entities
{
    /// <summary>
    /// Tracks rejection reasons for claims or items
    /// </summary>
  [Table("RejectionReason")]
    public class RejectionReasonEntity : BaseEntity
    {
        [Key]
    public int Id { get; set; }

        /// <summary>
        /// Reference to claim (if claim-level rejection)
        /// </summary>
    [ForeignKey("Claim")]
        public int? ClaimId { get; set; }

        /// <summary>
  /// Reference to claim item (if item-level rejection)
        /// </summary>
        [ForeignKey("ClaimItem")]
        public int? ClaimItemId { get; set; }

    /// <summary>
   /// Reference to claim response (if response-level)
      /// </summary>
        [ForeignKey("ClaimResponse")]
    public int? ClaimResponseId { get; set; }

      /// <summary>
        /// NPHIES rejection code
        /// Examples: invalid-code, duplicate-claim, coverage-not-found, prior-auth-required
        /// </summary>
        [Required]
        [StringLength(100)]
        public string RejectionCode { get; set; }

    /// <summary>
    /// Human-readable reason
        /// </summary>
        [Required]
        [StringLength(500)]
        public string ReasonDescription { get; set; }

      /// <summary>
 /// Arabic description for NPHIES localization
        /// </summary>
        [StringLength(500)]
        public string ReasonDescriptionArabic { get; set; }

        /// <summary>
        /// Severity level
        /// Values: error (fatal), warning (requires review), info (informational)
    /// </summary>
        [Required]
 [StringLength(20)]
   public string Severity { get; set; } = "error";

 /// <summary>
      /// Whether this rejection can be recovered (e.g., resubmit with corrections)
        /// </summary>
        public bool IsRecoverable { get; set; }

        /// <summary>
        /// Suggested remediation steps
        /// </summary>
   [StringLength(1000)]
   public string RemediationSteps { get; set; }

     /// <summary>
        /// When this rejection occurred
        /// </summary>
        [Required]
        public DateTime RejectionDate { get; set; } = DateTime.UtcNow;

        /// <summary>
     /// Whether this has been addressed/corrected
        /// </summary>
   public bool IsResolved { get; set; }

        /// <summary>
      /// When it was resolved (if applicable)
        /// </summary>
        public DateTime? ResolvedDate { get; set; }

  /// <summary>
  /// Notes about this rejection
        /// </summary>
        [StringLength(500)]
        public string Notes { get; set; }

    // Navigation properties
        public virtual Claim Claim { get; set; }
        public virtual ClaimItem ClaimItem { get; set; }
        public virtual ClaimResponse ClaimResponse { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
public string CreatedBy { get; set; } = "System";
 public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }
}
