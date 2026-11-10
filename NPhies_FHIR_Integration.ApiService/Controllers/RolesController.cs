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
    /// API controller for role management
    /// </summary>
 [ApiController]
    [Route("api/[controller]")]
  [Authorize]
    [Produces("application/json")]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;
    private readonly ILogger<RolesController> _logger;

public RolesController(IRoleService roleService, ILogger<RolesController> logger)
        {
    _roleService = roleService;
          _logger = logger;
        }

        /// <summary>
        /// Get all roles
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<ActionResult<List<RoleDto>>> GetAllRoles()
   {
 try
            {
       _logger.LogInformation("Getting all roles");
  var roles = await _roleService.GetAllRolesAsync();
return Ok(roles);
     }
            catch (Exception ex)
   {
  _logger.LogError(ex, "Error getting all roles");
return StatusCode(StatusCodes.Status500InternalServerError, 
        new { message = "Error retrieving roles", error = ex.Message });
 }
      }

        /// <summary>
        /// Get role by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
[ProducesResponseType(StatusCodes.Status401Unauthorized)]
   public async Task<ActionResult<RoleDto>> GetRoleById(int id)
    {
        try
    {
         _logger.LogInformation("Getting role {RoleId}", id);
   var role = await _roleService.GetRoleByIdAsync(id);
        if (role == null)
         return NotFound(new { message = $"Role with ID {id} not found" });
         return Ok(role);
            }
            catch (Exception ex)
   {
       _logger.LogError(ex, "Error getting role {RoleId}", id);
  return StatusCode(StatusCodes.Status500InternalServerError, 
  new { message = "Error retrieving role", error = ex.Message });
   }
   }

        /// <summary>
 /// Get role by name
        /// </summary>
    [HttpGet("byname/{name}")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<RoleDto>> GetRoleByName(string name)
    {
          try
     {
        _logger.LogInformation("Getting role by name: {RoleName}", name);
     var role = await _roleService.GetRoleByNameAsync(name);
    if (role == null)
        return NotFound(new { message = $"Role '{name}' not found" });
             return Ok(role);
          }
       catch (Exception ex)
    {
           _logger.LogError(ex, "Error getting role by name: {RoleName}", name);
 return StatusCode(StatusCodes.Status500InternalServerError, 
new { message = "Error retrieving role", error = ex.Message });
     }
        }

        /// <summary>
        /// Get roles by department
      /// </summary>
        [HttpGet("department/{department}")]
   [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<RoleDto>>> GetRolesByDepartment(string department)
        {
      try
     {
      _logger.LogInformation("Getting roles for department: {Department}", department);
            var roles = await _roleService.GetRolesByDepartmentAsync(department);
         return Ok(roles);
     }
    catch (Exception ex)
            {
   _logger.LogError(ex, "Error getting roles for department: {Department}", department);
         return StatusCode(StatusCodes.Status500InternalServerError, 
          new { message = "Error retrieving roles", error = ex.Message });
     }
        }

   /// <summary>
        /// Get roles by level
        /// </summary>
        [HttpGet("level/{level}")]
  [ProducesResponseType(typeof(List<RoleDto>), StatusCodes.Status200OK)]
        public async Task<ActionResult<List<RoleDto>>> GetRolesByLevel(int level)
        {
            try
       {
                _logger.LogInformation("Getting roles for level: {Level}", level);
        var roles = await _roleService.GetRolesByLevelAsync(level);
       return Ok(roles);
   }
  catch (Exception ex)
  {
                _logger.LogError(ex, "Error getting roles for level: {Level}", level);
         return StatusCode(StatusCodes.Status500InternalServerError, 
      new { message = "Error retrieving roles", error = ex.Message });
 }
        }

        /// <summary>
     /// Create a new role
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_MANAGER")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<ActionResult<RoleDto>> CreateRole([FromBody] CreateUpdateRoleDto dto)
        {
            try
    {
if (!ModelState.IsValid)
          return BadRequest(ModelState);

                _logger.LogInformation("Creating role: {RoleName}", dto.Name);
         var role = await _roleService.CreateRoleAsync(dto);
return CreatedAtAction(nameof(GetRoleById), new { id = role.Id }, role);
            }
 catch (Exception ex)
            {
       _logger.LogError(ex, "Error creating role: {RoleName}", dto.Name);
           return StatusCode(StatusCodes.Status500InternalServerError, 
           new { message = "Error creating role", error = ex.Message });
     }
        }

     /// <summary>
        /// Update a role
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_MANAGER")]
        [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<RoleDto>> UpdateRole(int id, [FromBody] CreateUpdateRoleDto dto)
     {
  try
      {
      if (!ModelState.IsValid)
       return BadRequest(ModelState);

                _logger.LogInformation("Updating role: {RoleId}", id);
        var role = await _roleService.UpdateRoleAsync(id, dto);
         return Ok(role);
            }
      catch (InvalidOperationException ex)
          {
      _logger.LogWarning(ex, "Role not found: {RoleId}", id);
    return NotFound(new { message = ex.Message });
         }
          catch (Exception ex)
   {
           _logger.LogError(ex, "Error updating role: {RoleId}", id);
  return StatusCode(StatusCodes.Status500InternalServerError, 
          new { message = "Error updating role", error = ex.Message });
      }
        }

        /// <summary>
/// Delete a role
      /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_MANAGER")]
 [ProducesResponseType(StatusCodes.Status204NoContent)]
 [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
  public async Task<IActionResult> DeleteRole(int id)
        {
 try
      {
           _logger.LogInformation("Deleting role: {RoleId}", id);
                var result = await _roleService.DeleteRoleAsync(id);
  if (!result)
       return NotFound(new { message = $"Role with ID {id} not found" });
       return NoContent();
         }
            catch (InvalidOperationException ex)
   {
        _logger.LogWarning(ex, "Cannot delete role: {RoleId}", id);
        return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
            }
        catch (Exception ex)
            {
      _logger.LogError(ex, "Error deleting role: {RoleId}", id);
    return StatusCode(StatusCodes.Status500InternalServerError, 
     new { message = "Error deleting role", error = ex.Message });
            }
    }

        /// <summary>
/// Assign permission to role
        /// </summary>
        [HttpPost("{roleId}/permissions/{permissionId}")]
   [Authorize(Roles = "TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_MANAGER")]
        [ProducesResponseType(StatusCodes.Status200OK)]
      [ProducesResponseType(StatusCodes.Status404NotFound)]
   public async Task<IActionResult> AssignPermissionToRole(int roleId, int permissionId)
        {
      try
      {
                _logger.LogInformation("Assigning permission {PermissionId} to role {RoleId}", permissionId, roleId);
      var result = await _roleService.AssignPermissionToRoleAsync(roleId, permissionId);
                if (!result)
  return NotFound(new { message = "Failed to assign permission" });
    return Ok(new { message = "Permission assigned successfully" });
            }
  catch (Exception ex)
            {
                _logger.LogError(ex, "Error assigning permission to role");
        return StatusCode(StatusCodes.Status500InternalServerError, 
         new { message = "Error assigning permission", error = ex.Message });
          }
        }

        /// <summary>
     /// Remove permission from role
        /// </summary>
 [HttpDelete("{roleId}/permissions/{permissionId}")]
  [Authorize(Roles = "TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_MANAGER")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemovePermissionFromRole(int roleId, int permissionId)
        {
  try
            {
   _logger.LogInformation("Removing permission {PermissionId} from role {RoleId}", permissionId, roleId);
        var result = await _roleService.RemovePermissionFromRoleAsync(roleId, permissionId);
                if (!result)
      return NotFound(new { message = "Permission not found for this role" });
     return NoContent();
    }
    catch (Exception ex)
        {
  _logger.LogError(ex, "Error removing permission from role");
     return StatusCode(StatusCodes.Status500InternalServerError, 
         new { message = "Error removing permission", error = ex.Message });
            }
        }

        /// <summary>
   /// Get role permissions
        /// </summary>
     [HttpGet("{roleId}/permissions")]
        [ProducesResponseType(typeof(List<PermissionDto>), StatusCodes.Status200OK)]
  public async Task<ActionResult<List<PermissionDto>>> GetRolePermissions(int roleId)
     {
      try
 {
           _logger.LogInformation("Getting permissions for role {RoleId}", roleId);
var permissions = await _roleService.GetRolePermissionsAsync(roleId);
           return Ok(permissions);
          }
            catch (Exception ex)
            {
       _logger.LogError(ex, "Error getting role permissions");
    return StatusCode(StatusCodes.Status500InternalServerError, 
     new { message = "Error retrieving permissions", error = ex.Message });
            }
        }

        /// <summary>
        /// Get role hierarchy
      /// </summary>
    [HttpGet("hierarchy/{department}")]
        [ProducesResponseType(typeof(RoleHierarchyDto), StatusCodes.Status200OK)]
        public async Task<ActionResult<RoleHierarchyDto>> GetRoleHierarchy(string department)
        {
       try
 {
  _logger.LogInformation("Getting role hierarchy for department: {Department}", department);
      var hierarchy = await _roleService.GetRoleHierarchyAsync(department);
                return Ok(hierarchy);
            }
      catch (Exception ex)
            {
    _logger.LogError(ex, "Error getting role hierarchy");
 return StatusCode(StatusCodes.Status500InternalServerError, 
  new { message = "Error retrieving hierarchy", error = ex.Message });
            }
        }
    }
}
