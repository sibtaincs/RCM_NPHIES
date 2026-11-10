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
    /// Implementation of permission audit logging service for compliance and security
    /// </summary>
    public class PermissionAuditService : IPermissionAuditService
    {
        private readonly IRepository<PermissionAuditLog> _auditRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<UserRole> _userRoleRepository;
        private readonly IRepository<Role> _roleRepository;

        public PermissionAuditService(
   IRepository<PermissionAuditLog> auditRepository,
IRepository<User> userRepository,
IRepository<UserRole> userRoleRepository,
     IRepository<Role> roleRepository)
        {
            _auditRepository = auditRepository;
            _userRepository = userRepository;
            _userRoleRepository = userRoleRepository;
            _roleRepository = roleRepository;
        }

        #region Audit Logging

        public async Task LogActionAsync(int userId, string action, string resourceType, int resourceId, string details, string? ipAddress = null)
        {
            // Get user's current role
            var userRoles = await _userRoleRepository.GetAllAsync();
            var userRole = userRoles.FirstOrDefault(ur =>
         ur.UserId == userId && ur.RemovedAt == null && ur.IsPrimary);

            string roleAtTime = "UNKNOWN";
            if (userRole != null)
            {
                var role = await _roleRepository.GetByIdAsync(userRole.RoleId.ToString());
                roleAtTime = role?.Name ?? "UNKNOWN";
            }

            var auditLog = new PermissionAuditLog
            {
                UserId = userId,
                Action = action,
                ResourceType = resourceType,
                ResourceId = resourceId,
                Details = details,
                UserRoleAtTime = roleAtTime,
                IpAddress = ipAddress ?? "SYSTEM",
                CreatedAt = DateTime.UtcNow
            };

            await _auditRepository.AddAsync(auditLog);
            await _auditRepository.SaveChangesAsync();
        }

        public async Task LogPermissionCheckAsync(int userId, string permission, bool granted, string? ipAddress = null)
        {
            string action = granted ? $"PERMISSION_GRANTED_{permission}" : $"PERMISSION_DENIED_{permission}";
            string details = $"Permission check for {permission}: {(granted ? "GRANTED" : "DENIED")}";

            await LogActionAsync(userId, action, "PERMISSION", 0, details, ipAddress);
        }

        public async Task LogRoleAssignmentAsync(int userId, int roleId, bool assigned, string? ipAddress = null)
        {
            string action = assigned ? "ROLE_ASSIGNED" : "ROLE_REMOVED";
            string details = assigned ? $"Role {roleId} assigned" : $"Role {roleId} removed";

            await LogActionAsync(userId, action, "ROLE", roleId, details, ipAddress);
        }

        #endregion

        #region Audit Retrieval

        public async Task<List<PermissionAuditLogDto>> GetUserAuditLogsAsync(int userId)
        {
            var logs = await _auditRepository.GetAllAsync();
            var userLogs = logs
          .Where(al => al.UserId == userId)
          .OrderByDescending(al => al.CreatedAt)
           .ToList();

            return await MapToDtoAsync(userLogs);
        }

        public async Task<List<PermissionAuditLogDto>> GetResourceAuditLogsAsync(string resourceType, int resourceId)
        {
            var logs = await _auditRepository.GetAllAsync();
            var resourceLogs = logs
           .Where(al => al.ResourceType == resourceType && al.ResourceId == resourceId)
            .OrderByDescending(al => al.CreatedAt)
          .ToList();

            return await MapToDtoAsync(resourceLogs);
        }

        public async Task<List<PermissionAuditLogDto>> GetAuditLogsByActionAsync(string action)
        {
            var logs = await _auditRepository.GetAllAsync();
            var actionLogs = logs
              .Where(al => al.Action == action)
               .OrderByDescending(al => al.CreatedAt)
            .ToList();

            return await MapToDtoAsync(actionLogs);
        }

        public async Task<List<PermissionAuditLogDto>> GetAuditLogsAsync(DateTime fromDate, DateTime toDate)
        {
            var logs = await _auditRepository.GetAllAsync();
            var rangeLogs = logs
            .Where(al => al.CreatedAt >= fromDate && al.CreatedAt <= toDate)
                  .OrderByDescending(al => al.CreatedAt)
             .ToList();

            return await MapToDtoAsync(rangeLogs);
        }

        #endregion

        #region Compliance Reports

        public async Task<List<PermissionAuditLogDto>> GetUnauthorizedAccessAttemptsAsync(int days = 30)
        {
            var fromDate = DateTime.UtcNow.AddDays(-days);
            var logs = await GetAuditLogsAsync(fromDate, DateTime.UtcNow);

            var unauthorizedAttempts = logs
        .Where(l => l.Action.Contains("DENIED") || l.Action.Contains("ESCALATED"))
           .ToList();

            return unauthorizedAttempts;
        }

        public async Task<List<PermissionAuditLogDto>> GetHighRiskActionsAsync()
        {
            var logs = await _auditRepository.GetAllAsync();

            var highRiskActions = new List<string>
      {
          "CLAIMS:OVERRIDE",
    "USERS:MANAGE",
 "GUIDELINES:MANAGE",
         "SYSTEM:SETTINGS",
      "PERMISSION_DENIED_CLAIMS:APPROVE"
             };

            var riskLogs = logs
                .Where(l => highRiskActions.Any(action => l.Action.Contains(action)))
                 .OrderByDescending(l => l.CreatedAt)
              .ToList();

            return await MapToDtoAsync(riskLogs);
        }

        #endregion

        #region Helper Methods

        private async Task<List<PermissionAuditLogDto>> MapToDtoAsync(List<PermissionAuditLog> logs)
        {
            var result = new List<PermissionAuditLogDto>();

            foreach (var log in logs)
            {
                var user = await _userRepository.GetByIdAsync(log.UserId.ToString());

                result.Add(new PermissionAuditLogDto
                {
                    Id = log.Id,
                    UserId = log.UserId,
                    UserName = (user?.FirstName ?? "") + " " + (user?.LastName ?? "SYSTEM"),
                    Action = log.Action,
                    ResourceType = log.ResourceType,
                    ResourceId = log.ResourceId,
                    Details = log.Details,
                    UserRoleAtTime = log.UserRoleAtTime,
                    IpAddress = log.IpAddress,
                    CreatedAt = log.CreatedAt
                });
            }

            return result;
        }

        #endregion
    }
}
