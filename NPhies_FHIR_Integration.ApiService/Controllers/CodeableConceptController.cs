using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Domain.CodeableConcept.Services;
using NPhies_FHIR_Integration.Domain.CodeableConcept.Models;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// API Controller for NPHIES CodeableConcept operations (terminology management)
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Tags("Terminology - CodeableConcept")]
public class CodeableConceptController : ControllerBase
{
    private readonly ICodeableConceptService _service;
    private readonly ILogger<CodeableConceptController> _logger;

    public CodeableConceptController(
        ICodeableConceptService service,
        ILogger<CodeableConceptController> logger)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ========== CODE SYSTEM ENDPOINTS ==========

    /// <summary>
    /// Get all CodeSystems
    /// </summary>
    [HttpGet("codesystems")]
    [ProducesResponseType(typeof(IEnumerable<CodeSystemDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllCodeSystems([FromQuery] bool activeOnly = true)
    {
        try
        {
            _logger.LogInformation("Retrieving all CodeSystems (activeOnly: {ActiveOnly})", activeOnly);
            var codeSystems = await _service.GetAllCodeSystemsAsync(activeOnly);
            return Ok(codeSystems);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving CodeSystems");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Get CodeSystem by URL
    /// </summary>
    [HttpGet("codesystems/by-url")]
    [ProducesResponseType(typeof(CodeSystemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCodeSystemByUrl([FromQuery] string url)
    {
        try
        {
            _logger.LogInformation("Retrieving CodeSystem by URL: {Url}", url);
            var codeSystem = await _service.GetCodeSystemByUrlAsync(url);
            
            if (codeSystem == null)
                return NotFound(new { error = $"CodeSystem not found with URL: {url}" });

            return Ok(codeSystem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving CodeSystem by URL: {Url}", url);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Get CodeSystem by Name
    /// </summary>
    [HttpGet("codesystems/by-name/{name}")]
    [ProducesResponseType(typeof(CodeSystemDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCodeSystemByName(string name)
    {
        try
        {
            _logger.LogInformation("Retrieving CodeSystem by name: {Name}", name);
            var codeSystem = await _service.GetCodeSystemByNameAsync(name);
            
            if (codeSystem == null)
                return NotFound(new { error = $"CodeSystem not found with name: {name}" });

            return Ok(codeSystem);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving CodeSystem by name: {Name}", name);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    // ========== VALUE SET ENDPOINTS ==========

    /// <summary>
    /// Get all ValueSets
    /// </summary>
    [HttpGet("valuesets")]
    [ProducesResponseType(typeof(IEnumerable<ValueSetDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllValueSets([FromQuery] bool activeOnly = true)
    {
        try
        {
            _logger.LogInformation("Retrieving all ValueSets (activeOnly: {ActiveOnly})", activeOnly);
            var valueSets = await _service.GetAllValueSetsAsync(activeOnly);
            return Ok(valueSets);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ValueSets");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Get ValueSet by URL
    /// </summary>
    [HttpGet("valuesets/by-url")]
    [ProducesResponseType(typeof(ValueSetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetValueSetByUrl([FromQuery] string url)
    {
        try
        {
            _logger.LogInformation("Retrieving ValueSet by URL: {Url}", url);
            var valueSet = await _service.GetValueSetByUrlAsync(url);
            
            if (valueSet == null)
                return NotFound(new { error = $"ValueSet not found with URL: {url}" });

            return Ok(valueSet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ValueSet by URL: {Url}", url);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Get ValueSet by Name
    /// </summary>
    [HttpGet("valuesets/by-name/{name}")]
    [ProducesResponseType(typeof(ValueSetDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetValueSetByName(string name)
    {
        try
        {
            _logger.LogInformation("Retrieving ValueSet by name: {Name}", name);
            var valueSet = await _service.GetValueSetByNameAsync(name);
            
            if (valueSet == null)
                return NotFound(new { error = $"ValueSet not found with name: {name}" });

            return Ok(valueSet);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving ValueSet by name: {Name}", name);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Get all concepts in a ValueSet
    /// </summary>
    [HttpGet("valuesets/{valueSetId:int}/concepts")]
    [ProducesResponseType(typeof(IEnumerable<ConceptDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetValueSetConcepts(int valueSetId)
    {
        try
        {
            _logger.LogInformation("Retrieving concepts for ValueSet ID: {ValueSetId}", valueSetId);
            var concepts = await _service.GetValueSetConceptsAsync(valueSetId);
            return Ok(concepts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving concepts for ValueSet ID: {ValueSetId}", valueSetId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    // ========== CONCEPT ENDPOINTS ==========

    /// <summary>
    /// Get concepts by CodeSystem
    /// </summary>
    [HttpGet("concepts/by-codesystem/{codeSystemId:int}")]
    [ProducesResponseType(typeof(IEnumerable<ConceptDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConceptsByCodeSystem(int codeSystemId, [FromQuery] bool activeOnly = true)
    {
        try
        {
            _logger.LogInformation("Retrieving concepts for CodeSystem ID: {CodeSystemId}", codeSystemId);
            var concepts = await _service.GetConceptsByCodeSystemAsync(codeSystemId, activeOnly);
            return Ok(concepts);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving concepts for CodeSystem ID: {CodeSystemId}", codeSystemId);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Get specific concept
    /// </summary>
    [HttpGet("concepts/{codeSystemId:int}/{code}")]
    [ProducesResponseType(typeof(ConceptDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConcept(int codeSystemId, string code)
    {
        try
        {
            _logger.LogInformation("Retrieving concept: CodeSystem={CodeSystemId}, Code={Code}", codeSystemId, code);
            var concept = await _service.GetConceptAsync(code, codeSystemId);
            
            if (concept == null)
                return NotFound(new { error = $"Concept not found: {code} in CodeSystem {codeSystemId}" });

            return Ok(concept);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving concept: {Code}", code);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    // ========== VALIDATION ENDPOINTS ==========

    /// <summary>
    /// Validate a code against CodeSystem and optionally ValueSet
    /// </summary>
    [HttpPost("validate")]
    [ProducesResponseType(typeof(ValidationResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateCode([FromBody] ValidateCodeRequest request)
    {
        try
        {
            _logger.LogInformation("Validating code: {Code} in CodeSystem: {CodeSystemUrl}", 
                request.Code, request.CodeSystemUrl);

            var result = await _service.ValidateCodeAsync(
                request.Code, 
                request.CodeSystemUrl, 
                request.ValueSetUrl);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating code: {Code}", request.Code);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Check if code exists in ValueSet
    /// </summary>
    [HttpGet("valuesets/check-membership")]
    [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
    public async Task<IActionResult> IsCodeInValueSet(
        [FromQuery] string code, 
        [FromQuery] string valueSetUrl)
    {
        try
        {
            _logger.LogInformation("Checking if code {Code} is in ValueSet: {ValueSetUrl}", code, valueSetUrl);
            var isInValueSet = await _service.IsCodeInValueSetAsync(code, valueSetUrl);
            return Ok(new { code, valueSetUrl, isMember = isInValueSet });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking ValueSet membership for code: {Code}", code);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Validate a required field for a specific message type
    /// </summary>
    [HttpPost("validate-field")]
    [ProducesResponseType(typeof(ValidationResultDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> ValidateRequiredField([FromBody] ValidateFieldRequest request)
    {
        try
        {
            _logger.LogInformation("Validating field: {Path} for message type: {MessageType}", 
                request.FieldPath, request.MessageType);

            var result = await _service.ValidateRequiredFieldAsync(
                request.FieldPath, 
                request.Code, 
                request.MessageType);

            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating field: {Path}", request.FieldPath);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Get validation rules for a specific field
    /// </summary>
    [HttpGet("validation-rules")]
    [ProducesResponseType(typeof(IEnumerable<ValidationRuleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetValidationRules([FromQuery] string fieldPath)
    {
        try
        {
            _logger.LogInformation("Retrieving validation rules for field: {FieldPath}", fieldPath);
            var rules = await _service.GetValidationRulesForFieldAsync(fieldPath);
            return Ok(rules);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving validation rules for field: {FieldPath}", fieldPath);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    // ========== MESSAGE TYPE ENDPOINTS ==========

    /// <summary>
    /// Get NPHIES message type information
    /// </summary>
    [HttpGet("message-types/{messageType}")]
    [ProducesResponseType(typeof(NphiesMessageTypeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetMessageType(string messageType)
    {
        try
        {
            _logger.LogInformation("Retrieving message type: {MessageType}", messageType);
            var type = await _service.GetMessageTypeAsync(messageType);
            
            if (type == null)
                return NotFound(new { error = $"Message type not found: {messageType}" });

            return Ok(type);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving message type: {MessageType}", messageType);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Get required elements for a message type
    /// </summary>
    [HttpGet("message-types/{messageType}/required-elements")]
    [ProducesResponseType(typeof(IEnumerable<NphiesMessageRequiredElementDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMessageRequiredElements(string messageType)
    {
        try
        {
            _logger.LogInformation("Retrieving required elements for message type: {MessageType}", messageType);
            var elements = await _service.GetMessageRequiredElementsAsync(messageType);
            return Ok(elements);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving required elements for message type: {MessageType}", messageType);
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }

    /// <summary>
    /// Get validation context for a specific field in a message type
    /// </summary>
    [HttpGet("validation-context")]
    [ProducesResponseType(typeof(ValidationContextDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetValidationContext(
        [FromQuery] string messageType, 
        [FromQuery] string fieldPath)
    {
        try
        {
            _logger.LogInformation("Retrieving validation context: MessageType={MessageType}, FieldPath={FieldPath}", 
                messageType, fieldPath);

            var context = await _service.GetValidationContextAsync(messageType, fieldPath);
            
            if (context == null)
                return NotFound(new { error = $"Validation context not found for {fieldPath} in {messageType}" });

            return Ok(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving validation context");
            return StatusCode(500, new { error = "Internal server error", details = ex.Message });
        }
    }
}

// ========== REQUEST MODELS ==========

/// <summary>
/// Request model for code validation
/// </summary>
public class ValidateCodeRequest
{
    public string Code { get; set; } = string.Empty;
    public string CodeSystemUrl { get; set; } = string.Empty;
    public string? ValueSetUrl { get; set; }
}

/// <summary>
/// Request model for field validation
/// </summary>
public class ValidateFieldRequest
{
    public string FieldPath { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string MessageType { get; set; } = string.Empty;
}