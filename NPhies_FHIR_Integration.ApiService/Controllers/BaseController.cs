using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Common.Constants;
using NPhies_FHIR_Integration.Common.Models;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Base controller for all API controllers
/// Provides common methods and response patterns
/// </summary>
[ApiController]
public abstract class BaseController : ControllerBase
{
    /// <summary>
    /// Returns 200 OK with optional message parameter
    /// </summary>
    protected new IActionResult Ok<T>(T data, string message = null)
    {
        if (string.IsNullOrEmpty(message))
          return base.Ok(data);
        
        return base.Ok(new { data, message });
    }

    /// <summary>
    /// Returns 400 Bad Request with optional message and errors
    /// </summary>
    protected new IActionResult BadRequest(string message, List<string> errors = null)
    {
     var response = new { message, errors = errors ?? new List<string>() };
        return base.BadRequest(response);
    }

    /// <summary>
    /// Returns 404 Not Found with optional message
    /// </summary>
  protected new IActionResult NotFound(string message = "Resource not found")
    {
        return base.NotFound(new { message });
    }

    /// <summary>
    /// Returns 500 Internal Server Error with optional message
    /// </summary>
    protected IActionResult InternalServerError(string message = "An internal server error occurred")
    {
        return StatusCode(StatusCodes.Status500InternalServerError, new { message });
    }

    /// <summary>
    /// Returns 201 Created response
    /// </summary>
    protected IActionResult Created<T>(string location, T data, string message = null)
    {
        var response = string.IsNullOrEmpty(message) 
            ? (object)data 
       : new { data, message };
      return base.Created(location, response);
    }
}
