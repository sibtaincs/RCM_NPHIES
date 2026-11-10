using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Domain.CodeableConcept.Models;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Domain.CodeableConcept.Services
{
    /// <summary>
    /// Service for CodeableConcept validation and lookup operations
    /// </summary>
    public interface ICodeableConceptService
    {
        // CodeSystem operations
        Task<CodeSystemDto> GetCodeSystemByUrlAsync(string url);
        Task<CodeSystemDto> GetCodeSystemByNameAsync(string name);
        Task<IEnumerable<CodeSystemDto>> GetAllCodeSystemsAsync(bool activeOnly = true);

        // Concept operations
        Task<ConceptDto> GetConceptAsync(string code, int codeSystemId);
        Task<IEnumerable<ConceptDto>> GetConceptsByCodeSystemAsync(int codeSystemId, bool activeOnly = true);
        Task<bool> IsValidConceptAsync(string code, string codeSystemUrl);

        // ValueSet operations
        Task<ValueSetDto> GetValueSetByUrlAsync(string url);
        Task<ValueSetDto> GetValueSetByNameAsync(string name);
        Task<IEnumerable<ValueSetDto>> GetAllValueSetsAsync(bool activeOnly = true);
        Task<IEnumerable<ConceptDto>> GetValueSetConceptsAsync(int valueSetId);
        Task<bool> IsCodeInValueSetAsync(string code, string valueSetUrl);

        // Validation operations
        Task<ValidationResultDto> ValidateCodeAsync(string code, string codeSystemUrl, string valueSetUrl = null);
        Task<ValidationResultDto> ValidateRequiredFieldAsync(string path, string code, string messageType);
        Task<IEnumerable<ValidationRuleDto>> GetValidationRulesForFieldAsync(string fieldPath);

        // Message type operations
        Task<NphiesMessageTypeDto> GetMessageTypeAsync(string messageType);
        Task<IEnumerable<NphiesMessageRequiredElementDto>> GetMessageRequiredElementsAsync(string messageType);
        Task<ValidationContextDto> GetValidationContextAsync(string messageType, string fieldPath);
    }

    /// <summary>
    /// Implementation of ICodeableConceptService
    /// </summary>
    public class CodeableConceptService : ICodeableConceptService
    {
        private readonly ApplicationDbContext _dbContext;

        public CodeableConceptService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        #region CodeSystem Operations

        public async Task<CodeSystemDto> GetCodeSystemByUrlAsync(string url)
        {
            var codeSystem = await _dbContext.CodeSystems
                       .AsNoTracking()
           .Where(cs => cs.Url == url && cs.IsActive)
                         .FirstOrDefaultAsync();

            if (codeSystem == null)
                return null;

            var conceptCount = await _dbContext.Concepts
           .Where(c => c.CodeSystemId == codeSystem.CodeSystemId && c.IsActive)
            .CountAsync();

            return MapToCodeSystemDto(codeSystem, conceptCount);
        }

        public async Task<CodeSystemDto> GetCodeSystemByNameAsync(string name)
        {
            var codeSystem = await _dbContext.CodeSystems
                 .AsNoTracking()
                .Where(cs => cs.Name == name && cs.IsActive)
               .FirstOrDefaultAsync();

            if (codeSystem == null)
                return null;

            var conceptCount = await _dbContext.Concepts
                       .Where(c => c.CodeSystemId == codeSystem.CodeSystemId && c.IsActive)
                  .CountAsync();

            return MapToCodeSystemDto(codeSystem, conceptCount);
        }

        public async Task<IEnumerable<CodeSystemDto>> GetAllCodeSystemsAsync(bool activeOnly = true)
        {
            var query = _dbContext.CodeSystems.AsNoTracking();

            if (activeOnly)
                query = query.Where(cs => cs.IsActive);

            var codeSystems = await query.ToListAsync();

            var dtos = new List<CodeSystemDto>();
            foreach (var cs in codeSystems)
            {
                var conceptCount = await _dbContext.Concepts
           .Where(c => c.CodeSystemId == cs.CodeSystemId && (activeOnly ? c.IsActive : true))
           .CountAsync();

                dtos.Add(MapToCodeSystemDto(cs, conceptCount));
            }

            return dtos;
        }

        #endregion

        #region Concept Operations

        public async Task<ConceptDto> GetConceptAsync(string code, int codeSystemId)
        {
            var concept = await _dbContext.Concepts
             .AsNoTracking()
                      .Where(c => c.Code == code && c.CodeSystemId == codeSystemId && c.IsActive)
                      .FirstOrDefaultAsync();

            return concept == null ? null : MapToConceptDto(concept);
        }

        public async Task<IEnumerable<ConceptDto>> GetConceptsByCodeSystemAsync(int codeSystemId, bool activeOnly = true)
        {
            var query = _dbContext.Concepts
             .AsNoTracking()
                  .Where(c => c.CodeSystemId == codeSystemId);

            if (activeOnly)
                query = query.Where(c => c.IsActive);

            var concepts = await query.OrderBy(c => c.SortOrder ?? 0).ToListAsync();
            return concepts.Select(MapToConceptDto).ToList();
        }

        public async Task<bool> IsValidConceptAsync(string code, string codeSystemUrl)
        {
            var codeSystem = await _dbContext.CodeSystems
          .AsNoTracking()
         .Where(cs => cs.Url == codeSystemUrl && cs.IsActive)
      .FirstOrDefaultAsync();

            if (codeSystem == null)
                return false;

            var conceptExists = await _dbContext.Concepts
               .AnyAsync(c => c.Code == code && c.CodeSystemId == codeSystem.CodeSystemId && c.IsActive);

            return conceptExists;
        }

        #endregion

        #region ValueSet Operations

        public async Task<ValueSetDto> GetValueSetByUrlAsync(string url)
        {
            var valueSet = await _dbContext.ValueSets
     .AsNoTracking()
        .Include(vs => vs.CodeSystemMappings)
      .Where(vs => vs.Url == url && vs.IsActive)
           .FirstOrDefaultAsync();

            if (valueSet == null)
                return null;

            return await MapToValueSetDtoAsync(valueSet);
        }

        public async Task<ValueSetDto> GetValueSetByNameAsync(string name)
        {
            var valueSet = await _dbContext.ValueSets
          .AsNoTracking()
            .Include(vs => vs.CodeSystemMappings)
                  .Where(vs => vs.Name == name && vs.IsActive)
         .FirstOrDefaultAsync();

            if (valueSet == null)
                return null;

            return await MapToValueSetDtoAsync(valueSet);
        }

        public async Task<IEnumerable<ValueSetDto>> GetAllValueSetsAsync(bool activeOnly = true)
        {
            var query = _dbContext.ValueSets.AsNoTracking();

            if (activeOnly)
                query = query.Where(vs => vs.IsActive);

            var valueSets = await query.ToListAsync();
            var dtos = new List<ValueSetDto>();

            foreach (var vs in valueSets)
                dtos.Add(await MapToValueSetDtoAsync(vs));

            return dtos;
        }

        public async Task<IEnumerable<ConceptDto>> GetValueSetConceptsAsync(int valueSetId)
        {
            var concepts = await _dbContext.Concepts
               .AsNoTracking()
               .Where(c => c.CodeSystem.ValueSetMappings
            .Any(vscm => vscm.ValueSetId == valueSetId) && c.IsActive)
                    .OrderBy(c => c.SortOrder)
                .ToListAsync();

            return concepts.Select(MapToConceptDto).ToList();
        }

        public async Task<bool> IsCodeInValueSetAsync(string code, string valueSetUrl)
        {
            var valueSet = await _dbContext.ValueSets
       .AsNoTracking()
                     .Include(vs => vs.CodeSystemMappings)
            .ThenInclude(vscm => vscm.CodeSystem)
               .Where(vs => vs.Url == valueSetUrl && vs.IsActive)
                .FirstOrDefaultAsync();

            if (valueSet == null)
                return false;

            var codeSystemIds = valueSet.CodeSystemMappings
        .Where(m => m.IsActive)
            .Select(m => m.CodeSystemId)
         .ToList();

            var conceptExists = await _dbContext.Concepts
          .AnyAsync(c => c.Code == code &&
        codeSystemIds.Contains(c.CodeSystemId) && c.IsActive);

            return conceptExists;
        }

        #endregion

        #region Validation Operations

        public async Task<ValidationResultDto> ValidateCodeAsync(string code, string codeSystemUrl, string valueSetUrl = null)
        {
            var result = new ValidationResultDto
            {
                IsValid = false,
                Errors = new List<string>()
            };

            // Validate CodeSystem exists
            var codeSystem = await _dbContext.CodeSystems
              .AsNoTracking()
         .Where(cs => cs.Url == codeSystemUrl && cs.IsActive)
     .FirstOrDefaultAsync();

            if (codeSystem == null)
            {
                result.Errors.Add($"CodeSystem '{codeSystemUrl}' not found or inactive.");
                return result;
            }

            // Validate code exists in CodeSystem
            var concept = await _dbContext.Concepts
   .AsNoTracking()
    .Where(c => c.Code == code && c.CodeSystemId == codeSystem.CodeSystemId && c.IsActive)
                .FirstOrDefaultAsync();

            if (concept == null)
            {
                result.Errors.Add($"Code '{code}' not found in CodeSystem '{codeSystemUrl}'.");
                return result;
            }

            // If ValueSet specified, validate code is in ValueSet
            if (!string.IsNullOrEmpty(valueSetUrl))
            {
                var isInValueSet = await IsCodeInValueSetAsync(code, valueSetUrl);
                if (!isInValueSet)
                {
                    result.Errors.Add($"Code '{code}' is not in ValueSet '{valueSetUrl}'.");
                    return result;
                }
            }

            result.IsValid = true;
            result.Concept = MapToConceptDto(concept);
            return result;
        }

        public async Task<ValidationResultDto> ValidateRequiredFieldAsync(string path, string code, string messageType)
        {
            var result = new ValidationResultDto
            {
                IsValid = false,
                Errors = new List<string>()
            };

            // Get validation context
            var context = await GetValidationContextAsync(messageType, path);
            if (context == null)
            {
                result.Errors.Add($"No validation context found for path '{path}' in message type '{messageType}'.");
                return result;
            }

            // Validate code
            if (context.ValueSetUrl != null)
            {
                var validationResult = await ValidateCodeAsync(code, context.CodeSystemUrl, context.ValueSetUrl);
                result.IsValid = validationResult.IsValid;
                result.Errors = validationResult.Errors;
                result.Concept = validationResult.Concept;
            }
            else
            {
                var validationResult = await ValidateCodeAsync(code, context.CodeSystemUrl);
                result.IsValid = validationResult.IsValid;
                result.Errors = validationResult.Errors;
                result.Concept = validationResult.Concept;
            }

            return result;
        }

        public async Task<IEnumerable<ValidationRuleDto>> GetValidationRulesForFieldAsync(string fieldPath)
        {
            var rules = await _dbContext.ValidationRules
               .AsNoTracking()
                 .Where(vr => vr.FieldPath == fieldPath && vr.IsActive)
                .ToListAsync();

            return rules.Select(r => new ValidationRuleDto
            {
                ValidationRuleId = r.ValidationRuleId,
                ErrorCode = r.ErrorCode,
                ErrorMessage = r.ErrorMessage,
                ErrorMessageArabic = r.ErrorMessageArabic,
                FieldPath = r.FieldPath,
                RuleType = r.RuleType,
                Severity = r.Severity
            }).ToList();
        }

        #endregion

        #region Message Type Operations

        public async Task<NphiesMessageTypeDto> GetMessageTypeAsync(string messageType)
        {
            var type = await _dbContext.NphiesMessageTypes
         .AsNoTracking()
                .Where(nmt => nmt.MessageType == messageType && nmt.IsActive)
     .FirstOrDefaultAsync();

            if (type == null)
                return null;

            return new NphiesMessageTypeDto
            {
                NphiesMessageTypeId = type.NphiesMessageTypeId,
                MessageType = type.MessageType,
                MessageTypeArabic = type.MessageTypeArabic,
                FhirResourceType = type.FhirResourceType,
                Description = type.Description,
                Version = type.Version
            };
        }

        public async Task<IEnumerable<NphiesMessageRequiredElementDto>> GetMessageRequiredElementsAsync(string messageType)
        {
            var messageTypeEntity = await _dbContext.NphiesMessageTypes
               .AsNoTracking()
       .Where(nmt => nmt.MessageType == messageType && nmt.IsActive)
        .FirstOrDefaultAsync();

            if (messageTypeEntity == null)
                return Enumerable.Empty<NphiesMessageRequiredElementDto>();

            var elements = await _dbContext.NphiesMessageRequiredElements
                     .AsNoTracking()
              .Where(nmre => nmre.NphiesMessageTypeId == messageTypeEntity.NphiesMessageTypeId)
                           .Include(nmre => nmre.ValueSet)
            .ToListAsync();

            return elements.Select(e => new NphiesMessageRequiredElementDto
            {
                ElementPath = e.ElementPath,
                ValueSetUrl = e.ValueSet?.Url,
                IsRequired = e.IsRequired,
                Cardinality = e.Cardinality,
                Notes = e.Notes
            }).ToList();
        }

        public async Task<ValidationContextDto> GetValidationContextAsync(string messageType, string fieldPath)
        {
            var element = await _dbContext.ProfileElements
       .AsNoTracking()
               .Include(pe => pe.ValueSet)
        .ThenInclude(vs => vs.CodeSystemMappings)
         .ThenInclude(vscm => vscm.CodeSystem)
              .Where(pe => pe.MessageType == messageType && pe.Path == fieldPath && pe.IsActive)
           .FirstOrDefaultAsync();

            if (element?.ValueSet == null)
                return null;

            var codeSystemUrl = element.ValueSet.CodeSystemMappings.FirstOrDefault()?.CodeSystem?.Url;

            return new ValidationContextDto
            {
                Path = fieldPath,
                MessageType = messageType,
                ValueSetUrl = element.ValueSet.Url,
                CodeSystemUrl = codeSystemUrl,
                IsRequired = element.IsRequired,
                BindingStrength = element.BindingStrength
            };
        }

        #endregion

        #region Helper Methods

        private CodeSystemDto MapToCodeSystemDto(CodeSystemEntity entity, int conceptCount)
        {
            return new CodeSystemDto
            {
                CodeSystemId = entity.CodeSystemId,
                Url = entity.Url,
                Version = entity.Version,
                Name = entity.Name,
                Title = entity.Title,
                Definition = entity.Definition,
                ConceptCount = conceptCount,
                IsActive = entity.IsActive
            };
        }

        private ConceptDto MapToConceptDto(ConceptEntity entity)
        {
            return new ConceptDto
            {
                ConceptId = entity.ConceptId,
                Code = entity.Code,
                Display = entity.Display,
                Definition = entity.Definition,
                DisplayArabic = entity.DisplayArabic,
                IsActive = entity.IsActive
            };
        }

        private async Task<ValueSetDto> MapToValueSetDtoAsync(ValueSetEntity entity)
        {
            var conceptCount = await _dbContext.Concepts
                 .Where(c => entity.CodeSystemMappings
                   .Select(m => m.CodeSystemId)
                     .Contains(c.CodeSystemId) && c.IsActive)
              .CountAsync();

            return new ValueSetDto
            {
                ValueSetId = entity.ValueSetId,
                Url = entity.Url,
                Version = entity.Version,
                Name = entity.Name,
                Title = entity.Title,
                Definition = entity.Definition,
                CodeSystemUrls = entity.CodeSystemMappings
          .Select(m => m.CodeSystem?.Url)
 .Where(u => u != null)
 .ToList(),
                ConceptCount = conceptCount,
                IsActive = entity.IsActive
            };
        }

        #endregion
    }

    #region DTOs

    public class ValidationResultDto
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; }
        public ConceptDto Concept { get; set; }
    }

    public class NphiesMessageTypeDto
    {
        public int NphiesMessageTypeId { get; set; }
        public string MessageType { get; set; }
        public string MessageTypeArabic { get; set; }
        public string FhirResourceType { get; set; }
        public string Description { get; set; }
        public string Version { get; set; }
    }

    public class NphiesMessageRequiredElementDto
    {
        public string ElementPath { get; set; }
        public string ValueSetUrl { get; set; }
        public bool IsRequired { get; set; }
        public string Cardinality { get; set; }
        public string Notes { get; set; }
    }

    public class ValidationContextDto
    {
        public string Path { get; set; }
        public string MessageType { get; set; }
        public string ValueSetUrl { get; set; }
        public string CodeSystemUrl { get; set; }
        public bool IsRequired { get; set; }
        public string BindingStrength { get; set; }
    }

    #endregion
}
