using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Domain.DTOs;

namespace NPhies_FHIR_Integration.Application.Services.RBAC
{
    /// <summary>
    /// Service for managing roles and permissions in hierarchical RBAC system
    /// </summary>
    public interface IRoleService
    {
        // Role Management
        Task<RoleDto?> GetRoleByIdAsync(int roleId);
 Task<RoleDto?> GetRoleByNameAsync(string roleName);
        Task<List<RoleDto>> GetAllRolesAsync();
        Task<List<RoleDto>> GetRolesByDepartmentAsync(string department);
   Task<List<RoleDto>> GetRolesByLevelAsync(int level);
  Task<RoleDto> CreateRoleAsync(CreateUpdateRoleDto dto);
  Task<RoleDto> UpdateRoleAsync(int roleId, CreateUpdateRoleDto dto);
        Task<bool> DeleteRoleAsync(int roleId);

// Permission Management
     Task<PermissionDto?> GetPermissionByIdAsync(int permissionId);
        Task<PermissionDto?> GetPermissionByNameAsync(string permissionName);
        Task<List<PermissionDto>> GetAllPermissionsAsync();
Task<List<PermissionDto>> GetPermissionsByCategoryAsync(string category);
    Task<List<PermissionDto>> GetPermissionsByLevelAsync(int level);

        // Role-Permission Mapping
        Task<bool> AssignPermissionToRoleAsync(int roleId, int permissionId);
    Task<bool> RemovePermissionFromRoleAsync(int roleId, int permissionId);
        Task<List<PermissionDto>> GetRolePermissionsAsync(int roleId);

        // Role Hierarchy
      Task<RoleHierarchyDto> GetRoleHierarchyAsync(string department);
        Task<List<RoleDto>> GetHierarchyChainAsync(int roleId);
    }

    /// <summary>
    /// Service for user role assignments and management
    /// </summary>
    public interface IUserRoleService
    {
 // User Role Assignment
        Task<List<UserRoleDto>> GetUserRolesAsync(int userId);
        Task<UserRoleDto> GetPrimaryRoleAsync(int userId);
      Task<List<int>> GetUserPermissionsAsync(int userId);
        Task<bool> AssignRoleToUserAsync(int userId, int roleId, bool isPrimary = false);
      Task<bool> RemoveRoleFromUserAsync(int userId, int roleId);
        Task<bool> ChangePrimaryRoleAsync(int userId, int newRoleId);

        // Permission Checks
        Task<bool> HasPermissionAsync(int userId, string permissionName);
        Task<bool> HasAllPermissionsAsync(int userId, List<string> permissionNames);
  Task<bool> HasAnyPermissionAsync(int userId, List<string> permissionNames);
  Task<bool> HasRoleAsync(int userId, string roleName);
        Task<bool> HasRoleLevelAsync(int userId, int minLevel);

        // Multi-role Support
 Task<bool> AssignMultipleRolesToUserAsync(int userId, List<int> roleIds, int primaryRoleId);
        Task<List<RoleDto>> GetUserRolesDetailAsync(int userId);
    }

    /// <summary>
    /// Service for supervisor-subordinate relationships and management
    /// </summary>
    public interface ISupervisorService
    {
        // Supervisor Assignments
        Task<SupervisorAssignmentDto> CreateSupervisorAssignmentAsync(CreateSupervisorAssignmentDto dto);
        Task<SupervisorAssignmentDto?> GetSupervisorAssignmentAsync(int supervisorId, int subordinateId);
        Task<List<SupervisorAssignmentDto>> GetSupervisorTeamAsync(int supervisorId);
        Task<List<SupervisorAssignmentDto>> GetSubordinatesSupervisorsAsync(int subordinateId);
  Task<bool> RemoveSupervisorAssignmentAsync(int supervisorId, int subordinateId);

        // Supervisor Queries
     Task<int> GetTeamSizeAsync(int supervisorId);
        Task<bool> IsSupervisorOfAsync(int supervisorId, int subordinateId);
 Task<List<int>> GetAllSubordinatesRecursiveAsync(int supervisorId); // Get entire chain

        // Team Management
        Task<TeamMetricsDto> GetTeamMetricsAsync(int supervisorId);
     Task<List<ReviewerPerformanceDto>> GetTeamPerformanceAsync(int supervisorId);

