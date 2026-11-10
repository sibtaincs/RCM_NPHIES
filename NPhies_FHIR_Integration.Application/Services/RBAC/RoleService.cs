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
    /// Implementation of role and permission management service
    /// </summary>
    public class RoleService : IRoleService
    {
        private readonly IRepository<Role> _roleRepository;
        private readonly IRepository<Permission> _permissionRepository;
        private readonly IRepository<RolePermission> _rolePermissionRepository;

        public RoleService(
        IRepository<Role> roleRepository,
       IRepository<Permission> permissionRepository,
      IRepository<RolePermission> rolePermissionRepository)
        {
            _roleRepository = roleRepository;
            _permissionRepository = permissionRepository;
            _rolePermissionRepository = rolePermissionRepository;
        }

        #region Role Management

        public async Task<RoleDto?> GetRoleByIdAsync(int roleId)
        {
            var role = await _roleRepository.GetByIdAsync(roleId.ToString());
            return role is null ? null : MapToDto(role);
        }

        public async Task<RoleDto?> GetRoleByNameAsync(string roleName)
        {
            var roles = await _roleRepository.GetAllAsync();
            var role = roles.FirstOrDefault(r => r.Name == roleName);
            return role is null ? null : MapToDto(role);
        }

        public async Task<List<RoleDto>> GetAllRolesAsync()
        {
            var roles = await _roleRepository.GetAllAsync();
            return roles.Select(MapToDto).ToList();
        }

        public async Task<List<RoleDto>> GetRolesByDepartmentAsync(string department)
        {
            var roles = await _roleRepository.GetAllAsync();
            var filtered = roles.Where(r => r.Department == department).ToList();
            return filtered.Select(MapToDto).ToList();
        }

        public async Task<List<RoleDto>> GetRolesByLevelAsync(int level)
        {
            var roles = await _roleRepository.GetAllAsync();
            var filtered = roles.Where(r => r.Level == level).ToList();
            return filtered.Select(MapToDto).ToList();
        }

        public async Task<RoleDto> CreateRoleAsync(CreateUpdateRoleDto dto)
        {
            var role = new Role
            {
                Name = dto.Name,
                DisplayName = dto.DisplayName,
                Department = dto.Department,
                Level = dto.Level,
                Description = dto.Description,
                SupervisorRoleId = dto.SupervisorRoleId,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            var created = await _roleRepository.AddAsync(role);
            await _roleRepository.SaveChangesAsync();

            // Assign permissions
            if (dto.PermissionIds?.Any() == true)
            {
                foreach (var permissionId in dto.PermissionIds)
                {
                    await AssignPermissionToRoleAsync(int.Parse(created.Id), permissionId);
                }
            }

            return MapToDto(created);
        }

        public async Task<RoleDto> UpdateRoleAsync(int roleId, CreateUpdateRoleDto dto)
        {
            var role = await _roleRepository.GetByIdAsync(roleId.ToString());
            if (role == null)
                throw new InvalidOperationException($"Role with ID {roleId} not found");

            role.DisplayName = dto.DisplayName;
            role.Department = dto.Department;
            role.Level = dto.Level;
            role.Description = dto.Description;
            role.SupervisorRoleId = dto.SupervisorRoleId;
            role.IsActive = dto.IsActive;
            role.UpdatedAt = DateTime.UtcNow;

            _roleRepository.Update(role);
            await _roleRepository.SaveChangesAsync();

            // Remove old permissions and add new ones
            var oldPermissions = await _rolePermissionRepository.GetAllAsync();
            var toRemove = oldPermissions.Where(rp => rp.RoleId == roleId).ToList();
            foreach (var rp in toRemove)
            {
                _rolePermissionRepository.Delete(rp);
            }
            await _rolePermissionRepository.SaveChangesAsync();

            // Add new permissions
            if (dto.PermissionIds?.Any() == true)
            {
                foreach (var permissionId in dto.PermissionIds)
                {
                    await AssignPermissionToRoleAsync(roleId, permissionId);
                }
            }

            return MapToDto(role);
        }

        public async Task<bool> DeleteRoleAsync(int roleId)
        {
            var role = await _roleRepository.GetByIdAsync(roleId.ToString());
            if (role == null)
                return false;

            // Check if any users have this role
            var userRoles = role.UserRoles?.Any() ?? false;
            if (userRoles)
                throw new InvalidOperationException("Cannot delete role with assigned users");

            _roleRepository.Delete(role);
            await _roleRepository.SaveChangesAsync();
            return true;
        }

        #endregion

        #region Permission Management

        public async Task<PermissionDto?> GetPermissionByIdAsync(int permissionId)
        {
            var permission = await _permissionRepository.GetByIdAsync(permissionId.ToString());
            return permission is null ? null : MapToDto(permission);
        }

        public async Task<PermissionDto?> GetPermissionByNameAsync(string permissionName)
        {
            var permissions = await _permissionRepository.GetAllAsync();
            var permission = permissions.FirstOrDefault(p => p.Name == permissionName);
            return permission is null ? null : MapToDto(permission);
        }

        public async Task<List<PermissionDto>> GetAllPermissionsAsync()
        {
            var permissions = await _permissionRepository.GetAllAsync();
            return permissions.Select(MapToDto).ToList();
        }

        public async Task<List<PermissionDto>> GetPermissionsByCategoryAsync(string category)
        {
            var permissions = await _permissionRepository.GetAllAsync();
            var filtered = permissions.Where(p => p.Category == category).ToList();
            return filtered.Select(MapToDto).ToList();
        }

        public async Task<List<PermissionDto>> GetPermissionsByLevelAsync(int level)
        {
            var permissions = await _permissionRepository.GetAllAsync();
            var filtered = permissions.Where(p => p.RequiredLevel <= level).ToList();
            return filtered.Select(MapToDto).ToList();
        }

        #endregion

        #region Role-Permission Mapping

        public async Task<bool> AssignPermissionToRoleAsync(int roleId, int permissionId)
        {
            var rolePermission = new RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                GrantedByDefault = true,
                AssignedAt = DateTime.UtcNow
            };

            var result = await _rolePermissionRepository.AddAsync(rolePermission);
            await _rolePermissionRepository.SaveChangesAsync();
            return result != null;
        }

        public async Task<bool> RemovePermissionFromRoleAsync(int roleId, int permissionId)
        {
            var rolePermissions = await _rolePermissionRepository.GetAllAsync();
            var rolePermission = rolePermissions.FirstOrDefault(rp =>
               rp.RoleId == roleId && rp.PermissionId == permissionId);

            if (rolePermission == null)
                return false;

            _rolePermissionRepository.Delete(rolePermission);
            await _rolePermissionRepository.SaveChangesAsync();
            return true;
        }

        public async Task<List<PermissionDto>> GetRolePermissionsAsync(int roleId)
        {
            var rolePermissions = await _rolePermissionRepository.GetAllAsync();
            var permissions = rolePermissions
        .Where(rp => rp.RoleId == roleId)
          .Select(rp => rp.Permission)
       .ToList();

            return permissions.Select(MapToDto).ToList();
        }

        #endregion

        #region Role Hierarchy

        public async Task<RoleHierarchyDto> GetRoleHierarchyAsync(string department)
        {
            var roles = await GetRolesByDepartmentAsync(department);
            var hierarchy = new RoleHierarchyDto
            {
                Department = department,
                Levels = new List<RoleLevelDto>()
            };

            for (int level = 1; level <= 4; level++)
            {
                var levelRoles = roles.Where(r => r.Level == level).ToList();
                if (levelRoles.Any())
                {
                    hierarchy.Levels.Add(new RoleLevelDto
                    {
                        Level = level,
                        LevelName = GetLevelName(level),
                        Roles = levelRoles
                    });
                }
            }

            return hierarchy;
        }

        public async Task<List<RoleDto>> GetHierarchyChainAsync(int roleId)
        {
            var role = await _roleRepository.GetByIdAsync(roleId.ToString());
            if (role is null)
                return new List<RoleDto>();
            var chain = new List<RoleDto> { MapToDto(role) };

            while (role.SupervisorRoleId.HasValue)
            {
                role = await _roleRepository.GetByIdAsync(role.SupervisorRoleId.Value.ToString());
                if (role != null)
                    chain.Add(MapToDto(role));
                else
                    break;
            }

            return chain;
        }

        #endregion

        #region Helper Methods

        private RoleDto MapToDto(Role role)
        {
            return new RoleDto
            {
                Id = int.Parse(role.Id),
                Name = role.Name,
                DisplayName = role.DisplayName,
                Department = role.Department,
                Level = role.Level,
                Description = role.Description,
                SupervisorRoleId = role.SupervisorRoleId,
                IsActive = role.IsActive,
                PermissionIds = role.RolePermissions?.Select(rp => rp.PermissionId).ToList() ?? new List<int>(),
                UserCount = role.UserRoles?.Count ?? 0,
                CreatedAt = role.CreatedAt
            };
        }

        private PermissionDto MapToDto(Permission permission)
        {
            return new PermissionDto
            {
                Id = int.Parse(permission.Id),
                Name = permission.Name,
                Category = permission.Category,
                Action = permission.Action,
                RequiredLevel = permission.RequiredLevel,
                Description = permission.Description,
                IsActive = permission.IsActive
            };
        }

        private static string GetLevelName(int level)
        {
            return level switch
            {
                1 => "Reviewer",
                2 => "Senior Reviewer",
                3 => "Supervisor",
                4 => "Manager",
                _ => "Unknown"
            };
        }

        #endregion
    }
}
