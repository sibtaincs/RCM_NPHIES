using System;
using System.Collections.Generic;

namespace NPhies_FHIR_Integration.Domain.Entities
{
    /// <summary>
    /// Hierarchical Role entity for RBAC system
    /// Supports 2 tracks (Technical & Medical) with 4 levels each (1-4)
    /// </summary>
    public class Role : BaseEntity
    {
        /// <summary>
        /// Role name (e.g., TECHNICAL_REVIEWER, SENIOR_MEDICAL_REVIEWER)
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Display name for UI (e.g., "Technical Reviewer", "Senior Medical Reviewer")
      /// </summary>
  public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// Department: "TECHNICAL", "MEDICAL", "ADMIN"
    /// </summary>
    public string Department { get; set; } = string.Empty;

        /// <summary>
        /// Level: 1=Reviewer, 2=Senior, 3=Supervisor, 4=Manager
        /// </summary>
        public int Level { get; set; }

        /// <summary>
   /// Role description for admin reference
     /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Parent role ID for hierarchy (if supervisor role, this is null; if reviewer role, parent is supervisor)
        /// </summary>
        public int? SupervisorRoleId { get; set; }

        /// <summary>
   /// Is this role active?
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
 /// Permissions assigned to this role
        /// </summary>
 public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();

        /// <summary>
      /// Users assigned to this role
        /// </summary>
        public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

        /// <summary>
        /// Supervisor assignments for this role (if Level 3 or 4)
        /// </summary>
   public virtual ICollection<SupervisorAssignment> SupervisorAssignments { get; set; } = new List<SupervisorAssignment>();
    }

    /// <summary>
    /// Permission entity - defines what actions users can perform
    /// </summary>
    public class Permission : BaseEntity
    {
        /// <summary>
   /// Permission name (e.g., CLAIMS:READ, CLAIMS:APPROVE)
  /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Category: CLAIMS, REPORTS, USERS, QUEUE, AUDIT, SYSTEM, GUIDELINES
/// </summary>
        public string Category { get; set; } = string.Empty;

    /// <summary>
        /// Action: READ, VALIDATE, REVIEW, APPROVE, DENY, ESCALATE, OVERRIDE, UPDATE, CREATE, MANAGE, EXPORT
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
  /// Minimum level required (0=all, 1=entry, 2=senior, 3=supervisor, 4=manager)
        /// </summary>
        public int RequiredLevel { get; set; } = 0;

        /// <summary>
      /// Description for admin reference
        /// </summary>
      public string Description { get; set; } = string.Empty;

 /// <summary>
        /// Is this permission active?
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Roles that have this permission
        /// </summary>
     public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    }

    /// <summary>
    /// Role-Permission mapping (many-to-many)
    /// </summary>
    public class RolePermission
    {
  public int RoleId { get; set; }
    public int PermissionId { get; set; }

        /// <summary>
      /// Is this permission granted by default for this role?
        /// </summary>
   public bool GrantedByDefault { get; set; } = true;

 public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        public virtual Role Role { get; set; }
    public virtual Permission Permission { get; set; }
    }

    /// <summary>
    /// User-Role mapping (many-to-many)
    /// A user can have one primary role but can have additional secondary roles
    /// </summary>
    public class UserRole
    {
        public int UserId { get; set; }
    public int RoleId { get; set; }

        /// <summary>
        /// Is this the primary role for the user?
        /// </summary>
        public bool IsPrimary { get; set; } = false;

      public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RemovedAt { get; set; }

    public virtual User User { get; set; }
     public virtual Role Role { get; set; }
    }

    /// <summary>
    /// Supervisor assignment - who supervises whom
    /// </summary>
    public class SupervisorAssignment
    {
        public int Id { get; set; }

      /// <summary>
        /// The supervisor's user ID
        /// </summary>
        public int SupervisorUserId { get; set; }

        /// <summary>
        /// The subordinate's user ID
        /// </summary>
        public int SubordinateUserId { get; set; }

  /// <summary>
        /// Department: "TECHNICAL" or "MEDICAL"
        /// </summary>
        public string Department { get; set; } = string.Empty;

     /// <summary>
        /// The supervisor's role ID
        /// </summary>
        public int SupervisorRoleId { get; set; }

