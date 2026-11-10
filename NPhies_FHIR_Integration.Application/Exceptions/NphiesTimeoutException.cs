namespace NPhies_FHIR_Integration.Application.Exceptions;

/// <summary>
/// Exception thrown when a request to NPHIES times out
/// </summary>
public class NphiesTimeoutException : NphiesException
{
    /// <summary>
    /// Timeout duration in seconds
    /// </summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>
    /// Creates a new timeout exception
    /// </summary>
public NphiesTimeoutException(string message) : base(message)
    {
    ErrorCode = "TIMEOUT_ERROR";
 }

    /// <summary>
    /// Creates a new timeout exception with timeout duration
    /// </summary>
    public NphiesTimeoutException(string message, int timeoutSeconds)
        : base(message)
    {
        ErrorCode = "TIMEOUT_ERROR";
        TimeoutSeconds = timeoutSeconds;
    }

    /// <summary>
    /// Creates a new timeout exception with inner exception
    /// </summary>
    public NphiesTimeoutException(string message, Exception innerException)
  : base(message, innerException)
    {
        ErrorCode = "TIMEOUT_ERROR";
    }
}
