using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.Services.RBAC;
using NPhies_FHIR_Integration.Domain.DTOs;

namespace NPhies_FHIR_Integration.ApiService.Controllers
{
    /// <summary>
    /// API controller for user role management and permission checking
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
   [Authorize]
    [Produces("application/json")]
    public class UserRolesController : ControllerBase
    {
        private readonly IUserRoleService _userRoleService;
       private readonly ILogger<UserRolesController> _logger;

        public UserRolesController(IUserRoleService userRoleService, ILogger<UserRolesController> logger)
        {
     _userRoleService = userRoleService;
            _logger = logger;
        }

        /// <summary>
        /// Get all roles for a user
        /// </summary>
        [HttpGet("user/{userId}")]
     [ProducesResponseType(typeof(List<UserRoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<UserRoleDto>>> GetUserRoles(int userId)
        {
    try
            {
        _logger.LogInformation("Getting roles for user {UserId}", userId);
       var roles = await _userRoleService.GetUserRolesAsync(userId);
       return Ok(roles);
    }
    catch (Exception ex)
            {
     _logger.LogError(ex, "Error getting user roles");
    return StatusCode(StatusCodes.Status500InternalServerError, 
 new { message = "Error retrieving user roles", error = ex.Message });
       }
  }

   /// <summary>
     /// Get primary role for a user
        /// </summary>
    [HttpGet("user/{userId}/primary")]
        [ProducesResponseType(typeof(UserRoleDto), StatusCodes.Status200OK)]
   [ProducesResponseType(StatusCodes.Status404NotFound)]
   public async Task<ActionResult<UserRoleDto>> GetUserPrimaryRole(int userId)
        {
           try
 {
          _logger.LogInformation("Getting primary role for user {UserId}", userId);
  var role = await _userRoleService.GetPrimaryRoleAsync(userId);
 if (role == null)
        return NotFound(new { message = $"No primary role found for user {userId}" });
    return Ok(role);
   }
  catch (Exception ex)
    {
   _logger.LogError(ex, "Error getting user primary role");
    return StatusCode(StatusCodes.Status500InternalServerError, 
 new { message = "Error retrieving primary role", error = ex.Message });
 }
        }

        /// <summary>
 /// Get user permissions
      /// </summary>
 [HttpGet("user/{userId}/permissions")]
  [ProducesResponseType(typeof(List<int>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<int>>> GetUserPermissions(int userId)
        {
            try
   {
        _logger.LogInformation("Getting permissions for user {UserId}", userId);
        var permissions = await _userRoleService.GetUserPermissionsAsync(userId);
    return Ok(permissions);
        }
 catch (Exception ex)
   {
        _logger.LogError(ex, "Error getting user permissions");
         return StatusCode(StatusCodes.Status500InternalServerError, 
        new { message = "Error retrieving permissions", error = ex.Message });
 }
        }

        /// <summary>
        /// Assign role to user
 /// </summary>
        [HttpPost("assign")]
    [Authorize(Roles = "TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_MANAGER")]
        [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status400BadRequest)]
     public async Task<IActionResult> AssignRoleToUser([FromBody] AssignRoleDto dto)
        {
     try
     {
         if (!ModelState.IsValid)
         return BadRequest(ModelState);

         _logger.LogInformation("Assigning role {RoleId} to user {UserId}", dto.RoleId, dto.UserId);
    var result = await _userRoleService.AssignRoleToUserAsync(dto.UserId, dto.RoleId, dto.IsPrimary);
      if (!result)
    return BadRequest(new { message = "Failed to assign role" });
        return Ok(new { message = "Role assigned successfully" });
    }
   catch (Exception ex)
            {
   _logger.LogError(ex, "Error assigning role to user");
     return StatusCode(StatusCodes.Status500InternalServerError, 
     new { message = "Error assigning role", error = ex.Message });
           }
        }

    /// <summary>
  /// Remove role from user
   /// </summary>
        [HttpPost("remove")]
 [Authorize(Roles = "TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_MANAGER")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
      public async Task<IActionResult> RemoveRoleFromUser([FromBody] AssignRoleDto dto)
    {
      try
     {
        _logger.LogInformation("Removing role {RoleId} from user {UserId}", dto.RoleId, dto.UserId);
  var result = await _userRoleService.RemoveRoleFromUserAsync(dto.UserId, dto.RoleId);
            if (!result)
   return BadRequest(new { message = "Failed to remove role" });
        return Ok(new { message = "Role removed successfully" });
            }
      catch (Exception ex)
  {
     _logger.LogError(ex, "Error removing role from user");
           return StatusCode(StatusCodes.Status500InternalServerError, 
            new { message = "Error removing role", error = ex.Message });
 }
        }

    /// <summary>
        /// Change user's primary role
   /// </summary>
       [HttpPost("change-primary")]
      [Authorize(Roles = "TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_MANAGER")]
  [ProducesResponseType(StatusCodes.Status200OK)]
   [ProducesResponseType(StatusCodes.Status400BadRequest)]
       public async Task<IActionResult> ChangePrimaryRole([FromBody] AssignRoleDto dto)
        {
   try
     {
    _logger.LogInformation("Changing primary role to {RoleId} for user {UserId}", dto.RoleId, dto.UserId);
      var result = await _userRoleService.ChangePrimaryRoleAsync(dto.UserId, dto.RoleId);
     if (!result)
          return BadRequest(new { message = "Failed to change primary role" });
         return Ok(new { message = "Primary role changed successfully" });
      }
    catch (Exception ex)
         {
 _logger.LogError(ex, "Error changing primary role");
   return StatusCode(StatusCodes.Status500InternalServerError, 
       new { message = "Error changing primary role", error = ex.Message });
           }
        }

    /// <summary>
        /// Check if user has permission
  /// </summary>
   [HttpGet("user/{userId}/has-permission/{permission}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<ActionResult<bool>> HasPermission(int userId, string permission)
    {
      try
     {
     _logger.LogInformation("Checking if user {UserId} has permission {Permission}", userId, permission);
      var hasPermission = await _userRoleService.HasPermissionAsync(userId, permission);
         return Ok(hasPermission);
            }
  catch (Exception ex)
            {
      _logger.LogError(ex, "Error checking permission");
    return StatusCode(StatusCodes.Status500InternalServerError, 
     new { message = "Error checking permission", error = ex.Message });
            }
        }

        /// <summary>
       /// Check if user has all permissions
       /// </summary>
       [HttpPost("user/{userId}/has-all-permissions")]
       [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<ActionResult<bool>> HasAllPermissions(int userId, [FromBody] List<string> permissions)
        {
 try
         {
     _logger.LogInformation("Checking if user {UserId} has all permissions", userId);
      var hasPermissions = await _userRoleService.HasAllPermissionsAsync(userId, permissions);
          return Ok(hasPermissions);
   }
        catch (Exception ex)
       {
 _logger.LogError(ex, "Error checking permissions");
        return StatusCode(StatusCodes.Status500InternalServerError, 
 new { message = "Error checking permissions", error = ex.Message });
       }
  }

       /// <summary>
        /// Check if user has any permission
   /// </summary>
       [HttpPost("user/{userId}/has-any-permission")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
     public async Task<ActionResult<bool>> HasAnyPermission(int userId, [FromBody] List<string> permissions)
  {
            try
     {
  _logger.LogInformation("Checking if user {UserId} has any permission", userId);
      var hasPermission = await _userRoleService.HasAnyPermissionAsync(userId, permissions);
 return Ok(hasPermission);
}
            catch (Exception ex)
    {
  _logger.LogError(ex, "Error checking permissions");
           return StatusCode(StatusCodes.Status500InternalServerError, 
    new { message = "Error checking permissions", error = ex.Message });
      }
        }

       /// <summary>
        /// Check if user has role
       /// </summary>
        [HttpGet("user/{userId}/has-role/{roleName}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
        public async Task<ActionResult<bool>> HasRole(int userId, string roleName)
        {
  try
      {
       _logger.LogInformation("Checking if user {UserId} has role {RoleName}", userId, roleName);
        var hasRole = await _userRoleService.HasRoleAsync(userId, roleName);
          return Ok(hasRole);
          }
      catch (Exception ex)
         {
  _logger.LogError(ex, "Error checking role");
      return StatusCode(StatusCodes.Status500InternalServerError, 
         new { message = "Error checking role", error = ex.Message });
  }
        }

        /// <summary>
     /// Check if user has minimum level
        /// </summary>
        [HttpGet("user/{userId}/has-level/{level}")]
        [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
 public async Task<ActionResult<bool>> HasRoleLevel(int userId, int level)
        {
     try
       {
       _logger.LogInformation("Checking if user {UserId} has level {Level}", userId, level);
       var hasLevel = await _userRoleService.HasRoleLevelAsync(userId, level);
  return Ok(hasLevel);
     }
      catch (Exception ex)
       {
  _logger.LogError(ex, "Error checking level");
return StatusCode(StatusCodes.Status500InternalServerError, 
         new { message = "Error checking level", error = ex.Message });
      }
        }

    /// <summary>
    /// Get user roles with details
        /// </summary>
    [HttpGet("user/{userId}/details")]
  [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<RoleDto>>> GetUserRolesDetail(int userId)
  {
       try
        {
     _logger.LogInformation("Getting role details for user {UserId}", userId);
var roles = await _userRoleService.GetUserRolesDetailAsync(userId);
       return Ok(roles);
           }
   catch (Exception ex)
       {
        _logger.LogError(ex, "Error getting role details");
     return StatusCode(StatusCodes.Status500InternalServerError, 
         new { message = "Error retrieving role details", error = ex.Message });
    }
        }
  }
}
