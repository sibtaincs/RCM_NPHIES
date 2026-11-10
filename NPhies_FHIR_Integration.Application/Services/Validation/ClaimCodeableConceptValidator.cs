using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Domain.CodeableConcept.Services;
using NPhies_FHIR_Integration.Domain.Entities;

namespace NPhies_FHIR_Integration.Application.Services.Validation;

/// <summary>
/// Claim validation extension that uses CodeableConcept service
/// for terminology validation
/// </summary>
public class ClaimCodeableConceptValidator
{
    private readonly ICodeableConceptService _codeableConceptService;
    private readonly ILogger<ClaimCodeableConceptValidator> _logger;

    public ClaimCodeableConceptValidator(
        ICodeableConceptService codeableConceptService,
        ILogger<ClaimCodeableConceptValidator> logger)
    {
   _codeableConceptService = codeableConceptService ?? throw new ArgumentNullException(nameof(codeableConceptService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Validate all CodeableConcept fields in a Claim
    /// </summary>
    public async Task<ClaimValidationResult> ValidateClaimAsync(Claim claim)
    {
     var result = new ClaimValidationResult
        {
            IsValid = true,
            Errors = new List<string>()
        };

        try
        {
            _logger.LogInformation("Validating claim {ClaimNumber} CodeableConcept fields", claim.ClaimNumber);

            // Validate claim type
  await ValidateClaimTypeAsync(claim, result);

       // Validate claim subtype
            await ValidateClaimSubTypeAsync(claim, result);

            // Validate priority
      await ValidatePriorityAsync(claim, result);

            // Validate diagnoses
    await ValidateDiagnosesAsync(claim, result);

            // Validate claim items
          await ValidateClaimItemsAsync(claim, result);

       // Validate care team
            await ValidateCareTeamAsync(claim, result);

 result.IsValid = !result.Errors.Any();

         if (result.IsValid)
            {
           _logger.LogInformation("? Claim {ClaimNumber} passed CodeableConcept validation", claim.ClaimNumber);
            }
   else
      {
    _logger.LogWarning("? Claim {ClaimNumber} failed CodeableConcept validation with {ErrorCount} errors", 
   claim.ClaimNumber, result.Errors.Count);
}
        }
        catch (Exception ex)
     {
         _logger.LogError(ex, "Error validating claim {ClaimNumber}", claim.ClaimNumber);
    result.IsValid = false;
            result.Errors.Add($"Validation error: {ex.Message}");
        }

        return result;
    }

    private async Task ValidateClaimTypeAsync(Claim claim, ClaimValidationResult result)
    {
        if (string.IsNullOrEmpty(claim.ClaimType))
        {
            result.Errors.Add("Claim.type is required");
        return;
        }

  var validationResult = await _codeableConceptService.ValidateRequiredFieldAsync(
      "Claim.type",
 claim.ClaimType,
    "claim-request");

    if (!validationResult.IsValid)
        {
     result.Errors.AddRange(validationResult.Errors);
  }
    }

    private async Task ValidateClaimSubTypeAsync(Claim claim, ClaimValidationResult result)
    {
  if (string.IsNullOrEmpty(claim.ClaimSubType))
        {
     result.Errors.Add("Claim.subType is required");
            return;
        }

        var validationResult = await _codeableConceptService.ValidateRequiredFieldAsync(
        "Claim.subType",
      claim.ClaimSubType,
  "claim-request");

        if (!validationResult.IsValid)
   {
            result.Errors.AddRange(validationResult.Errors);
    }
    }

    private async Task ValidatePriorityAsync(Claim claim, ClaimValidationResult result)
    {
        if (string.IsNullOrEmpty(claim.Priority))
   {
            result.Errors.Add("Claim.priority is required");
         return;
        }

        var validationResult = await _codeableConceptService.ValidateRequiredFieldAsync(
        "Claim.priority",
  claim.Priority,
            "claim-request");

        if (!validationResult.IsValid)
     {
    result.Errors.AddRange(validationResult.Errors);
        }
    }

    private async Task ValidateDiagnosesAsync(Claim claim, ClaimValidationResult result)
    {
        if (claim.Diagnoses == null || !claim.Diagnoses.Any())
      {
            result.Errors.Add("Claim.diagnosis is required (at least one diagnosis)");
     return;
    }

    foreach (var diagnosis in claim.Diagnoses)
        {
  // Validate diagnosis code
          if (string.IsNullOrEmpty(diagnosis.DiagnosisCode))
   {
         result.Errors.Add($"Diagnosis sequence {diagnosis.Sequence}: diagnosis code is required");
       continue;
    }

            var diagnosisValidation = await _codeableConceptService.ValidateRequiredFieldAsync(
    "Claim.diagnosis.diagnosisCodeableConcept",
           diagnosis.DiagnosisCode,
        "claim-request");

            if (!diagnosisValidation.IsValid)
      {
       result.Errors.AddRange(diagnosisValidation.Errors.Select(e => 
    $"Diagnosis sequence {diagnosis.Sequence}: {e}"));
            }

            // Validate diagnosis type
          if (!string.IsNullOrEmpty(diagnosis.DiagnosisType))
   {
          var typeValidation = await _codeableConceptService.ValidateRequiredFieldAsync(
  "Claim.diagnosis.type",
                diagnosis.DiagnosisType,
    "claim-request");

     if (!typeValidation.IsValid)
       {
            result.Errors.AddRange(typeValidation.Errors.Select(e => 
      $"Diagnosis sequence {diagnosis.Sequence} type: {e}"));
         }
  }
        }
    }

    private async Task ValidateClaimItemsAsync(Claim claim, ClaimValidationResult result)
    {
   if (claim.Items == null || !claim.Items.Any())
   {
     result.Errors.Add("Claim.item is required (at least one item)");
   return;
        }

        foreach (var item in claim.Items)
        {
         if (string.IsNullOrEmpty(item.ProductOrServiceCode))
            {
       result.Errors.Add($"Item sequence {item.Sequence}: productOrService code is required");
 continue;
      }

      var itemValidation = await _codeableConceptService.ValidateRequiredFieldAsync(
     "Claim.item.productOrService",
     item.ProductOrServiceCode,
    "claim-request");

       if (!itemValidation.IsValid)
       {
         result.Errors.AddRange(itemValidation.Errors.Select(e => 
        $"Item sequence {item.Sequence}: {e}"));
            }
        }
  }

    private async Task ValidateCareTeamAsync(Claim claim, ClaimValidationResult result)
    {
        if (claim.CareTeam == null || !claim.CareTeam.Any())
        {
            // Care team is optional for some claim types
 return;
        }

        foreach (var member in claim.CareTeam)
   {
      // Validate role if present
if (!string.IsNullOrEmpty(member.Role))
            {
     var roleValidation = await _codeableConceptService.ValidateRequiredFieldAsync(
            "Claim.careTeam.role",
    member.Role,
      "claim-request");

             if (!roleValidation.IsValid)
  {
                    result.Errors.AddRange(roleValidation.Errors.Select(e => 
         $"Care team sequence {member.Sequence}: {e}"));
        }
}

     // Validate qualification if present
    if (!string.IsNullOrEmpty(member.Qualification))
   {
        var qualificationValidation = await _codeableConceptService.ValidateRequiredFieldAsync(
          "Claim.careTeam.qualification",
      member.Qualification,
   "claim-request");

                if (!qualificationValidation.IsValid)
         {
           result.Errors.AddRange(qualificationValidation.Errors.Select(e => 
             $"Care team sequence {member.Sequence} qualification: {e}"));
      }
        }
        }
    }
}

/// <summary>
/// Result of claim validation
/// </summary>
public class ClaimValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, string> ValidatedFields { get; set; } = new();
}
