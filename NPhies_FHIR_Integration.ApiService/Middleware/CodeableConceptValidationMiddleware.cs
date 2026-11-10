using NPhies_FHIR_Integration.Domain.CodeableConcept.Services;
using System.Text.Json;

namespace NPhies_FHIR_Integration.ApiService.Middleware;

/// <summary>
/// Middleware for automatic validation of CodeableConcept fields in NPHIES requests
/// Validates codes against appropriate ValueSets based on message type
/// </summary>
public class CodeableConceptValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<CodeableConceptValidationMiddleware> _logger;

    public CodeableConceptValidationMiddleware(
        RequestDelegate next,
        ILogger<CodeableConceptValidationMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, ICodeableConceptService codeableConceptService)
    {
    // Only validate POST/PUT requests to NPHIES endpoints
        if (!ShouldValidate(context))
      {
        await _next(context);
   return;
        }

     // Enable request buffering to read body multiple times
        context.Request.EnableBuffering();

        try
        {
  // Read request body
    using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
   var requestBody = await reader.ReadToEndAsync();
       context.Request.Body.Position = 0; // Reset position for next middleware

   // Parse JSON and extract message type
         var jsonDoc = JsonDocument.Parse(requestBody);
     var messageType = ExtractMessageType(context, jsonDoc);

            if (!string.IsNullOrEmpty(messageType))
    {
      // Validate CodeableConcept fields
            var validationErrors = await ValidateCodeableConceptFields(
     codeableConceptService, 
  messageType, 
           jsonDoc);

    if (validationErrors.Any())
       {
    _logger.LogWarning("CodeableConcept validation failed for {MessageType}: {Errors}", 
         messageType, string.Join(", ", validationErrors));

            context.Response.StatusCode = StatusCodes.Status400BadRequest;
        context.Response.ContentType = "application/json";

      var errorResponse = new
  {
   error = "CodeableConcept Validation Failed",
  messageType,
  validationErrors
         };

          await context.Response.WriteAsJsonAsync(errorResponse);
         return;
     }

   _logger.LogInformation("CodeableConcept validation passed for {MessageType}", messageType);
      }
     }
   catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CodeableConceptValidationMiddleware");
          // Continue to next middleware on error - don't block requests
        }

        await _next(context);
    }

    private bool ShouldValidate(HttpContext context)
    {
     // Only validate specific NPHIES endpoints
        var path = context.Request.Path.Value?.ToLower() ?? "";
        var method = context.Request.Method.ToUpper();

        if (method != "POST" && method != "PUT")
        return false;

  // Validate claims, eligibility, pre-auth, etc.
        return path.Contains("/claims") ||
       path.Contains("/eligibility") ||
   path.Contains("/preauthorization") ||
               path.Contains("/pre-authorization");
    }

    private string ExtractMessageType(HttpContext context, JsonDocument jsonDoc)
    {
        // Try to determine message type from:
     // 1. Request path
        var path = context.Request.Path.Value?.ToLower() ?? "";
        
        if (path.Contains("/claims/submit"))
    return "claim-request";
        if (path.Contains("/eligibility/check"))
      return "eligibility-request";
        if (path.Contains("/preauthorization/submit") || path.Contains("/pre-authorization/submit"))
            return "priorauth-request";

      // 2. JSON resourceType field
   if (jsonDoc.RootElement.TryGetProperty("resourceType", out var resourceType))
 {
  var type = resourceType.GetString()?.ToLower();
   return type switch
          {
     "claim" => "claim-request",
            "coverageeligibilityrequest" => "eligibility-request",
  "claimresponse" => "claim-response",
     "coverageeligibilityresponse" => "eligibility-response",
                _ => string.Empty
         };
        }

  return string.Empty;
    }

    private async Task<List<string>> ValidateCodeableConceptFields(
        ICodeableConceptService service,
        string messageType,
   JsonDocument jsonDoc)
    {
     var errors = new List<string>();

        try
        {
            // Get required elements for this message type
          var requiredElements = await service.GetMessageRequiredElementsAsync(messageType);

         foreach (var element in requiredElements.Where(e => e.IsRequired))
    {
                // Extract value from JSON using field path
     var value = ExtractValueFromPath(jsonDoc, element.ElementPath);
   
           if (string.IsNullOrEmpty(value))
           {
    errors.Add($"Required field '{element.ElementPath}' is missing");
             continue;
     }

            // Validate against ValueSet if specified
          if (!string.IsNullOrEmpty(element.ValueSetUrl))
         {
   var validationResult = await service.ValidateRequiredFieldAsync(
              element.ElementPath, 
     value, 
          messageType);

          if (!validationResult.IsValid)
              {
   errors.AddRange(validationResult.Errors);
         }
}
            }
        }
      catch (Exception ex)
        {
            _logger.LogError(ex, "Error validating CodeableConcept fields for {MessageType}", messageType);
            // Don't add to errors - allow request to proceed
  }

        return errors;
    }

    private string? ExtractValueFromPath(JsonDocument jsonDoc, string path)
    {
try
        {
            // Simple path extraction (e.g., "Claim.type" -> type field)
     // For complex paths, would need more sophisticated parsing
            var parts = path.Split('.');
      var current = jsonDoc.RootElement;

        foreach (var part in parts.Skip(1)) // Skip resource name
    {
     if (current.TryGetProperty(part, out var element))
       {
          current = element;
        }
     else
       {
          return null;
        }
}

   // Handle CodeableConcept structure
       if (current.ValueKind == JsonValueKind.Object)
    {
   if (current.TryGetProperty("coding", out var coding) && 
        coding.ValueKind == JsonValueKind.Array && 
        coding.GetArrayLength() > 0)
                {
        var firstCoding = coding[0];
    if (firstCoding.TryGetProperty("code", out var code))
              {
            return code.GetString();
   }
       }
            }

            return current.ValueKind == JsonValueKind.String ? current.GetString() : null;
  }
   catch
        {
     return null;
}
    }
}
