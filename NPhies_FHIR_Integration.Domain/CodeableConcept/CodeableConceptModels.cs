using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace NPhies_FHIR_Integration.Domain.CodeableConcept.Models
{
    /// <summary>
    /// Represents a FHIR CodeSystem - a collection of codes with shared metadata
    /// </summary>
    [Table("CodeSystem")]
    public class CodeSystemEntity
    {
        [Key]
        public int CodeSystemId { get; set; }

        [Required]
        [StringLength(300)]
        public string Url { get; set; }

        [StringLength(50)]
        public string Version { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(300)]
        public string Title { get; set; }

        public string Definition { get; set; }

        [StringLength(200)]
        public string Committee { get; set; }

        [StringLength(100)]
        public string Oid { get; set; }

        public string Copyright { get; set; }

        [StringLength(300)]
        public string SourceResource { get; set; }

        public bool CaseSensitive { get; set; } = true;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<ConceptEntity> Concepts { get; set; } = new List<ConceptEntity>();
        public ICollection<ValueSetCodeSystemMapEntity> ValueSetMappings { get; set; } = new List<ValueSetCodeSystemMapEntity>();
    }

    /// <summary>
    /// Represents an individual code within a CodeSystem
    /// </summary>
    [Table("Concept")]
    public class ConceptEntity
    {
        [Key]
        public long ConceptId { get; set; }

        [Required]
        [ForeignKey("CodeSystem")]
        public int CodeSystemId { get; set; }

        [Required]
        [StringLength(100)]
        public string Code { get; set; }

        [StringLength(500)]
        public string Display { get; set; }

        public string Definition { get; set; }

        [StringLength(500)]
        public string DisplayArabic { get; set; }

        public string DefinitionArabic { get; set; }

        public bool IsActive { get; set; } = true;

        public int? SortOrder { get; set; }

        [StringLength(100)]
        public string ParentCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual CodeSystemEntity CodeSystem { get; set; }
    }

    /// <summary>
    /// Represents a FHIR ValueSet - a logical set of codes
    /// </summary>
    [Table("ValueSet")]
    public class ValueSetEntity
    {
        [Key]
        public int ValueSetId { get; set; }

        [Required]
        [StringLength(300)]
        public string Url { get; set; }

        [StringLength(50)]
        public string Version { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; }

        [StringLength(300)]
        public string Title { get; set; }

        public string Definition { get; set; }

        [StringLength(200)]
        public string Committee { get; set; }

        [StringLength(100)]
        public string Oid { get; set; }

        public string Copyright { get; set; }

        [StringLength(300)]
        public string SourceResource { get; set; }

        public string Restrictions { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Navigation properties
        public ICollection<ValueSetCodeSystemMapEntity> CodeSystemMappings { get; set; } = new List<ValueSetCodeSystemMapEntity>();
        public ICollection<ProfileElementEntity> ProfileElements { get; set; } = new List<ProfileElementEntity>();
        public ICollection<ConceptCodeFilterEntity> CodeFilters { get; set; } = new List<ConceptCodeFilterEntity>();
    }

    /// <summary>
    /// Represents the M:N relationship between ValueSets and CodeSystems
    /// </summary>
    [Table("ValueSetCodeSystemMap")]
    public class ValueSetCodeSystemMapEntity
    {
        [Key]
        public int ValueSetCodeSystemMapId { get; set; }

        [Required]
        [ForeignKey("ValueSet")]
        public int ValueSetId { get; set; }

        [Required]
        [ForeignKey("CodeSystem")]
        public int CodeSystemId { get; set; }

        public int? Sequence { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ValueSetEntity ValueSet { get; set; }
        public virtual CodeSystemEntity CodeSystem { get; set; }
    }

    /// <summary>
    /// Represents a FHIR Profile element binding to a ValueSet
    /// </summary>
    [Table("ProfileElement")]
    public class ProfileElementEntity
    {
        [Key]
        public int ProfileElementId { get; set; }

        [Required]
        [StringLength(100)]
        public string ProfileName { get; set; }

        [Required]
        [StringLength(400)]
        public string Path { get; set; }

        [StringLength(400)]
        public string PathArabic { get; set; }
        public string ElementPath { get; set; }

        public string Definition { get; set; }

        [ForeignKey("ValueSet")]
        public int? ValueSetId { get; set; }

        [StringLength(50)]
        public string BindingStrength { get; set; } // required, extensible, preferred, example

        [StringLength(100)]
        public string MessageType { get; set; } // eligibility-request, claim-request, etc.

        [StringLength(100)]
        public string ResourceType { get; set; } // Claim, CoverageEligibilityRequest, etc.

        public bool IsRequired { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual ValueSetEntity ValueSet { get; set; }
    }

    /// <summary>
    /// Represents code filters for ValueSets that restrict specific codes
    /// </summary>
    [Table("ConceptCodeFilter")]
    public class ConceptCodeFilterEntity
    {
        [Key]
        public int ConceptCodeFilterId { get; set; }

        [Required]
        [ForeignKey("ValueSet")]
        public int ValueSetId { get; set; }

        [Required]
        [StringLength(100)]
        public string Code { get; set; }

        [StringLength(500)]
        public string Display { get; set; }

        public string Notes { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ValueSetEntity ValueSet { get; set; }
    }

    /// <summary>
    /// Represents NPHIES-specific validation rules
    /// </summary>
    [Table("ValidationRule")]
    public class ValidationRuleEntity
    {
        [Key]
        public int ValidationRuleId { get; set; }

        [Required]
        [StringLength(50)]
        public string ErrorCode { get; set; }

        [Required]
        public string ErrorMessage { get; set; }

        public string ErrorMessageArabic { get; set; }

        [StringLength(400)]
        public string FieldPath { get; set; }

        [StringLength(100)]
        public string RuleType { get; set; } // CodeExists, SystemMustBe, ValueSetMembership, etc.

        public string Parameters { get; set; } // JSON for rule-specific parameters

        [StringLength(50)]
        public string Severity { get; set; } // error, warning, info

        // Navigation property
        [ForeignKey("ValueSet")]
        public int? ValueSetId { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public virtual ValueSetEntity? ValueSet { get; set; }
    }

    /// <summary>
    /// Represents NPHIES message types
    /// </summary>
    [Table("NphiesMessageType")]
    public class NphiesMessageTypeEntity
    {
        [Key]
        public int NphiesMessageTypeId { get; set; }

        [Required]
        [StringLength(100)]
        public string MessageType { get; set; } // eligibility-request, claim-request, etc.

        [StringLength(100)]
        public string MessageTypeArabic { get; set; }

        [Required]
        [StringLength(100)]
        public string FhirResourceType { get; set; } // Claim, CoverageEligibilityRequest, etc.

        public string Description { get; set; }

        [StringLength(50)]
        public string Version { get; set; }

        public bool IsActive { get; set; } = true;

        // Navigation properties
        public ICollection<NphiesMessageRequiredElementEntity> RequiredElements { get; set; } = new List<NphiesMessageRequiredElementEntity>();
    }

    /// <summary>
    /// Represents required elements for each NPHIES message type
    /// </summary>
    [Table("NphiesMessageRequiredElement")]
    public class NphiesMessageRequiredElementEntity
    {
        [Key]
        public int NphiesMessageRequiredElementId { get; set; }

        [Required]
        [ForeignKey("NphiesMessageType")]
        public int NphiesMessageTypeId { get; set; }

        [Required]
        [StringLength(400)]
        public string ElementPath { get; set; }

        [ForeignKey("ValueSet")]
        public int? ValueSetId { get; set; }

        public bool IsRequired { get; set; } = true;

        [StringLength(20)]
        public string Cardinality { get; set; } // 0..1, 1..1, 0..*, 1..*, etc.

        public string Notes { get; set; }

        // Navigation properties
        public virtual NphiesMessageTypeEntity NphiesMessageType { get; set; }
        public virtual ValueSetEntity ValueSet { get; set; }
    }

    // ============================================================================
    // DTOs for API responses
    // ============================================================================

    public class CodeSystemDto
    {
        public int CodeSystemId { get; set; }
        public string Url { get; set; }
        public string Version { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Definition { get; set; }
        public int ConceptCount { get; set; }
        public bool IsActive { get; set; }
    }

    public class ConceptDto
    {
        public long ConceptId { get; set; }
        public string Code { get; set; }
        public string Display { get; set; }
        public string Definition { get; set; }
        public string DisplayArabic { get; set; }
        public bool IsActive { get; set; }
    }

    public class ValueSetDto
    {
        public int ValueSetId { get; set; }
        public string Url { get; set; }
        public string Version { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Definition { get; set; }
        public List<string> CodeSystemUrls { get; set; } = new List<string>();
        public int ConceptCount { get; set; }
        public bool IsActive { get; set; }
    }

    public class ValidationRuleDto
    {
        public int ValidationRuleId { get; set; }
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorMessageArabic { get; set; }
        public string FieldPath { get; set; }
        public string RuleType { get; set; }
        public string Severity { get; set; }
    }
}
