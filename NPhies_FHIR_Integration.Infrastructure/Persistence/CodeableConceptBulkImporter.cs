using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Domain.CodeableConcept.Models;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Infrastructure.Persistence
{
    /// <summary>
    /// Utility for bulk importing CodeableConcept data from Excel/CSV files
    /// </summary>
    public interface ICodeableConceptBulkImporter
    {
        Task ImportConceptsAsync(int codeSystemId, IEnumerable<ConceptImportModel> concepts);
        Task ImportValueSetMappingsAsync(int valueSetId, IEnumerable<string> codeSystemUrls);
        Task ImportProfileElementsAsync(IEnumerable<ProfileElementImportModel> elements);
        Task ImportMessageRequiredElementsAsync(string messageType, IEnumerable<MessageElementImportModel> elements);
        Task ImportValidationRulesAsync(IEnumerable<ValidationRuleImportModel> rules);
    }

    public class CodeableConceptBulkImporter : ICodeableConceptBulkImporter
    {
        private readonly ApplicationDbContext _dbContext;

        public CodeableConceptBulkImporter(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Import concepts for a CodeSystem (from Excel/CSV)
        /// </summary>
        public async Task ImportConceptsAsync(int codeSystemId, IEnumerable<ConceptImportModel> concepts)
        {
            // Validate CodeSystem exists
            var codeSystem = await _dbContext.CodeSystems.FindAsync(codeSystemId);
            if (codeSystem == null)
                throw new InvalidOperationException($"CodeSystem with ID {codeSystemId} not found.");

            var conceptsToAdd = new List<ConceptEntity>();
            var sortOrder = 0;

            foreach (var concept in concepts.Where(c => !string.IsNullOrWhiteSpace(c.Code)))
            {
                // Check for duplicates
                var exists = await _dbContext.Concepts
             .AnyAsync(c => c.CodeSystemId == codeSystemId && c.Code == concept.Code);

                if (exists)
                {
                    Console.WriteLine($"??  Code '{concept.Code}' already exists in CodeSystem {codeSystemId}, skipping.");
                    continue;
                }

                conceptsToAdd.Add(new ConceptEntity
                {
                    CodeSystemId = codeSystemId,
                    Code = concept.Code.Trim(),
                    Display = concept.Display?.Trim(),
                    Definition = concept.Definition?.Trim(),
                    DisplayArabic = concept.DisplayArabic?.Trim(),
                    DefinitionArabic = concept.DefinitionArabic?.Trim(),
                    IsActive = concept.IsActive ?? true,
                    SortOrder = sortOrder++,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (conceptsToAdd.Any())
            {
                _dbContext.Concepts.AddRange(conceptsToAdd);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"? Imported {conceptsToAdd.Count} concepts for CodeSystem {codeSystemId}");
            }
        }

        /// <summary>
        /// Link a ValueSet to multiple CodeSystems
        /// </summary>
        public async Task ImportValueSetMappingsAsync(int valueSetId, IEnumerable<string> codeSystemUrls)
        {
            var valueSet = await _dbContext.ValueSets.FindAsync(valueSetId);
            if (valueSet == null)
                throw new InvalidOperationException($"ValueSet with ID {valueSetId} not found.");

            var mappingsToAdd = new List<ValueSetCodeSystemMapEntity>();
            var sequence = 0;

            foreach (var url in codeSystemUrls.Where(u => !string.IsNullOrWhiteSpace(u)))
            {
                var codeSystem = await _dbContext.CodeSystems
                                .Where(cs => cs.Url == url.Trim())
                  .FirstOrDefaultAsync();

                if (codeSystem == null)
                {
                    Console.WriteLine($"??  CodeSystem '{url}' not found, skipping.");
                    continue;
                }

                // Check for duplicates
                var exists = await _dbContext.ValueSetCodeSystemMaps
                     .AnyAsync(m => m.ValueSetId == valueSetId && m.CodeSystemId == codeSystem.CodeSystemId);

                if (exists)
                {
                    Console.WriteLine($"??  Mapping already exists for ValueSet {valueSetId} -> CodeSystem {codeSystem.CodeSystemId}");
                    continue;
                }

                mappingsToAdd.Add(new ValueSetCodeSystemMapEntity
                {
                    ValueSetId = valueSetId,
                    CodeSystemId = codeSystem.CodeSystemId,
                    Sequence = sequence++,
                    IsActive = true
                });
            }

            if (mappingsToAdd.Any())
            {
                _dbContext.ValueSetCodeSystemMaps.AddRange(mappingsToAdd);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"? Imported {mappingsToAdd.Count} ValueSet mappings");
            }
        }

        /// <summary>
        /// Import FHIR profile elements
        /// </summary>
        public async Task ImportProfileElementsAsync(IEnumerable<ProfileElementImportModel> elements)
        {
            var elementsToAdd = new List<ProfileElementEntity>();

            foreach (var element in elements.Where(e => !string.IsNullOrWhiteSpace(e.Path)))
            {
                // Find ValueSet if specified
                int? valueSetId = null;
                if (!string.IsNullOrWhiteSpace(element.ValueSetUrl))
                {
                    var vs = await _dbContext.ValueSets
                           .Where(v => v.Url == element.ValueSetUrl)
                 .FirstOrDefaultAsync();

                    if (vs == null)
                    {
                        Console.WriteLine($"??  ValueSet '{element.ValueSetUrl}' not found for path '{element.Path}'");
                        continue;
                    }

                    valueSetId = vs.ValueSetId;
                }

                // Check for duplicates
                var exists = await _dbContext.ProfileElements
            .AnyAsync(pe => pe.Path == element.Path && pe.ProfileName == element.ProfileName);

                if (exists)
                {
                    Console.WriteLine($"??  Profile element '{element.Path}' already exists");
                    continue;
                }

                elementsToAdd.Add(new ProfileElementEntity
                {
                    ProfileName = element.ProfileName ?? "NPHIES",
                    Path = element.Path.Trim(),
                    PathArabic = element.PathArabic?.Trim(),
                    Definition = element.Definition?.Trim(),
                    ValueSetId = valueSetId,
                    BindingStrength = element.BindingStrength ?? "required",
                    MessageType = element.MessageType?.Trim(),
                    ResourceType = element.ResourceType?.Trim(),
                    IsRequired = element.IsRequired ?? false,
                    IsActive = element.IsActive ?? true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (elementsToAdd.Any())
            {
                _dbContext.ProfileElements.AddRange(elementsToAdd);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"? Imported {elementsToAdd.Count} profile elements");
            }
        }

        /// <summary>
        /// Import message type required elements
        /// </summary>
        public async Task ImportMessageRequiredElementsAsync(string messageType, IEnumerable<MessageElementImportModel> elements)
        {
            var nmt = await _dbContext.NphiesMessageTypes
         .Where(m => m.MessageType == messageType)
            .FirstOrDefaultAsync();

            if (nmt == null)
                throw new InvalidOperationException($"Message type '{messageType}' not found.");

            var elementsToAdd = new List<NphiesMessageRequiredElementEntity>();

            foreach (var element in elements.Where(e => !string.IsNullOrWhiteSpace(e.ElementPath)))
            {
                // Find ValueSet if specified
                int? valueSetId = null;
                if (!string.IsNullOrWhiteSpace(element.ValueSetUrl))
                {
                    var vs = await _dbContext.ValueSets
                     .Where(v => v.Url == element.ValueSetUrl)
                          .FirstOrDefaultAsync();

                    if (vs != null)
                        valueSetId = vs.ValueSetId;
                }

                elementsToAdd.Add(new NphiesMessageRequiredElementEntity
                {
                    NphiesMessageTypeId = nmt.NphiesMessageTypeId,
                    ElementPath = element.ElementPath.Trim(),
                    ValueSetId = valueSetId,
                    IsRequired = element.IsRequired ?? true,
                    Cardinality = element.Cardinality ?? "1..1",
                    Notes = element.Notes?.Trim()
                });
            }

            if (elementsToAdd.Any())
            {
                _dbContext.NphiesMessageRequiredElements.AddRange(elementsToAdd);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"? Imported {elementsToAdd.Count} required elements for message type '{messageType}'");
            }
        }

        /// <summary>
        /// Import validation rules
        /// </summary>
        public async Task ImportValidationRulesAsync(IEnumerable<ValidationRuleImportModel> rules)
        {
            var rulesToAdd = new List<ValidationRuleEntity>();

            foreach (var rule in rules.Where(r => !string.IsNullOrWhiteSpace(r.ErrorCode)))
            {
                // Check for duplicates
                var exists = await _dbContext.ValidationRules
                    .AnyAsync(vr => vr.ErrorCode == rule.ErrorCode);

                if (exists)
                {
                    Console.WriteLine($"??  Validation rule '{rule.ErrorCode}' already exists");
                    continue;
                }

                rulesToAdd.Add(new ValidationRuleEntity
                {
                    ErrorCode = rule.ErrorCode.Trim(),
                    ErrorMessage = rule.ErrorMessage?.Trim(),
                    ErrorMessageArabic = rule.ErrorMessageArabic?.Trim(),
                    FieldPath = rule.FieldPath?.Trim(),
                    RuleType = rule.RuleType?.Trim(),
                    Parameters = rule.Parameters?.Trim(),
                    Severity = rule.Severity ?? "error",
                    IsActive = rule.IsActive ?? true,
                    CreatedAt = DateTime.UtcNow
                });
            }

            if (rulesToAdd.Any())
            {
                _dbContext.ValidationRules.AddRange(rulesToAdd);
                await _dbContext.SaveChangesAsync();
                Console.WriteLine($"? Imported {rulesToAdd.Count} validation rules");
            }
        }
    }

    // ============================================================================
    // Import Models (map from CSV/Excel)
    // ============================================================================

    public class ConceptImportModel
    {
        public string Code { get; set; }
        public string Display { get; set; }
        public string Definition { get; set; }
        public string DisplayArabic { get; set; }
        public string DefinitionArabic { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ProfileElementImportModel
    {
        public string ProfileName { get; set; }
        public string Path { get; set; }
        public string PathArabic { get; set; }
        public string Definition { get; set; }
        public string ValueSetUrl { get; set; }
        public string BindingStrength { get; set; }
        public string MessageType { get; set; }
        public string ResourceType { get; set; }
        public bool? IsRequired { get; set; }
        public bool? IsActive { get; set; }
    }

    public class MessageElementImportModel
    {
        public string ElementPath { get; set; }
        public string ValueSetUrl { get; set; }
        public bool? IsRequired { get; set; }
        public string Cardinality { get; set; }
        public string Notes { get; set; }
    }

    public class ValidationRuleImportModel
    {
        public string ErrorCode { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorMessageArabic { get; set; }
        public string FieldPath { get; set; }
        public string RuleType { get; set; }
        public string Parameters { get; set; }
        public string Severity { get; set; }
        public bool? IsActive { get; set; }
    }
}