      // Manager Queries
    Task<DepartmentMetricsDto> GetDepartmentMetricsAsync(int managerId);
        Task<List<SupervisorAssignmentDto>> GetManagerSupervisorsAsync(int managerId);
    }

    /// <summary>
    /// Service for claim assignment and routing based on complexity and role
    /// </summary>
    public interface IClaimRoutingService
    {
   // Claim Assignment
      Task<ClaimAssignmentDto> AssignClaimAsync(AssignClaimDto dto);
        Task<ClaimAssignmentDto?> GetClaimAssignmentAsync(int claimAssignmentId);
  Task<List<ClaimAssignmentDto>> GetUserQueueAsync(int userId, string reviewType);
        Task<List<ClaimAssignmentDto>> GetPendingClaimsAsync(string? reviewType = null);

        // Complexity Scoring
        int CalculateComplexityScore(Claim claim);
        string GetTargetRoleForComplexity(string reviewType, int complexityScore);

        // Reviewer Selection
Task<int> FindAvailableReviewerAsync(string reviewType, int complexityScore);
        Task<int> FindAvailableSupervisorAsync(string department, int complexityScore);

        // Review Submission
        Task<bool> SubmitReviewAsync(int claimAssignmentId, SubmitReviewDto dto, int reviewerId);
        Task<bool> EscalateClaimAsync(int claimAssignmentId, int escalatedToUserId, string reason);

        // QA Queue Management
     Task<List<ClaimAssignmentDto>> GetQAQueueAsync(int supervisorId);
        Task<bool> SubmitQAReviewAsync(int claimAssignmentId, SubmitQAReviewDto dto, int reviewerId);

        // Metrics
        Task<List<ClaimAssignmentDto>> GetReviewerMetricsAsync(int reviewerId, int days = 7);
  }

    /// <summary>
    /// Service for audit logging of permission-related activities
    /// </summary>
    public interface IPermissionAuditService
    {
  // Audit Logging
        Task LogActionAsync(int userId, string action, string resourceType, int resourceId, string details, string? ipAddress = null);
        Task LogPermissionCheckAsync(int userId, string permission, bool granted, string? ipAddress = null);
      Task LogRoleAssignmentAsync(int userId, int roleId, bool assigned, string? ipAddress = null);

        // Audit Retrieval
  Task<List<PermissionAuditLogDto>> GetUserAuditLogsAsync(int userId);
        Task<List<PermissionAuditLogDto>> GetResourceAuditLogsAsync(string resourceType, int resourceId);
        Task<List<PermissionAuditLogDto>> GetAuditLogsByActionAsync(string action);
        Task<List<PermissionAuditLogDto>> GetAuditLogsAsync(DateTime fromDate, DateTime toDate);

        // Compliance Reports
  Task<List<PermissionAuditLogDto>> GetUnauthorizedAccessAttemptsAsync(int days = 30);
        Task<List<PermissionAuditLogDto>> GetHighRiskActionsAsync();
    }

    /// <summary>
    /// Hierarchical permission helper service
    /// </summary>
    public class PermissionInheritanceHelper
    {
      /// <summary>
        /// Get all permissions for a given level and above
        /// Level inheritance: 1 (basic) ? 2 (+ advanced) ? 3 (+ supervision) ? 4 (+ management)
        /// </summary>
        public static List<string> GetPermissionsForLevel(int level)
        {
            var permissions = new List<string>();

            // Level 1: Basic Reviewer Permissions
      if (level >= 1)
     {
         permissions.AddRange(new[]
         {
        "CLAIMS:READ",
          "CLAIMS:VALIDATE",
"CLAIMS:REVIEW",
         "REPORTS:READ",
       "REPORTS:EXPORT"
           });
            }

       // Level 2: Senior Reviewer Additional Permissions
      if (level >= 2)
   {
 permissions.AddRange(new[]
           {
      "CLAIMS:UPDATE",
    "CLAIMS:ESCALATE",
  "REPORTS:CREATE"
    });
            }

          // Level 3: Supervisor Additional Permissions
            if (level >= 3)
     {
       permissions.AddRange(new[]
   {
      "CLAIMS:OVERRIDE",
           "USERS:READ",
           "QUEUE:MANAGE",
            "AUDIT:READ"
      });
      }

            // Level 4: Manager Additional Permissions
      if (level >= 4)
            {
         permissions.AddRange(new[]
     {
         "USERS:MANAGE",
  "GUIDELINES:MANAGE",
        "SYSTEM:SETTINGS"
 });
      }

    return permissions;
        }

