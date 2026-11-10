namespace NPhies_FHIR_Integration.Application.Exceptions;

/// <summary>
/// Exception thrown when NPHIES returns validation errors
/// </summary>
public class NphiesValidationException : NphiesException
{
    /// <summary>
    /// List of validation errors
    /// </summary>
    public List<string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Creates a new validation exception
    /// </summary>
    public NphiesValidationException(string message) : base(message)
    {
        ErrorCode = "VALIDATION_ERROR";
    }

    /// <summary>
    /// Creates a new validation exception with error list
    /// </summary>
    public NphiesValidationException(string message, List<string> errors)
        : base(message)
    {
    ErrorCode = "VALIDATION_ERROR";
 ValidationErrors = errors ?? new List<string>();
    }

    /// <summary>
    /// Creates a new validation exception with error code and errors
    /// </summary>
    public NphiesValidationException(string message, string errorCode, List<string> errors)
      : base(message, errorCode)
    {
    ValidationErrors = errors ?? new List<string>();
    }
}
