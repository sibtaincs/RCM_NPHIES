using System.Net;

namespace NPhies_FHIR_Integration.Application.Exceptions;

/// <summary>
/// Base exception for all NPHIES-related errors
/// </summary>
public class NphiesException : Exception
{
    /// <summary>
    /// NPHIES error code (if available)
    /// </summary>
    public string ErrorCode { get; set; } = string.Empty;

    /// <summary>
    /// HTTP status code (if applicable)
    /// </summary>
    public HttpStatusCode? StatusCode { get; set; }

/// <summary>
    /// Creates a new NPHIES exception
    /// </summary>
    public NphiesException(string message) : base(message)
    {
  }

    /// <summary>
    /// Creates a new NPHIES exception with inner exception
    /// </summary>
    public NphiesException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Creates a new NPHIES exception with error code
    /// </summary>
    public NphiesException(string message, string errorCode)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    /// <summary>
    /// Creates a new NPHIES exception with error code and status code
    /// </summary>
    public NphiesException(string message, string errorCode, HttpStatusCode statusCode)
        : base(message)
    {
        ErrorCode = errorCode;
        StatusCode = statusCode;
    }
}
