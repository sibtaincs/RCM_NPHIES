using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPhies_FHIR_Integration.Domain.Entities
{
    /// <summary>
    /// NPHIES Extension Entity
    /// Supports NPHIES-specific extensions
    /// </summary>
    [Table("NphiesExtension")]
   public class NphiesExtensionEntity : BaseEntity
    {
      [Key]
   public int Id { get; set; }

        /// <summary>
        /// Extension URL
   /// Examples:
        /// - http://nphies.sa/fhir/StructureDefinition/extension-rta-diagnosis
        /// - http://nphies.sa/fhir/StructureDefinition/extension-cause-of-death
        /// - http://nphies.sa/fhir/StructureDefinition/extension-diagnoses-on-admission
        /// </summary>
   [Required]
        [StringLength(300)]
   public string ExtensionUrl { get; set; }

  /// <summary>
      /// Extension type for easier identification
        /// </summary>
        [Required]
        [StringLength(100)]
        public string ExtensionType { get; set; }

    /// <summary>
        /// Resource type this extension applies to
        /// </summary>
        [Required]
        [StringLength(100)]
        public string ResourceType { get; set; }

        /// <summary>
        /// Resource ID this extension belongs to
        /// </summary>
     [Required]
        [StringLength(100)]
        public string ResourceId { get; set; }

        /// <summary>
        /// Value type (CodeableConcept, string, date, boolean, etc.)
        /// </summary>
   [Required]
      [StringLength(50)]
        public string ValueType { get; set; }

     /// <summary>
  /// Extension value (serialized as needed)
  /// </summary>
        [Required]
public string Value { get; set; }

        /// <summary>
        /// Additional context
       /// </summary>
    [StringLength(500)]
  public string Context { get; set; }

        // Timestamps
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public string CreatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string UpdatedBy { get; set; }
    }
}
