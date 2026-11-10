namespace NPhies_FHIR_Integration.Application.DTOs;

/// <summary>
/// Standard API response wrapper for successful requests
/// </summary>
/// <typeparam name="T">Data type</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Create successful response
    /// </summary>
    public static ApiResponse<T> SuccessResponse(T data, string message = "Success", int statusCode = 200)
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data,
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Create error response
    /// </summary>
    public static ApiResponse<T> ErrorResponse(string message, int statusCode = 500)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default,
            StatusCode = statusCode
        };
    }
}

/// <summary>
/// Standard API response wrapper for non-generic responses
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Create successful response
    /// </summary>
    public static ApiResponse SuccessResponse(string message = "Success", int statusCode = 200)
    {
        return new ApiResponse
        {
            Success = true,
            Message = message,
            StatusCode = statusCode
        };
    }

    /// <summary>
    /// Create error response
    /// </summary>
    public static ApiResponse ErrorResponse(string message, int statusCode = 500)
    {
        return new ApiResponse
        {
            Success = false,
            Message = message,
            StatusCode = statusCode
        };
    }
}

/// <summary>
/// Paginated API response wrapper
/// </summary>
/// <typeparam name="T">Item type</typeparam>
public class PaginatedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = new List<T>();
    public int PageNumber { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public int TotalPages { get; set; }
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    /// <summary>
    /// Create paginated response
    /// </summary>
    public static PaginatedResponse<T> CreatePaginatedResponse(
          IEnumerable<T> items,
        int pageNumber,
          int pageSize,
   int totalCount)
    {
        var totalPages = (totalCount + pageSize - 1) / pageSize;
        return new PaginatedResponse<T>
        {
            Items = items,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalCount = totalCount,
            TotalPages = totalPages
        };
    }
}

/// <summary>
/// API error response for exceptions
/// </summary>
public class ErrorResponse
{
    public int StatusCode { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ExceptionMessage { get; set; }
    public string? StackTrace { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public IEnumerable<string>? Errors { get; set; }

    /// <summary>
    /// Create error response
    /// </summary>
    public static ErrorResponse CreateErrorResponse(
        int statusCode,
  string message,
        string? exceptionMessage = null,
        string? stackTrace = null,
 IEnumerable<string>? errors = null)
    {
        return new ErrorResponse
        {
            StatusCode = statusCode,
            Message = message,
            ExceptionMessage = exceptionMessage,
            StackTrace = stackTrace,
            Errors = errors
        };
    }
}

/// <summary>
/// Validation error response
/// </summary>
public class ValidationErrorResponse
{
    public int StatusCode { get; set; } = 400;
    public string Message { get; set; } = "Validation failed";
    public Dictionary<string, string[]> Errors { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Pagination query parameters DTO
/// </summary>
public class PaginationParams
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;

    /// <summary>
    /// Validate pagination parameters
    /// </summary>
    public bool Validate(out string error)
    {
        error = string.Empty;

        if (PageNumber < 1)
        {
            error = "Page number must be greater than 0";
            return false;
        }

        if (PageSize < 1)
        {
            error = "Page size must be greater than 0";
            return false;
        }

        if (PageSize > 100)
        {
            error = "Page size cannot exceed 100";
            return false;
        }

        return true;
    }
}

/// <summary>
/// Validation result DTO for rule engine
/// </summary>
public class ValidationResultDto
{
    public bool IsValid { get; set; }
    public int TotalErrors { get; set; }
    public int TotalWarnings { get; set; }
    public List<ValidationErrorDto> Errors { get; set; } = new();
    public List<ValidationErrorDto> Warnings { get; set; } = new();
    public DateTime ValidatedAt { get; set; } = DateTime.UtcNow;
    public string? ClaimId { get; set; }
}

/// <summary>
/// Validation error DTO
/// </summary>
public class ValidationErrorDto
{
    public string RuleId { get; set; } = string.Empty;
    public string Severity { get; set; } = "Warning";
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
