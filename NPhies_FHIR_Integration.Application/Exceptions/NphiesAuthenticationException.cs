namespace NPhies_FHIR_Integration.Application.Exceptions;

/// <summary>
/// Exception thrown when authentication with NPHIES fails
/// </summary>
public class NphiesAuthenticationException : NphiesException
{
    /// <summary>
/// Creates a new authentication exception
    /// </summary>
    public NphiesAuthenticationException(string message) : base(message)
    {
    ErrorCode = "AUTH_ERROR";
    }

    /// <summary>
    /// Creates a new authentication exception with inner exception
    /// </summary>
    public NphiesAuthenticationException(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = "AUTH_ERROR";
    }

    /// <summary>
    /// Creates a new authentication exception with specific error code
    /// </summary>
    public NphiesAuthenticationException(string message, string errorCode)
        : base(message, errorCode)
    {
    }
}