      /// <summary>
   /// Number of people in the supervisor's team
     /// </summary>
        public int TeamSize { get; set; }

        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RemovedAt { get; set; }

        public virtual User Supervisor { get; set; }
        public virtual User Subordinate { get; set; }
        public virtual Role SupervisorRole { get; set; }
    }

    /// <summary>
    /// Dynamic claim assignment based on complexity routing
    /// </summary>
    public class ClaimAssignment
    {
        public int Id { get; set; }

        /// <summary>
        /// Claim ID being assigned
        /// </summary>
 public int ClaimId { get; set; }

   /// <summary>
     /// Complexity score 1-10
        /// 1-3: Simple, 4-6: Moderate, 7-9: Complex, 10: Critical
        /// </summary>
        public int ComplexityScore { get; set; }

        /// <summary>
      /// Review type: "TECHNICAL" or "MEDICAL"
        /// </summary>
    public string ReviewType { get; set; } = string.Empty;

        /// <summary>
        /// User assigned to review
  /// </summary>
        public int AssignedToUserId { get; set; }

        /// <summary>
        /// The role used for this assignment
        /// </summary>
  public string AssignedRole { get; set; } = string.Empty;

 /// <summary>
  /// When assigned
        /// </summary>
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

   /// <summary>
   /// When completed
    /// </summary>
        public DateTime? CompletedAt { get; set; }

    /// <summary>
    /// Review result: PASS, FAIL, APPROVE, DENY, REQUEST_INFO, ESCALATE
        /// </summary>
        public string Result { get; set; } = string.Empty;

    /// <summary>
  /// Comments from reviewer
        /// </summary>
    public string Comments { get; set; } = string.Empty;

        /// <summary>
        /// If escalated, who was it escalated to?
        /// </summary>
        public int? EscalatedToUserId { get; set; }

        /// <summary>
        /// Is this assignment marked for QA review?
        /// </summary>
    public bool IsQAReview { get; set; } = false;

        /// <summary>
        /// QA review result
        /// </summary>
        public string QAResult { get; set; } = string.Empty;

        public virtual User AssignedToUser { get; set; }
        public virtual User EscalatedToUser { get; set; }
 }

    /// <summary>
    /// QA Review record for supervisor quality assurance
    /// </summary>
    public class QAReview
    {
        public int Id { get; set; }

        /// <summary>
    /// Claim assignment being QA reviewed
        /// </summary>
        public int ClaimAssignmentId { get; set; }

        /// <summary>
        /// The supervisor performing QA
        /// </summary>
  public int ReviewedByUserId { get; set; }

     /// <summary>
        /// QA findings/comments
        /// </summary>
public string Findings { get; set; } = string.Empty;

        /// <summary>
        /// QA result: PASS, NEEDS_REVISION, ESCALATE
        /// </summary>
        public string Result { get; set; } = string.Empty;

        /// <summary>
        /// Issues found (if any)
 /// </summary>
  public string IssuesFound { get; set; } = string.Empty;

      public DateTime ReviewedAt { get; set; } = DateTime.UtcNow;

        public virtual ClaimAssignment ClaimAssignment { get; set; }
        public virtual User ReviewedByUser { get; set; }
    }

    /// <summary>
    /// Audit log for permission tracking and compliance
    /// </summary>
    public class PermissionAuditLog
 {
    public int Id { get; set; }

        /// <summary>
        /// User who performed the action
        /// </summary>
        public int UserId { get; set; }

        /// <summary>
        /// Action performed: CLAIM_REVIEWED, CLAIM_APPROVED, CLAIM_DENIED, CLAIM_ESCALATED, ROLE_ASSIGNED, etc.
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
     /// Resource type affected: CLAIM, USER, ROLE, etc.
        /// </summary>
     public string ResourceType { get; set; } = string.Empty;

  /// <summary>
   /// Resource ID affected
        /// </summary>
   public int ResourceId { get; set; }

    /// <summary>
        /// Details of the action
    /// </summary>
        public string Details { get; set; } = string.Empty;

        /// <summary>
    /// User's role at time of action
/// </summary>
  public string UserRoleAtTime { get; set; } = string.Empty;

        /// <summary>
        /// IP address of the request
 /// </summary>
        public string IpAddress { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public virtual User User { get; set; }
}
}
