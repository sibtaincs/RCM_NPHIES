using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Common.Constants;
using NPhies_FHIR_Integration.Common.Models;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Health check endpoint
/// </summary>
[Route(AppConstants.ApiRoutePrefix + "/[controller]")]
[ApiController]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult GetHealth()
    {
        var response = ApiResponse.SuccessResponse("API is healthy and running");
        return Ok(response);
    }

    [HttpGet("version")]
    public IActionResult GetVersion()
    {
        return Ok(new { version = AppConstants.ApiVersion, timestamp = DateTime.UtcNow });
    }
}
