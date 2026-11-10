using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Domain.DTOs;
using NPhies_FHIR_Integration.Domain.Interfaces;

namespace NPhies_FHIR_Integration.Application.Services.RBAC
{
    /// <summary>
    /// Implementation of user role assignment and permission checking service
    /// </summary>
    public class UserRoleService : IUserRoleService
    {
   private readonly IRepository<User> _userRepository;
      private readonly IRepository<UserRole> _userRoleRepository;
  private readonly IRepository<Role> _roleRepository;
  private readonly IRepository<Permission> _permissionRepository;
        private readonly IRepository<RolePermission> _rolePermissionRepository;
   private readonly IPermissionAuditService _auditService;

        public UserRoleService(
       IRepository<User> userRepository,
 IRepository<UserRole> userRoleRepository,
     IRepository<Role> roleRepository,
  IRepository<Permission> permissionRepository,
       IRepository<RolePermission> rolePermissionRepository,
    IPermissionAuditService auditService)
        {
          _userRepository = userRepository;
   _userRoleRepository = userRoleRepository;
        _roleRepository = roleRepository;
     _permissionRepository = permissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
_auditService = auditService;
        }

        #region User Role Assignment

 public async Task<List<UserRoleDto>> GetUserRolesAsync(int userId)
      {
  var userRoles = await _userRoleRepository.GetAllAsync();
var userRolesList = userRoles
   .Where(ur => ur.UserId == userId && ur.RemovedAt == null)
            .ToList();

          var result = new List<UserRoleDto>();
 foreach (var ur in userRolesList)
    {
      var role = await _roleRepository.GetByIdAsync(ur.RoleId.ToString());
   result.Add(MapToDto(ur, role));
       }

    return result;
        }

        public async Task<UserRoleDto> GetPrimaryRoleAsync(int userId)
        {
    var userRoles = await _userRoleRepository.GetAllAsync();
    var primaryRole = userRoles.FirstOrDefault(ur =>
  ur.UserId == userId && ur.IsPrimary && ur.RemovedAt == null);

        if (primaryRole == null)
      return null;

      var role = await _roleRepository.GetByIdAsync(primaryRole.RoleId.ToString());
      return MapToDto(primaryRole, role);
        }

      public async Task<List<int>> GetUserPermissionsAsync(int userId)
        {
  var userRoles = await GetUserRolesAsync(userId);
       var permissionIds = new HashSet<int>();

        foreach (var userRole in userRoles)
     {
     var rolePermissions = await _rolePermissionRepository.GetAllAsync();
  var perms = rolePermissions
     .Where(rp => rp.RoleId == userRole.RoleId)
       .Select(rp => rp.PermissionId)
 .ToList();

   foreach (var perm in perms)
      permissionIds.Add(perm);
    }

    return permissionIds.ToList();
   }

        public async Task<bool> AssignRoleToUserAsync(int userId, int roleId, bool isPrimary = false)
      {
// Check if user already has this role
  var userRoles = await _userRoleRepository.GetAllAsync();
   var existing = userRoles.FirstOrDefault(ur =>
    ur.UserId == userId && ur.RoleId == roleId && ur.RemovedAt == null);

            if (existing != null)
     return false; // Already assigned

      // If this is primary, remove primary status from other roles
          if (isPrimary)
     {
      var primaryRoles = userRoles
      .Where(ur => ur.UserId == userId && ur.IsPrimary && ur.RemovedAt == null)
  .ToList();

   foreach (var pr in primaryRoles)
     {
        pr.IsPrimary = false;
            _userRoleRepository.Update(pr);
            }
 await _userRoleRepository.SaveChangesAsync();
        }

          var userRole = new UserRole
     {
  UserId = userId,
       RoleId = roleId,
      IsPrimary = isPrimary,
  AssignedAt = DateTime.UtcNow
  };

        var result = await _userRoleRepository.AddAsync(userRole);
  await _userRoleRepository.SaveChangesAsync();

    // Audit log
      var role = await _roleRepository.GetByIdAsync(roleId.ToString());
await _auditService.LogRoleAssignmentAsync(userId, roleId, true);

      return result != null;
        }

  public async Task<bool> RemoveRoleFromUserAsync(int userId, int roleId)
   {
   var userRoles = await _userRoleRepository.GetAllAsync();
 var userRole = userRoles.FirstOrDefault(ur =>
      ur.UserId == userId && ur.RoleId == roleId && ur.RemovedAt == null);

      if (userRole == null)
      return false;

     userRole.RemovedAt = DateTime.UtcNow;
 _userRoleRepository.Update(userRole);
  await _userRoleRepository.SaveChangesAsync();

  // Audit log
       await _auditService.LogRoleAssignmentAsync(userId, roleId, false);

          return true;
}

