namespace NPhies_FHIR_Integration.Domain.Entities;

/// <summary>
/// EligibilityError entity - represents errors in eligibility requests/responses
/// FHIR Resource: CoverageEligibilityResponse.error or OperationOutcome.issue
/// </summary>
public class EligibilityError : BaseEntity
{
    /// <summary>
 /// Reference to eligibility request (if error in request)
    /// </summary>
    public string? EligibilityRequestId { get; set; }

    /// <summary>
    /// The eligibility request that had an error
    /// </summary>
    public CoverageEligibilityRequest? EligibilityRequest { get; set; }

    /// <summary>
    /// Reference to eligibility response (if error in response)
    /// </summary>
 public string? EligibilityResponseId { get; set; }

    /// <summary>
 /// The eligibility response that had an error
    /// </summary>
    public CoverageEligibilityResponse? EligibilityResponse { get; set; }

  // Error Classification
    /// <summary>
    /// NPhies error code (system-specific error identifier)
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// Error code system
    /// System: http://nphies.sa/terminology/CodeSystem/error-code
    /// </summary>
    public string ErrorCodeSystem { get; set; } = "http://nphies.sa/terminology/CodeSystem/error-code";

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public string ErrorMessage { get; set; } = string.Empty;

/// <summary>
    /// Detailed error description or explanation
    /// </summary>
    public string ErrorDetails { get; set; } = string.Empty;

    // Error Severity
    /// <summary>
    /// Error severity level: "fatal", "error", "warning", "information"
/// System: http://hl7.org/fhir/issue-severity
    /// </summary>
    public string Severity { get; set; } = "error";

    // Error Location
    /// <summary>
    /// Path or location in the request/response where error occurred
    /// </summary>
    public string ErrorLocation { get; set; } = string.Empty;

    /// <summary>
  /// Field or element name that caused the error
    /// </summary>
    public string ErrorField { get; set; } = string.Empty;

    // Additional Context
    /// <summary>
    /// HTTP status code if applicable
    /// </summary>
public int? HttpStatusCode { get; set; }

    /// <summary>
    /// Timestamp when error was recorded
    /// </summary>
    public DateTime ErrorOccurredAt { get; set; } = DateTime.UtcNow;

 /// <summary>
    /// Any additional contextual information
    /// </summary>
    public string AdditionalContext { get; set; } = string.Empty;

    // Methods
    /// <summary>
    /// Get severity name (human-readable)
 /// </summary>
    public string GetSeverityName() => Severity switch
    {
        "fatal" => "Fatal Error",
        "error" => "Error",
    "warning" => "Warning",
        "information" => "Information",
        _ => Severity
    };

    /// <summary>
    /// Check if error is critical (fatal or error)
    /// </summary>
 public bool IsCritical => Severity == "fatal" || Severity == "error";

    /// <summary>
    /// Check if error is warning level
    /// </summary>
    public bool IsWarning => Severity == "warning";

    /// <summary>
    /// Get full error description
    /// </summary>
    public string GetFullDescription()
    {
        var parts = new List<string>();
        
    parts.Add($"[{ErrorCode}] {ErrorMessage}");
        
        if (!string.IsNullOrEmpty(ErrorDetails))
    parts.Add($"Details: {ErrorDetails}");
        
        if (!string.IsNullOrEmpty(ErrorLocation))
    parts.Add($"Location: {ErrorLocation}");

        if (!string.IsNullOrEmpty(ErrorField))
    parts.Add($"Field: {ErrorField}");

        if (!string.IsNullOrEmpty(AdditionalContext))
            parts.Add($"Context: {AdditionalContext}");

        return string.Join(" | ", parts);
    }

    /// <summary>
    /// Map NPhies error code to user-friendly message
    /// </summary>
    public string GetUserFriendlyMessage()
    {
        return ErrorCode switch
        {
 "INVALID_INPUT" => "The provided information is invalid. Please check and try again.",
 "PATIENT_NOT_FOUND" => "Patient information could not be found in the system.",
            "COVERAGE_NOT_FOUND" => "Coverage information could not be found.",
            "COVERAGE_INACTIVE" => "The patient's coverage is not active for the requested service date.",
      "SERVICE_NOT_COVERED" => "The requested service is not covered under this insurance plan.",
          "INVALID_PROVIDER" => "The provider information is invalid or not recognized.",
            "INVALID_LOCATION" => "The service location is invalid or not recognized.",
  "DUPLICATE_REQUEST" => "A duplicate request has already been submitted.",
  "SYSTEM_ERROR" => "A system error occurred. Please contact support.",
  "TIMEOUT" => "The request timed out. Please try again.",
_ => $"An error occurred: {ErrorMessage}"
        };
    }
}
