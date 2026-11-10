using System;
using System.Collections.Generic;

namespace NPhies_FHIR_Integration.Domain.DTOs
{
    /// <summary>
    /// DTO for Role management
    /// </summary>
    public class RoleDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Department { get; set; }
        public int Level { get; set; }
        public string Description { get; set; }
        public int? SupervisorRoleId { get; set; }
        public bool IsActive { get; set; }
        public List<int> PermissionIds { get; set; } = new();
        public int UserCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating/updating roles
    /// </summary>
    public class CreateUpdateRoleDto
    {
        public string Name { get; set; }
        public string DisplayName { get; set; }
        public string Department { get; set; }
        public int Level { get; set; }
        public string Description { get; set; }
        public int? SupervisorRoleId { get; set; }
        public bool IsActive { get; set; } = true;
        public List<int> PermissionIds { get; set; } = new();
    }

    /// <summary>
    /// DTO for Permission
    /// </summary>
    public class PermissionDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Category { get; set; }
        public string Action { get; set; }
        public int RequiredLevel { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO for User role assignment
    /// </summary>
    public class UserRoleDto
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public string RoleName { get; set; }
        public string RoleDisplayName { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime AssignedAt { get; set; }
    }

    /// <summary>
    /// DTO for assigning role to user
    /// </summary>
    public class AssignRoleDto
    {
        public int UserId { get; set; }
        public int RoleId { get; set; }
        public bool IsPrimary { get; set; } = false;
    }

    /// <summary>
    /// DTO for supervisor assignment
    /// </summary>
    public class SupervisorAssignmentDto
    {
        public int Id { get; set; }
        public int SupervisorUserId { get; set; }
        public string SupervisorName { get; set; }
        public int SubordinateUserId { get; set; }
        public string SubordinateName { get; set; }
        public string Department { get; set; }
        public int SupervisorRoleId { get; set; }
        public string SupervisorRole { get; set; }
        public int TeamSize { get; set; }
        public DateTime AssignedAt { get; set; }
    }

    /// <summary>
    /// DTO for creating supervisor assignment
    /// </summary>
    public class CreateSupervisorAssignmentDto
    {
        public int SupervisorUserId { get; set; }
        public int SubordinateUserId { get; set; }
        public string Department { get; set; }
        public int TeamSize { get; set; }
    }

    /// <summary>
    /// DTO for claim assignment with routing
    /// </summary>
    public class ClaimAssignmentDto
    {
        public int Id { get; set; }
        public int ClaimId { get; set; }
        public int ComplexityScore { get; set; }
        public string ReviewType { get; set; }
        public int AssignedToUserId { get; set; }
        public string AssignedToUserName { get; set; }
        public string AssignedRole { get; set; }
        public DateTime AssignedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string Result { get; set; }
        public string Comments { get; set; }
        public int? EscalatedToUserId { get; set; }
        public string EscalatedToUserName { get; set; }
        public bool IsQAReview { get; set; }
        public string QAResult { get; set; }
    }

    /// <summary>
    /// DTO for assigning claim to reviewer
    /// </summary>
    public class AssignClaimDto
    {
        public int ClaimId { get; set; }
        public string ReviewType { get; set; } // TECHNICAL or MEDICAL
        public int ComplexityScore { get; set; } // 1-10
        public int? PreferredReviewerId { get; set; } // If specified
    }

    /// <summary>
    /// DTO for submitting review result
    /// </summary>
    public class SubmitReviewDto
    {
        public int ClaimAssignmentId { get; set; }
        public string Result { get; set; } // PASS, FAIL, APPROVE, DENY, REQUEST_INFO, ESCALATE
        public string Comments { get; set; }
        public int? EscalatedToUserId { get; set; }
    }

    /// <summary>
    /// DTO for QA review
    /// </summary>
    public class QAReviewDto
    {
        public int Id { get; set; }
        public int ClaimAssignmentId { get; set; }
        public int ReviewedByUserId { get; set; }
        public string ReviewedByUserName { get; set; }
        public string Findings { get; set; }
        public string Result { get; set; } // PASS, NEEDS_REVISION, ESCALATE
        public string IssuesFound { get; set; }
        public DateTime ReviewedAt { get; set; }
    }

    /// <summary>
    /// DTO for submitting QA review
    /// </summary>
    public class SubmitQAReviewDto
    {
        public int ClaimAssignmentId { get; set; }
        public string Findings { get; set; }
        public string Result { get; set; } // PASS, NEEDS_REVISION, ESCALATE
        public string IssuesFound { get; set; }
    }

    /// <summary>
    /// DTO for permission audit log
    /// </summary>
    public class PermissionAuditLogDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }
        public string ResourceType { get; set; }
        public int ResourceId { get; set; }
        public string Details { get; set; }
        public string UserRoleAtTime { get; set; }
        public string IpAddress { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for team metrics
    /// </summary>
    public class TeamMetricsDto
    {
        public int SupervisorId { get; set; }
        public int TeamSize { get; set; }
        public int PendingReviews { get; set; }
        public int CompletedToday { get; set; }
        public double AverageCompletionTime { get; set; } // in minutes
        public double AccuracyRate { get; set; } // percentage
        public double ApprovalRate { get; set; } // for medical reviews
        public double DenialRate { get; set; } // for medical reviews
        public List<ReviewerPerformanceDto> ReviewerPerformance { get; set; } = new();
    }

    /// <summary>
    /// DTO for individual reviewer performance
    /// </summary>
    public class ReviewerPerformanceDto
    {
        public int ReviewerId { get; set; }
        public string ReviewerName { get; set; }
        public string Role { get; set; }
        public int ClaimsReviewedToday { get; set; }
        public int ClaimsReviewedThisWeek { get; set; }
        public double AverageTimeMinutes { get; set; }
        public double AccuracyPercentage { get; set; }
        public double ApprovalRate { get; set; }
        public int QAIssuesFound { get; set; }
        public string PerformanceRating { get; set; } // Excellent, Good, Average, Poor
    }

    /// <summary>
    /// DTO for role hierarchy display
    /// </summary>
    public class RoleHierarchyDto
    {
        public string Department { get; set; } // TECHNICAL or MEDICAL
        public List<RoleLevelDto> Levels { get; set; } = new();
    }

    /// <summary>
    /// DTO for role at specific level
    /// </summary>
    public class RoleLevelDto
    {
        public int Level { get; set; }
        public string LevelName { get; set; } // Reviewer, Senior, Supervisor, Manager
        public List<RoleDto> Roles { get; set; } = new();
    }

    /// <summary>
    /// DTO for claim routing rules
    /// </summary>
    public class ClaimRoutingRuleDto
    {
        public int Id { get; set; }
        public string ReviewType { get; set; } // TECHNICAL or MEDICAL
        public int MinComplexity { get; set; }
        public int MaxComplexity { get; set; }
        public string TargetRole { get; set; }
        public int QASamplingPercentage { get; set; }
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO for department-wide metrics
    /// </summary>
    public class DepartmentMetricsDto
    {
        public string Department { get; set; }
        public int TotalStaff { get; set; }
        public int PendingReviews { get; set; }
        public int CompletedToday { get; set; }
        public int CompletedThisWeek { get; set; }
        public double AverageAccuracy { get; set; }
        public double AverageCompletionTimeMinutes { get; set; }
        public double ApprovalRatePercentage { get; set; }
        public double DenialRatePercentage { get; set; }
        public int QAIssuesFound { get; set; }
        public List<SupervisorMetricsDto> SupervisorMetrics { get; set; } = new();
    }

    /// <summary>
    /// DTO for supervisor metrics
    /// </summary>
    public class SupervisorMetricsDto
    {
        public int SupervisorId { get; set; }
        public string SupervisorName { get; set; }
        public int TeamSize { get; set; }
        public int PendingReviews { get; set; }
        public double TeamAccuracy { get; set; }
        public double TeamApprovalRate { get; set; }
        public int QAReviewsPerformed { get; set; }
        public int IssuesFound { get; set; }
    }
}