    public async Task<bool> ChangePrimaryRoleAsync(int userId, int newRoleId)
      {
 var userRoles = await _userRoleRepository.GetAllAsync();
            var activeRoles = userRoles.Where(ur =>
      ur.UserId == userId && ur.RemovedAt == null).ToList();

      // Check if user has the new role
            if (!activeRoles.Any(ur => ur.RoleId == newRoleId))
     return false;

     // Remove primary from old roles
 var primaryRoles = activeRoles.Where(ur => ur.IsPrimary).ToList();
            foreach (var pr in primaryRoles)
    {
        pr.IsPrimary = false;
    _userRoleRepository.Update(pr);
         }
     await _userRoleRepository.SaveChangesAsync();

       // Set new primary role
        var newPrimary = activeRoles.First(ur => ur.RoleId == newRoleId);
         newPrimary.IsPrimary = true;
    _userRoleRepository.Update(newPrimary);
       await _userRoleRepository.SaveChangesAsync();

 return true;
   }

        #endregion

        #region Permission Checks

        public async Task<bool> HasPermissionAsync(int userId, string permissionName)
        {
            var permissions = await GetUserPermissionsAsync(userId);
        var permissionRecords = await _permissionRepository.GetAllAsync();

       var permissionRecord = permissionRecords.FirstOrDefault(p => p.Name == permissionName);
       if (permissionRecord == null)
        return false;

            var hasPermission = permissions.Contains(int.Parse(permissionRecord.Id));

// Audit log
       await _auditService.LogPermissionCheckAsync(userId, permissionName, hasPermission);

  return hasPermission;
        }

     public async Task<bool> HasAllPermissionsAsync(int userId, List<string> permissionNames)
        {
   foreach (var permission in permissionNames)
  {
         if (!await HasPermissionAsync(userId, permission))
    return false;
 }
       return true;
   }

      public async Task<bool> HasAnyPermissionAsync(int userId, List<string> permissionNames)
  {
 foreach (var permission in permissionNames)
  {
       if (await HasPermissionAsync(userId, permission))
   return true;
   }
            return false;
        }

      public async Task<bool> HasRoleAsync(int userId, string roleName)
  {
         var userRoles = await GetUserRolesAsync(userId);
return userRoles.Any(ur => ur.RoleName == roleName);
        }

   public async Task<bool> HasRoleLevelAsync(int userId, int minLevel)
        {
    var userRoles = await GetUserRolesAsync(userId);
 var roles = await _roleRepository.GetAllAsync();

    foreach (var userRole in userRoles)
      {
  var role = roles.FirstOrDefault(r => r.Id == userRole.RoleId.ToString());
     if (role != null && role.Level >= minLevel)
   return true;
            }

return false;
   }

 #endregion

 #region Multi-role Support

        public async Task<bool> AssignMultipleRolesToUserAsync(int userId, List<int> roleIds, int primaryRoleId)
        {
      // Remove existing roles
  var userRoles = await _userRoleRepository.GetAllAsync();
  var existingRoles = userRoles
      .Where(ur => ur.UserId == userId && ur.RemovedAt == null)
  .ToList();

   foreach (var er in existingRoles)
  {
       er.RemovedAt = DateTime.UtcNow;
 _userRoleRepository.Update(er);
            }
  await _userRoleRepository.SaveChangesAsync();

  // Assign new roles
          foreach (var roleId in roleIds)
  {
   bool isPrimary = roleId == primaryRoleId;
     await AssignRoleToUserAsync(userId, roleId, isPrimary);
            }

    return true;
        }

        public async Task<List<RoleDto>> GetUserRolesDetailAsync(int userId)
 {
    var userRoles = await GetUserRolesAsync(userId);
       var roles = await _roleRepository.GetAllAsync();
    var rolePermissions = await _rolePermissionRepository.GetAllAsync();

          var result = new List<RoleDto>();
  foreach (var userRole in userRoles)
  {
           var role = roles.FirstOrDefault(r => r.Id == userRole.RoleId.ToString());
      if (role != null)
      {
         var permissions = rolePermissions
  .Where(rp => rp.RoleId == userRole.RoleId)
       .Select(rp => rp.PermissionId)
         .ToList();

   result.Add(new RoleDto
         {
   Id = userRole.RoleId,
  Name = role.Name,
     DisplayName = role.DisplayName,
      Department = role.Department,
     Level = role.Level,
    Description = role.Description,
  PermissionIds = permissions,
       IsActive = role.IsActive,
   CreatedAt = role.CreatedAt
  });
   }
     }

  return result;
   }

        #endregion

      #region Helper Methods

        private UserRoleDto MapToDto(UserRole userRole, Role role)
 {
if (userRole == null || role == null)
    return null;

     return new UserRoleDto
     {
  UserId = userRole.UserId,
 RoleId = userRole.RoleId,
   RoleName = role.Name,
   RoleDisplayName = role.DisplayName,
IsPrimary = userRole.IsPrimary,
    AssignedAt = userRole.AssignedAt
      };
      }

        #endregion
    }
}
