namespace NPhies_FHIR_Integration.Common.Models;

/// <summary>
/// API Response wrapper for consistent API responses
/// </summary>
/// <typeparam name="T">Data type</typeparam>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ApiResponse<T> SuccessResponse(T data, string message = "Operation successful")
    {
      return new ApiResponse<T>
        {
        Success = true,
            Message = message,
            Data = data
        };
    }

    public static ApiResponse<T> ErrorResponse(string message, List<string>? errors = null)
    {
        return new ApiResponse<T>
        {
            Success = false,
           Message = message,
          Errors = errors ?? new List<string>()
        };
   }
}

/// <summary>
/// Non-generic API Response
/// </summary>
public class ApiResponse
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();

    public static ApiResponse SuccessResponse(string message = "Operation successful")
    {
        return new ApiResponse
     {
  Success = true,
  Message = message
   };
    }

    public static ApiResponse ErrorResponse(string message, List<string>? errors = null)
 {
        return new ApiResponse
        {
       Success = false,
          Message = message,
        Errors = errors ?? new List<string>()
        };
   }
}
