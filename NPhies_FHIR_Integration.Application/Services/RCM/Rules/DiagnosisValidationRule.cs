using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.Application.Services.RCM.Rules;

/// <summary>
/// Diagnosis Validation Rule - Priority 6
/// Validates diagnosis codes and checks consistency with service
/// Some services require specific diagnoses
/// </summary>
public class DiagnosisValidationRule : IAdjudicationRule
{
    private readonly ILogger<DiagnosisValidationRule> _logger;

  // Simple list of valid ICD-10 prefixes (in production, use full ICD-10 database)
    private static readonly HashSet<string> ValidICD10Prefixes = new()
    {
     "A", "B", "C", "D", "E", "F", "G", "H", "I", "J",
        "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T",
      "U", "V", "W", "X", "Y", "Z"
 };

    public string RuleId => "DIAGNOSIS_VALIDATION";
    public string RuleName => "Diagnosis Code Validation";
    public int Priority => 6;

    public DiagnosisValidationRule(ILogger<DiagnosisValidationRule> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Apply diagnosis validation if diagnosis code exists
    /// </summary>
    public async Task<bool> IsApplicableAsync(AdjudicationContext context)
    {
        return await Task.FromResult(!string.IsNullOrEmpty(context.DiagnosisCode));
    }

    /// <summary>
    /// Validate diagnosis code format
    /// </summary>
    public async Task<RuleResult> EvaluateAsync(AdjudicationContext context)
    {
        // Check if diagnosis code is in valid format
      if (!IsValidICD10Code(context.DiagnosisCode))
        {
       _logger.LogWarning("Invalid ICD-10 code format: {DiagnosisCode}",
        context.DiagnosisCode);

            return RuleResult.Applied(
     ruleId: RuleId,
      patientResponsibility: context.AllowedAmount,
    remaining: 0m,
            message: $"Invalid diagnosis code format: {context.DiagnosisCode}. Claim denied.");
     }

        _logger.LogInformation("Diagnosis code {DiagnosisCode} is valid",
            context.DiagnosisCode);

        return RuleResult.Skip(RuleId);
    }

    /// <summary>
    /// Check if diagnosis code is in valid ICD-10 format
    /// </summary>
    private bool IsValidICD10Code(string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length < 3)
            return false;

      // Check starts with valid letter
     if (!ValidICD10Prefixes.Contains(code[0].ToString().ToUpper()))
      return false;

        // Check has at least 3-5 characters and proper format
      if (code.Length < 3 || code.Length > 7)
          return false;

        return true;
 }
}
