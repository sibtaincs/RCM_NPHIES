using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.ApiService.Security;
using NPhies_FHIR_Integration.ApiService.Security.Authorization;
using NPhies_FHIR_Integration.Domain.Entities.Access;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Data-driven access control: manage roles, permissions and role-permission grants.
/// Roles created here can be assigned to users through <c>/api/users</c>.
/// </summary>
[ApiController]
[Route("api/access")]
[Authorize]
[Produces("application/json")]
public class AccessControlController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AccessControlController> _logger;

    public AccessControlController(ApplicationDbContext db, ILogger<AccessControlController> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
   _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    // ===================== Roles =====================

    /// <summary>All roles with their granted permission names.</summary>
    [HttpGet("roles")]
  [HasPermission(Permissions.RolesRead)]
    [ProducesResponseType(typeof(IEnumerable<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles()
    {
        var roles = await _db.AppRoles
    .AsNoTracking()
   .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
   .OrderBy(r => r.Name)
     .ToListAsync();

     return Ok(roles.Select(ToRoleResponse));
    }

/// <summary>A single role.</summary>
    [HttpGet("roles/{id}")]
    [HasPermission(Permissions.RolesRead)]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRole(string id)
    {
     var role = await _db.AppRoles.AsNoTracking()
     .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
    .FirstOrDefaultAsync(r => r.Id == id);

  return role is null
      ? NotFound(new { message = $"Role {id} not found" })
     : Ok(ToRoleResponse(role));
    }

 /// <summary>Create a custom role.</summary>
    [HttpPost("roles")]
    [HasPermission(Permissions.RolesManage)]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateRole([FromBody] SaveRoleRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
      return BadRequest(new { message = "Role name is required" });

        var name = dto.Name.Trim();
        if (await _db.AppRoles.AnyAsync(r => r.Name == name))
     return BadRequest(new { message = $"A role named '{name}' already exists" });

        var role = new AppRole
        {
     Name = name,
   DisplayName = string.IsNullOrWhiteSpace(dto.DisplayName) ? name : dto.DisplayName.Trim(),
      Description = dto.Description?.Trim(),
   IsSystemRole = false
    };

  _db.AppRoles.Add(role);
        await _db.SaveChangesAsync();

    await ReplaceGrantsAsync(role.Id, dto.PermissionIds);

    _logger.LogInformation("Role '{Role}' created by {User}", role.Name, User.Identity?.Name);
        return CreatedAtAction(nameof(GetRole), new { id = role.Id }, await LoadRoleResponseAsync(role.Id));
  }

 /// <summary>
    /// Update a role. System roles keep their <c>Name</c> but may be re-described
    /// and re-permissioned.
    /// </summary>
    [HttpPut("roles/{id}")]
    [HasPermission(Permissions.RolesManage)]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateRole(string id, [FromBody] SaveRoleRequest dto)
    {
        var role = await _db.AppRoles.FirstOrDefaultAsync(r => r.Id == id);
     if (role is null) return NotFound(new { message = $"Role {id} not found" });

  if (!role.IsSystemRole && !string.IsNullOrWhiteSpace(dto.Name))
        {
var name = dto.Name.Trim();
   if (await _db.AppRoles.AnyAsync(r => r.Name == name && r.Id != id))
     return BadRequest(new { message = $"A role named '{name}' already exists" });
        role.Name = name;
      }

   if (!string.IsNullOrWhiteSpace(dto.DisplayName)) role.DisplayName = dto.DisplayName.Trim();
      role.Description = dto.Description?.Trim();
        if (dto.IsActive.HasValue && !role.IsSystemRole) role.IsActive = dto.IsActive.Value;
        role.UpdatedAt = DateTime.UtcNow;

    await _db.SaveChangesAsync();

  if (dto.PermissionIds is not null)
   await ReplaceGrantsAsync(role.Id, dto.PermissionIds);

        return Ok(await LoadRoleResponseAsync(role.Id));
    }

    /// <summary>Delete a custom role. System roles and roles in use are protected.</summary>
    [HttpDelete("roles/{id}")]
    [HasPermission(Permissions.RolesManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DeleteRole(string id)
  {
    var role = await _db.AppRoles.FirstOrDefaultAsync(r => r.Id == id);
        if (role is null) return NotFound(new { message = $"Role {id} not found" });

        if (role.IsSystemRole)
       return BadRequest(new { message = "System roles cannot be deleted." });

        // Block deletion while any active user still holds the role.
        var holders = await _db.Users
            .Where(u => u.DeletedAt == null)
      .Select(u => u.Roles)
     .ToListAsync();

        if (holders.Any(r => r.Contains(role.Name)))
  return BadRequest(new { message = $"Role '{role.Name}' is still assigned to one or more users." });

     _db.AppRoles.Remove(role);
 await _db.SaveChangesAsync();
        return NoContent();
  }

    // ===================== Permissions =====================

    /// <summary>All permissions.</summary>
    [HttpGet("permissions")]
    [HasPermission(Permissions.RolesRead)]
    [ProducesResponseType(typeof(IEnumerable<PermissionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissions()
    {
    var permissions = await _db.AppPermissions.AsNoTracking()
     .OrderBy(p => p.Category).ThenBy(p => p.Name)
   .ToListAsync();

      return Ok(permissions.Select(ToPermissionResponse));
    }

    /// <summary>Create a custom permission.</summary>
    [HttpPost("permissions")]
    [HasPermission(Permissions.RolesManage)]
    [ProducesResponseType(typeof(PermissionResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePermission([FromBody] SavePermissionRequest dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name))
    return BadRequest(new { message = "Permission name is required" });
        if (string.IsNullOrWhiteSpace(dto.Category))
      return BadRequest(new { message = "Category is required" });

     var name = dto.Name.Trim().ToLowerInvariant();
        if (await _db.AppPermissions.AnyAsync(p => p.Name == name))
   return BadRequest(new { message = $"Permission '{name}' already exists" });

        var permission = new AppPermission
        {
        Name = name,
   Category = dto.Category.Trim(),
   Action = dto.Action?.Trim() ?? name.Split(':').LastOrDefault() ?? "read",
     Description = dto.Description?.Trim(),
    IsSystemPermission = false
        };

   _db.AppPermissions.Add(permission);
   await _db.SaveChangesAsync();

     return CreatedAtAction(nameof(GetPermissions), null, ToPermissionResponse(permission));
    }

    /// <summary>Update a permission's descriptive fields.</summary>
  [HttpPut("permissions/{id}")]
    [HasPermission(Permissions.RolesManage)]
    [ProducesResponseType(typeof(PermissionResponse), StatusCodes.Status200OK)]
  public async Task<IActionResult> UpdatePermission(string id, [FromBody] SavePermissionRequest dto)
    {
   var permission = await _db.AppPermissions.FirstOrDefaultAsync(p => p.Id == id);
    if (permission is null) return NotFound(new { message = $"Permission {id} not found" });

    if (!permission.IsSystemPermission && !string.IsNullOrWhiteSpace(dto.Name))
   {
      var name = dto.Name.Trim().ToLowerInvariant();
        if (await _db.AppPermissions.AnyAsync(p => p.Name == name && p.Id != id))
    return BadRequest(new { message = $"Permission '{name}' already exists" });
   permission.Name = name;
    }

        if (!string.IsNullOrWhiteSpace(dto.Category)) permission.Category = dto.Category.Trim();
        if (!string.IsNullOrWhiteSpace(dto.Action)) permission.Action = dto.Action.Trim();
        permission.Description = dto.Description?.Trim();
        permission.UpdatedAt = DateTime.UtcNow;

     await _db.SaveChangesAsync();
  return Ok(ToPermissionResponse(permission));
    }

 /// <summary>Delete a custom permission.</summary>
    [HttpDelete("permissions/{id}")]
    [HasPermission(Permissions.RolesManage)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeletePermission(string id)
    {
        var permission = await _db.AppPermissions.FirstOrDefaultAsync(p => p.Id == id);
        if (permission is null) return NotFound(new { message = $"Permission {id} not found" });

    if (permission.IsSystemPermission)
     return BadRequest(new { message = "System permissions cannot be deleted." });

  _db.AppPermissions.Remove(permission);
        await _db.SaveChangesAsync();
    return NoContent();
    }

    // ===================== Grants =====================

    /// <summary>Replace the full permission set of a role (used by the matrix UI).</summary>
    [HttpPut("roles/{id}/permissions")]
    [HasPermission(Permissions.RolesManage)]
    [ProducesResponseType(typeof(RoleResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetRolePermissions(string id, [FromBody] SetPermissionsRequest dto)
    {
        if (!await _db.AppRoles.AnyAsync(r => r.Id == id))
      return NotFound(new { message = $"Role {id} not found" });

        await ReplaceGrantsAsync(id, dto.PermissionIds ?? []);
      return Ok(await LoadRoleResponseAsync(id));
    }

    /// <summary>The effective permissions for a user, derived from their roles.</summary>
  [HttpGet("users/{userId}/effective-permissions")]
    [HasPermission(Permissions.UsersRead)]
    [ProducesResponseType(typeof(EffectiveAccessResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEffectivePermissions(string userId)
    {
      var user = await _db.Users.AsNoTracking()
 .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);

    if (user is null) return NotFound(new { message = $"User {userId} not found" });

        var roleNames = user.Roles ?? [];

  var permissions = await _db.AppRolePermissions.AsNoTracking()
     .Where(rp => rp.Role != null && roleNames.Contains(rp.Role.Name))
     .Select(rp => new { rp.Permission!.Name, rp.Permission.Category, RoleName = rp.Role!.Name })
     .ToListAsync();

        return Ok(new EffectiveAccessResponse
     {
       UserId = user.Id,
   Username = user.Username,
   Roles = roleNames,
   Permissions = permissions
      .GroupBy(p => p.Name)
      .Select(g => new EffectivePermission
{
    Name = g.Key,
     Category = g.First().Category,
     GrantedByRoles = g.Select(x => x.RoleName).Distinct().ToList()
   })
     .OrderBy(p => p.Category).ThenBy(p => p.Name)
  .ToList()
      });
    }

    // ===================== Helpers =====================

    private async Task ReplaceGrantsAsync(string roleId, List<string>? permissionIds)
 {
        var current = await _db.AppRolePermissions.Where(rp => rp.RoleId == roleId).ToListAsync();
    _db.AppRolePermissions.RemoveRange(current);

  if (permissionIds is { Count: > 0 })
        {
       // Only persist ids that actually exist.
 var valid = await _db.AppPermissions
      .Where(p => permissionIds.Contains(p.Id))
     .Select(p => p.Id)
     .ToListAsync();

    _db.AppRolePermissions.AddRange(valid.Select(pid => new AppRolePermission
      {
        RoleId = roleId,
    PermissionId = pid,
    GrantedBy = User.Identity?.Name ?? "admin"
    }));
   }

      await _db.SaveChangesAsync();
    }

private async Task<RoleResponse> LoadRoleResponseAsync(string roleId)
    {
        var role = await _db.AppRoles.AsNoTracking()
      .Include(r => r.RolePermissions).ThenInclude(rp => rp.Permission)
       .FirstAsync(r => r.Id == roleId);

        return ToRoleResponse(role);
    }

    private static RoleResponse ToRoleResponse(AppRole r) => new()
    {
        Id = r.Id,
        Name = r.Name,
        DisplayName = r.DisplayName,
        Description = r.Description,
  IsSystemRole = r.IsSystemRole,
        IsActive = r.IsActive,
        PermissionIds = r.RolePermissions.Select(rp => rp.PermissionId).ToList(),
        PermissionNames = r.RolePermissions
    .Where(rp => rp.Permission != null)
    .Select(rp => rp.Permission!.Name)
      .OrderBy(n => n)
       .ToList()
    };

    private static PermissionResponse ToPermissionResponse(AppPermission p) => new()
  {
        Id = p.Id,
        Name = p.Name,
    Category = p.Category,
     Action = p.Action,
        Description = p.Description,
        IsSystemPermission = p.IsSystemPermission
    };
}

// ===================== Contracts =====================

public class RoleResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemRole { get; set; }
    public bool IsActive { get; set; }
    public List<string> PermissionIds { get; set; } = [];
    public List<string> PermissionNames { get; set; } = [];
}

public class SaveRoleRequest
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool? IsActive { get; set; }
    public List<string>? PermissionIds { get; set; }
}

public class PermissionResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsSystemPermission { get; set; }
}

public class SavePermissionRequest
{
    public string? Name { get; set; }
  public string? Category { get; set; }
    public string? Action { get; set; }
    public string? Description { get; set; }
}

public class SetPermissionsRequest
{
    public List<string>? PermissionIds { get; set; }
}

public class EffectivePermission
{
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public List<string> GrantedByRoles { get; set; } = [];
}

public class EffectiveAccessResponse
{
    public string UserId { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public List<string> Roles { get; set; } = [];
    public List<EffectivePermission> Permissions { get; set; } = [];
}