        /// <summary>
        /// Check if level has permission (with inheritance)
 /// </summary>
  public static bool LevelHasPermission(int level, string permission)
{
            var permissionsForLevel = GetPermissionsForLevel(level);
            return permissionsForLevel.Contains(permission);
        }

        /// <summary>
      /// Get minimum level required for a permission
        /// </summary>
        public static int GetMinimumLevelForPermission(string permission)
      {
      return permission switch
            {
     // Level 1 permissions
                "CLAIMS:READ" or "CLAIMS:VALIDATE" or "CLAIMS:REVIEW" or "REPORTS:READ" or "REPORTS:EXPORT" => 1,

           // Level 2 permissions
         "CLAIMS:UPDATE" or "CLAIMS:ESCALATE" or "REPORTS:CREATE" => 2,

      // Level 3 permissions
        "CLAIMS:OVERRIDE" or "USERS:READ" or "QUEUE:MANAGE" or "AUDIT:READ" => 3,

     // Level 4 permissions
            "USERS:MANAGE" or "GUIDELINES:MANAGE" or "SYSTEM:SETTINGS" => 4,

 _ => 999 // Unknown permission
         };
        }
    }

    /// <summary>
/// Predefined roles factory
    /// </summary>
    public class RoleFactory
    {
  public static List<CreateUpdateRoleDto> CreateDefaultRoles()
 {
         return new List<CreateUpdateRoleDto>
   {
   // Technical Review Roles
         new CreateUpdateRoleDto
    {
  Name = "TECHNICAL_REVIEWER",
          DisplayName = "Technical Reviewer",
      Department = "TECHNICAL",
         Level = 1,
   Description = "Entry-level technical claim reviewer - validates data completeness and format",
     IsActive = true,
         PermissionIds = new List<int> { 1, 2, 3, 4, 5 } // Basic permissions
          },
     new CreateUpdateRoleDto
 {
      Name = "SENIOR_TECHNICAL_REVIEWER",
   DisplayName = "Senior Technical Reviewer",
   Department = "TECHNICAL",
    Level = 2,
      Description = "Senior technical reviewer - handles complex claim validations",
      IsActive = true,
          PermissionIds = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8 } // + advanced
       },
   new CreateUpdateRoleDto
      {
  Name = "TECHNICAL_REVIEW_SUPERVISOR",
       DisplayName = "Technical Review Supervisor",
         Department = "TECHNICAL",
     Level = 3,
      Description = "Supervises technical reviewers - QA and escalation authority",
            IsActive = true,
   PermissionIds = new List<int> { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 } // + supervision
   },
     new CreateUpdateRoleDto
       {
       Name = "TECHNICAL_REVIEW_MANAGER",
      DisplayName = "Technical Review Manager",
         Department = "TECHNICAL",
      Level = 4,
 Description = "Manages technical review department - strategy and policy",
IsActive = true,
    PermissionIds = new List<int> { } // All permissions
  },

 // Medical Review Roles
   new CreateUpdateRoleDto
      {
  Name = "MEDICAL_REVIEWER",
  DisplayName = "Medical Reviewer",
   Department = "MEDICAL",
           Level = 1,
  Description = "Entry-level medical reviewer - routine medical necessity reviews",
              IsActive = true,
             PermissionIds = new List<int> { 1, 3, 4, 13, 14, 15 } // Medical-specific permissions
           },
            new CreateUpdateRoleDto
       {
 Name = "SENIOR_MEDICAL_REVIEWER",
        DisplayName = "Senior Medical Reviewer",
 Department = "MEDICAL",
      Level = 2,
      Description = "Senior medical reviewer - complex clinical cases",
        IsActive = true,
    PermissionIds = new List<int> { 1, 3, 4, 6, 13, 14, 15, 16 } // + advanced medical
      },
     new CreateUpdateRoleDto
    {
   Name = "MEDICAL_REVIEW_SUPERVISOR",
         DisplayName = "Medical Review Supervisor",
        Department = "MEDICAL",
         Level = 3,
   Description = "Supervises medical reviewers - clinical QA and appeals",
        IsActive = true,
           PermissionIds = new List<int> { 1, 3, 4, 6, 9, 10, 11, 13, 14, 15, 16, 17 } // + supervision
       },
  new CreateUpdateRoleDto
  {
 Name = "MEDICAL_REVIEW_MANAGER",
   DisplayName = "Medical Review Manager",
       Department = "MEDICAL",
       Level = 4,
        Description = "Manages medical review department - clinical policy and guidelines",
               IsActive = true,
          PermissionIds = new List<int> { } // All permissions
         }
  };
  }

