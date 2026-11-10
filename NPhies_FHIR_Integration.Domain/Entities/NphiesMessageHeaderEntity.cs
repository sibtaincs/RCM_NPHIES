using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPhies_FHIR_Integration.Domain.Entities
{
  /// <summary>
    /// NPHIES Message Header Entity
    /// Represents the message envelope for all NPHIES communications
    /// </summary>
    [Table("NphiesMessageHeader")]
 public class NphiesMessageHeaderEntity : BaseEntity
    {
     [Key]
        public int Id { get; set; }

        /// <summary>
        /// Unique NPHIES message identifier
        /// </summary>
        [Required]
        [StringLength(50)]
        public string MessageId { get; set; }

   /// <summary>
        /// NPHIES event code
        /// Values: claim-request, eligibility-request, poll-request, cancel-request, etc.
      /// </summary>
        [Required]
        [StringLength(100)]
        public string EventCode { get; set; }

        /// <summary>
        /// Message timestamp (UTC)
        /// </summary>
        [Required]
   public DateTime TimeSent { get; set; }

        /// <summary>
        /// Message version
        /// </summary>
        [StringLength(20)]
        public string MessageVersion { get; set; } = "1.0.0";

    /// <summary>
        /// Provider organization ID (sender)
        /// </summary>
        [Required]
     public int ProviderOrganizationId { get; set; }

     /// <summary>
    /// Payer identifier (receiver) - typically "NPHIES"
        /// </summary>
        [Required]
      [StringLength(100)]
        public string PayerIdentifier { get; set; } = "NPHIES";

        /// <summary>
        /// Primary resource type (Claim, CoverageEligibilityRequest, etc.)
        /// </summary>
  [Required]
        [StringLength(100)]
        public string PrimaryResourceType { get; set; }

        /// <summary>
        /// Primary resource ID
      /// </summary>
        [Required]
        [StringLength(100)]
        public string PrimaryResourceId { get; set; }

        /// <summary>
        /// Reason for message (optional)
        /// </summary>
        [StringLength(500)]
        public string Reason { get; set; }

        /// <summary>
    /// Processing status
        /// Values: received, validated, processed, rejected, pending
   /// </summary>
[StringLength(50)]
  public string ProcessingStatus { get; set; } = "received";

      /// <summary>
        /// Response message ID (if this is a response)
        /// </summary>
   [StringLength(50)]
        public string ResponseMessageId { get; set; }

        /// <summary>
        /// Processing result
        /// </summary>
        [StringLength(500)]
        public string ProcessingResult { get; set; }

        /// <summary>
     /// Additional NPHIES metadata in JSON
     /// </summary>
      public string MetadataJson { get; set; }

        // Navigation properties
        [ForeignKey("ProviderOrganizationId")]
public virtual Organization ProviderOrganization { get; set; }

        // Audit fields
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
}
}
