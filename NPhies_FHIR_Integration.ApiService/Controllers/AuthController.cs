using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.ApiService.Security.Models;
using NPhies_FHIR_Integration.ApiService.Security.Services;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Authentication controller for login, logout, and token management
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
     IAuthenticationService authenticationService,
        ILogger<AuthController> logger)
    {
     _authenticationService = authenticationService;
 _logger = logger;
    }

    /// <summary>
    /// User login endpoint
    /// </summary>
    /// <param name="request">Login request with username and password</param>
    /// <returns>JWT token and user information</returns>
    [HttpPost("login")]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(LoginResponse), StatusCodes.Status401Unauthorized)]
   public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
 if (!ModelState.IsValid)
 {
   var errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage)).ToList();
       _logger.LogWarning("Login request validation failed");
      return BadRequest(new LoginResponse
   {
        Success = false,
Message = "Invalid request",
   });
 }

        var response = await _authenticationService.LoginAsync(request);
     if (!response.Success)
     {
            return Unauthorized(response);
        }

 return Ok(response);
    }

  /// <summary>
    /// Refresh access token using refresh token
    /// </summary>
    /// <param name="request">Refresh token request</param>
    /// <returns>New JWT token</returns>
    [HttpPost("refresh")]
    [ProducesResponseType(typeof(ApiResponse<JwtTokenModel>), StatusCodes.Status200OK)]
  [ProducesResponseType(typeof(ApiResponse<JwtTokenModel>), StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
    {
    if (!ModelState.IsValid)
        {
         _logger.LogWarning("Refresh token request validation failed");
           return BadRequest(new ApiResponse
  {
  Success = false,
        Message = "Invalid request"
 });
 }

  var response = await _authenticationService.RefreshTokenAsync(request);
        if (!response.Success)
        {
        return Unauthorized(response);
    }

 return Ok(response);
    }

    /// <summary>
  /// User logout endpoint
    /// </summary>
    /// <returns>Logout response</returns>
    [HttpPost("logout")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status401Unauthorized)]
 public async Task<IActionResult> Logout()
    {
    var userId = User.FindFirst("sub")?.Value;
   if (string.IsNullOrEmpty(userId))
      {
           return Unauthorized();
        }

 var response = await _authenticationService.LogoutAsync(userId);
        if (!response.Success)
        {
         return BadRequest(response);
        }

  return Ok(response);
    }

 /// <summary>
    /// Get current user information
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    [Microsoft.AspNetCore.Authorization.Authorize]
  [ProducesResponseType(typeof(ApiResponse<UserSecurityContext>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
  public async Task<IActionResult> GetCurrentUser()
    {
 var userId = User.FindFirst("sub")?.Value;
    if (string.IsNullOrEmpty(userId))
     {
   return Unauthorized();
    }

        var user = await _authenticationService.GetUserContextAsync(userId);
       if (user == null)
      {
  return NotFound(new ApiResponse
        {
   Success = false,
 Message = "User not found"
          });
        }

       return Ok(new ApiResponse<UserSecurityContext>
        {
     Success = true,
  Message = "User retrieved successfully",
    Data = user
        });
   }

   /// <summary>
   /// Health check endpoint for authentication service
  /// </summary>
    /// <returns>Health status</returns>
    [HttpGet("health")]
   [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public IActionResult Health()
  {
        return Ok(new
    {
    status = "healthy",
          service = "Authentication Service",
          timestamp = DateTime.UtcNow
       });
    }
}