 public static List<Permission> CreateDefaultPermissions()
   {
        return new List<Permission>
{
// Basic Permissions (Level 1)
     new Permission { Id = "1", Name = "CLAIMS:READ", Category = "CLAIMS", Action = "READ", RequiredLevel = 1, Description = "View claims" },
       new Permission { Id = "2", Name = "CLAIMS:VALIDATE", Category = "CLAIMS", Action = "VALIDATE", RequiredLevel = 1, Description = "Validate claims (technical)" },
       new Permission { Id = "3", Name = "CLAIMS:REVIEW", Category = "CLAIMS", Action = "REVIEW", RequiredLevel = 1, Description = "Review claims (medical)" },
          new Permission { Id = "4", Name = "REPORTS:READ", Category = "REPORTS", Action = "READ", RequiredLevel = 1, Description = "View reports" },
     new Permission { Id = "5", Name = "REPORTS:EXPORT", Category = "REPORTS", Action = "EXPORT", RequiredLevel = 1, Description = "Export reports" },

       // Advanced Permissions (Level 2)
     new Permission { Id = "6", Name = "CLAIMS:UPDATE", Category = "CLAIMS", Action = "UPDATE", RequiredLevel = 2, Description = "Add comments to claims" },
    new Permission { Id = "7", Name = "CLAIMS:ESCALATE", Category = "CLAIMS", Action = "ESCALATE", RequiredLevel = 2, Description = "Escalate complex claims" },
    new Permission { Id = "8", Name = "REPORTS:CREATE", Category = "REPORTS", Action = "CREATE", RequiredLevel = 2, Description = "Create custom reports" },

          // Supervision Permissions (Level 3)
       new Permission { Id = "9", Name = "CLAIMS:OVERRIDE", Category = "CLAIMS", Action = "OVERRIDE", RequiredLevel = 3, Description = "Override claim decisions" },
    new Permission { Id = "10", Name = "USERS:READ", Category = "USERS", Action = "READ", RequiredLevel = 3, Description = "View team members" },
           new Permission { Id = "11", Name = "QUEUE:MANAGE", Category = "QUEUE", Action = "MANAGE", RequiredLevel = 3, Description = "Manage review queues" },
         new Permission { Id = "12", Name = "AUDIT:READ", Category = "AUDIT", Action = "READ", RequiredLevel = 3, Description = "View audit logs" },

  // Medical-specific Permissions
 new Permission { Id = "13", Name = "CLAIMS:APPROVE", Category = "CLAIMS", Action = "APPROVE", RequiredLevel = 1, Description = "Approve claims (medical)" },
      new Permission { Id = "14", Name = "CLAIMS:DENY", Category = "CLAIMS", Action = "DENY", RequiredLevel = 1, Description = "Deny claims (medical)" },
   new Permission { Id = "15", Name = "GUIDELINES:READ", Category = "GUIDELINES", Action = "READ", RequiredLevel = 1, Description = "View clinical guidelines" },
 new Permission { Id = "16", Name = "CLAIMS:REQUEST_INFO", Category = "CLAIMS", Action = "REQUEST_INFO", RequiredLevel = 2, Description = "Request additional info" },
    new Permission { Id = "17", Name = "CODES:READ", Category = "CODES", Action = "READ", RequiredLevel = 1, Description = "View medical codes" },

       // Management Permissions (Level 4)
     new Permission { Id = "18", Name = "USERS:MANAGE", Category = "USERS", Action = "MANAGE", RequiredLevel = 4, Description = "Manage users and roles" },
      new Permission { Id = "19", Name = "GUIDELINES:MANAGE", Category = "GUIDELINES", Action = "MANAGE", RequiredLevel = 4, Description = "Manage clinical guidelines" },
         new Permission { Id = "20", Name = "SYSTEM:SETTINGS", Category = "SYSTEM", Action = "SETTINGS", RequiredLevel = 4, Description = "System configuration" }
 };
    }
    }
}
