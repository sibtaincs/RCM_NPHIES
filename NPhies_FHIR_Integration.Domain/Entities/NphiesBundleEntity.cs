using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPhies_FHIR_Integration.Domain.Entities
{
    /// <summary>
    /// NPHIES Bundle Entity
    /// Represents a FHIR Bundle containing multiple resources
    /// </summary>
    [Table("NphiesBundle")]
    public class NphiesBundleEntity : BaseEntity
    {
        [Key]
 public int Id { get; set; }

        /// <summary>
        /// Unique bundle identifier
        /// </summary>
        [Required]
        [StringLength(50)]
        public string BundleId { get; set; }

        /// <summary>
        /// Bundle type
   /// Values: message, transaction, batch, document, searchset, history, collection
        /// </summary>
        [Required]
        [StringLength(50)]
        public string BundleType { get; set; } = "message";

  /// <summary>
        /// Bundle timestamp (UTC)
 /// </summary>
   [Required]
     public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
/// Reference to message header (if type is "message")
        /// </summary>
        public int? MessageHeaderId { get; set; }

 /// <summary>
        /// Total number of entries
        /// </summary>
        public int TotalEntries { get; set; }

        /// <summary>
        /// Processing status
        /// </summary>
    [StringLength(50)]
        public string ProcessingStatus { get; set; } = "received";

        /// <summary>
        /// Whether bundle contains errors
        /// </summary>
   public bool HasErrors { get; set; }

        /// <summary>
        /// Error message (if any)
        /// </summary>
        [StringLength(500)]
        public string ErrorMessage { get; set; }

        /// <summary>
        /// Full bundle JSON (for flexibility)
        /// </summary>
    public string BundleJson { get; set; }

        // Navigation properties
        [ForeignKey("MessageHeaderId")]
  public virtual NphiesMessageHeaderEntity MessageHeader { get; set; }

        public virtual ICollection<BundleEntryEntity> Entries { get; set; } = new List<BundleEntryEntity>();

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public string CreatedBy { get; set; }
    }

    /// <summary>
    /// Bundle Entry Entity - Individual resources in a bundle
    /// </summary>
    [Table("BundleEntry")]
    public class BundleEntryEntity : BaseEntity
    {
        [Key]
     public int Id { get; set; }

        /// <summary>
   /// Reference to parent bundle
        /// </summary>
    [Required]
        [ForeignKey("Bundle")]
        public int BundleId { get; set; }

   /// <summary>
        /// Full URL of the resource
        /// </summary>
        [StringLength(200)]
    public string FullUrl { get; set; }

        /// <summary>
        /// Resource type (Claim, Patient, Organization, etc.)
        /// </summary>
        [Required]
        [StringLength(100)]
        public string ResourceType { get; set; }

        /// <summary>
      /// Resource ID
        /// </summary>
        [Required]
        [StringLength(100)]
        public string ResourceId { get; set; }

        /// <summary>
        /// Full resource JSON
        /// </summary>
        public string ResourceJson { get; set; }

        /// <summary>
        /// Sequence number in bundle
  /// </summary>
        public int SequenceNumber { get; set; }

     // Navigation
        public virtual NphiesBundleEntity Bundle { get; set; }

  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
