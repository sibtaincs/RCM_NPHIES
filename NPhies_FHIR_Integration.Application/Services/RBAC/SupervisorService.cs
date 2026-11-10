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
    /// Implementation of supervisor management and team metrics service
    /// </summary>
    public class SupervisorService : ISupervisorService
    {
        private readonly IRepository<SupervisorAssignment> _supervisorRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly IRepository<ClaimAssignment> _claimAssignmentRepository;
        private readonly IRepository<QAReview> _qaReviewRepository;
        private readonly IPermissionAuditService _auditService;

        public SupervisorService(
      IRepository<SupervisorAssignment> supervisorRepository,
    IRepository<User> userRepository,
      IRepository<Role> roleRepository,
      IRepository<ClaimAssignment> claimAssignmentRepository,
     IRepository<QAReview> qaReviewRepository,
       IPermissionAuditService auditService)
        {
            _supervisorRepository = supervisorRepository;
            _userRepository = userRepository;
            _roleRepository = roleRepository;
            _claimAssignmentRepository = claimAssignmentRepository;
            _qaReviewRepository = qaReviewRepository;
            _auditService = auditService;
        }

        #region Supervisor Assignments

        public async Task<SupervisorAssignmentDto> CreateSupervisorAssignmentAsync(CreateSupervisorAssignmentDto dto)
        {
            // Check if assignment already exists
            var existing = await _supervisorRepository.GetAllAsync();
            var alreadyExists = existing.FirstOrDefault(sa =>
                     sa.SupervisorUserId == dto.SupervisorUserId &&
              sa.SubordinateUserId == dto.SubordinateUserId &&
                 sa.RemovedAt == null);

            if (alreadyExists != null)
                throw new InvalidOperationException("Supervisor assignment already exists");

            // Get supervisor's role - note: User entity doesn't have RoleId, get it from UserRole
            var supervisor = await _userRepository.GetByIdAsync(dto.SupervisorUserId.ToString());
            if (supervisor == null)
                throw new InvalidOperationException("Supervisor user not found");

            var assignment = new SupervisorAssignment
            {
                SupervisorUserId = dto.SupervisorUserId,
                SubordinateUserId = dto.SubordinateUserId,
                Department = dto.Department,
                SupervisorRoleId = 0, // TODO: Get from UserRole mapping
                TeamSize = dto.TeamSize,
                AssignedAt = DateTime.UtcNow
            };

            var result = await _supervisorRepository.AddAsync(assignment);
            await _supervisorRepository.SaveChangesAsync();

            // Audit log
            await _auditService.LogActionAsync(
       dto.SupervisorUserId,
     "SUPERVISOR_ASSIGNMENT_CREATED",
        "USER",
    dto.SubordinateUserId,
      $"Assigned {dto.SubordinateUserId} as subordinate in {dto.Department}");

            return MapToDto(result);
        }

        public async Task<SupervisorAssignmentDto?> GetSupervisorAssignmentAsync(int supervisorId, int subordinateId)
        {
            var assignments = await _supervisorRepository.GetAllAsync();
            var assignment = assignments.FirstOrDefault(sa =>
              sa.SupervisorUserId == supervisorId &&
        sa.SubordinateUserId == subordinateId &&
        sa.RemovedAt == null);

            if (assignment == null)
                return null;

            return await MapToDtoAsync(assignment);
        }

        public async Task<List<SupervisorAssignmentDto>> GetSupervisorTeamAsync(int supervisorId)
        {
            var assignments = await _supervisorRepository.GetAllAsync();
            var team = assignments
              .Where(sa => sa.SupervisorUserId == supervisorId && sa.RemovedAt == null)
         .ToList();

            var result = new List<SupervisorAssignmentDto>();
            foreach (var assignment in team)
            {
                result.Add(await MapToDtoAsync(assignment));
            }

            return result;
        }

        public async Task<List<SupervisorAssignmentDto>> GetSubordinatesSupervisorsAsync(int subordinateId)
        {
            var assignments = await _supervisorRepository.GetAllAsync();
            var supervisors = assignments
               .Where(sa => sa.SubordinateUserId == subordinateId && sa.RemovedAt == null)
                     .ToList();

            var result = new List<SupervisorAssignmentDto>();
            foreach (var assignment in supervisors)
            {
                result.Add(await MapToDtoAsync(assignment));
            }

            return result;
        }

        public async Task<List<SupervisorAssignmentDto>> GetManagerSupervisorsAsync(int managerId)
        {
            // Get all supervisors that report to this manager
            var assignments = await _supervisorRepository.GetAllAsync();
            var supervisors = assignments
                  .Where(sa => sa.SupervisorUserId == managerId && sa.RemovedAt == null)
              .ToList();

            var result = new List<SupervisorAssignmentDto>();
            foreach (var assignment in supervisors)
            {
                result.Add(await MapToDtoAsync(assignment));
            }

            return result;
        }

        public async Task<bool> RemoveSupervisorAssignmentAsync(int supervisorId, int subordinateId)
        {
            var assignments = await _supervisorRepository.GetAllAsync();
            var assignment = assignments.FirstOrDefault(sa =>
             sa.SupervisorUserId == supervisorId &&
           sa.SubordinateUserId == subordinateId &&
              sa.RemovedAt == null);

            if (assignment == null)
                return false;

            assignment.RemovedAt = DateTime.UtcNow;
            _supervisorRepository.Update(assignment);
            await _supervisorRepository.SaveChangesAsync();

            // Audit log
            await _auditService.LogActionAsync(
    supervisorId,
          "SUPERVISOR_ASSIGNMENT_REMOVED",
     "USER",
              subordinateId,
         "Removed supervisor assignment");

            return true;
        }

        #endregion

        #region Supervisor Queries

        public async Task<int> GetTeamSizeAsync(int supervisorId)
        {
            var team = await GetSupervisorTeamAsync(supervisorId);
            return team.Count;
        }

        public async Task<bool> IsSupervisorOfAsync(int supervisorId, int subordinateId)
        {
            var assignment = await GetSupervisorAssignmentAsync(supervisorId, subordinateId);
            return assignment != null;
        }

        public async Task<List<int>> GetAllSubordinatesRecursiveAsync(int supervisorId)
        {
            var result = new List<int>();
            var directSubordinates = await GetSupervisorTeamAsync(supervisorId);

            foreach (var subordinate in directSubordinates)
            {
                result.Add(subordinate.SubordinateUserId);

                // Recursively get subordinates of this subordinate
                var indirectSubordinates = await GetAllSubordinatesRecursiveAsync(subordinate.SubordinateUserId);
                result.AddRange(indirectSubordinates);
            }

            return result;
        }

        #endregion

        #region Team Management

        public async Task<TeamMetricsDto> GetTeamMetricsAsync(int supervisorId)
        {
            var team = await GetSupervisorTeamAsync(supervisorId);
            var subordinateIds = team.Select(t => t.SubordinateUserId).ToList();

            var claimAssignments = await _claimAssignmentRepository.GetAllAsync();
            var qaReviews = await _qaReviewRepository.GetAllAsync();

            var today = DateTime.UtcNow.Date;
            var pendingAssignments = claimAssignments
     .Where(ca => subordinateIds.Contains(ca.AssignedToUserId) && ca.CompletedAt == null)
        .ToList();

            var completedToday = claimAssignments
               .Where(ca => subordinateIds.Contains(ca.AssignedToUserId) &&
             ca.CompletedAt.HasValue &&
              ca.CompletedAt.Value.Date == today)
             .ToList();

            var allTeamAssignments = claimAssignments
        .Where(ca => subordinateIds.Contains(ca.AssignedToUserId))
            .ToList();

            var qaForTeam = qaReviews
                   .Where(qa => subordinateIds.Contains(qa.ReviewedByUserId))
        .ToList();

            var avgTimeMinutes = allTeamAssignments.Any()
             ? allTeamAssignments
                 .Where(ca => ca.CompletedAt.HasValue)
        .Average(ca => (ca.CompletedAt!.Value - ca.AssignedAt).TotalMinutes)
              : 0;

            var accuracy = qaForTeam.Any()
     ? (double)qaForTeam.Count(qa => qa.Result == "PASS") / qaForTeam.Count() * 100
   : 0;

            var reviewerPerformance = new List<ReviewerPerformanceDto>();
            foreach (var subordinate in team)
            {
                var subAssignments = allTeamAssignments
                   .Where(ca => ca.AssignedToUserId == subordinate.SubordinateUserId)
                       .ToList();

                var subCompletedToday = completedToday
                    .Where(ca => ca.AssignedToUserId == subordinate.SubordinateUserId)
             .ToList();

                if (subAssignments.Any())
                {
                    reviewerPerformance.Add(new ReviewerPerformanceDto
                    {
                        ReviewerId = subordinate.SubordinateUserId,
                        ReviewerName = subordinate.SubordinateName,
                        Role = subordinate.SupervisorRole,
                        ClaimsReviewedToday = subCompletedToday.Count,
                        ClaimsReviewedThisWeek = subAssignments.Count(ca => (DateTime.UtcNow - ca.AssignedAt).Days < 7),
                        AverageTimeMinutes = subAssignments
                 .Where(ca => ca.CompletedAt.HasValue)
             .Average(ca => (ca.CompletedAt!.Value - ca.AssignedAt).TotalMinutes),
                        AccuracyPercentage = qaForTeam.Any(qa => qa.ReviewedByUserId == subordinate.SubordinateUserId)
             ? (qaForTeam.Where(qa => qa.ReviewedByUserId == subordinate.SubordinateUserId).Count(qa => qa.Result == "PASS") /
                  (double)qaForTeam.Count(qa => qa.ReviewedByUserId == subordinate.SubordinateUserId)) * 100
          : 0,
                        ApprovalRate = subAssignments.Any(ca => ca.ReviewType == "MEDICAL")
               ? (subAssignments.Count(ca => ca.Result == "APPROVE") / (double)subAssignments.Count(ca => ca.ReviewType == "MEDICAL")) * 100
                    : 0
                    });
                }
            }

            return new TeamMetricsDto
            {
                SupervisorId = supervisorId,
                TeamSize = team.Count,
                PendingReviews = pendingAssignments.Count,
                CompletedToday = completedToday.Count,
                AverageCompletionTime = avgTimeMinutes,
                AccuracyRate = accuracy,
                ReviewerPerformance = reviewerPerformance
            };
        }

        public async Task<List<ReviewerPerformanceDto>> GetTeamPerformanceAsync(int supervisorId)
        {
            var metrics = await GetTeamMetricsAsync(supervisorId);
            return metrics.ReviewerPerformance;
        }

        #endregion

        #region Manager Queries

        public async Task<DepartmentMetricsDto> GetDepartmentMetricsAsync(int managerId)
        {
            // Get all supervisors under this manager
            var allSubordinates = await GetAllSubordinatesRecursiveAsync(managerId);
            allSubordinates.Add(managerId); // Include manager themselves

            var claimAssignments = await _claimAssignmentRepository.GetAllAsync();
            var qaReviews = await _qaReviewRepository.GetAllAsync();

            var deptAssignments = claimAssignments
                 .Where(ca => allSubordinates.Contains(ca.AssignedToUserId))
          .ToList();

            var today = DateTime.UtcNow.Date;
            var completedToday = deptAssignments
             .Where(ca => ca.CompletedAt.HasValue && ca.CompletedAt.Value.Date == today)
              .ToList();

            var completedWeek = deptAssignments
        .Where(ca => ca.CompletedAt.HasValue && (DateTime.UtcNow - ca.CompletedAt.Value).Days < 7)
        .ToList();

            var avgAccuracy = qaReviews.Any()
       ? (double)qaReviews.Count(qa => qa.Result == "PASS") / qaReviews.Count() * 100
       : 0;

            var avgTimeMinutes = deptAssignments.Any(ca => ca.CompletedAt.HasValue)
        ? deptAssignments
            .Where(ca => ca.CompletedAt.HasValue)
         .Average(ca => (ca.CompletedAt!.Value - ca.AssignedAt).TotalMinutes)
         : 0;

            var approvalRate = deptAssignments.Where(ca => ca.ReviewType == "MEDICAL").Any()
            ? (deptAssignments.Count(ca => ca.Result == "APPROVE" && ca.ReviewType == "MEDICAL") /
                  (double)deptAssignments.Count(ca => ca.ReviewType == "MEDICAL")) * 100
                  : 0;

            var denialRate = deptAssignments.Where(ca => ca.ReviewType == "MEDICAL").Any()
       ? (deptAssignments.Count(ca => ca.Result == "DENY" && ca.ReviewType == "MEDICAL") /
  (double)deptAssignments.Count(ca => ca.ReviewType == "MEDICAL")) * 100
  : 0;

            return new DepartmentMetricsDto
            {
                Department = "DEPARTMENT", // TODO: Get actual department
                TotalStaff = allSubordinates.Count,
                PendingReviews = deptAssignments.Count(ca => ca.CompletedAt == null),
                CompletedToday = completedToday.Count,
                CompletedThisWeek = completedWeek.Count,
                AverageAccuracy = avgAccuracy,
                AverageCompletionTimeMinutes = avgTimeMinutes,
                ApprovalRatePercentage = approvalRate,
                DenialRatePercentage = denialRate,
                QAIssuesFound = qaReviews.Count(qa => qa.Result != "PASS")
            };
        }

        #endregion

        #region Helper Methods

        private SupervisorAssignmentDto MapToDto(SupervisorAssignment assignment)
        {
            return new SupervisorAssignmentDto
            {
                Id = assignment.Id,
                SupervisorUserId = assignment.SupervisorUserId,
                SubordinateUserId = assignment.SubordinateUserId,
                Department = assignment.Department,
                SupervisorRoleId = assignment.SupervisorRoleId,
                TeamSize = assignment.TeamSize,
                AssignedAt = assignment.AssignedAt
            };
        }

        private async Task<SupervisorAssignmentDto> MapToDtoAsync(SupervisorAssignment assignment)
        {
            var supervisor = await _userRepository.GetByIdAsync(assignment.SupervisorUserId.ToString());
            var subordinate = await _userRepository.GetByIdAsync(assignment.SubordinateUserId.ToString());
            var role = await _roleRepository.GetByIdAsync(assignment.SupervisorRoleId.ToString());

            return new SupervisorAssignmentDto
            {
                Id = assignment.Id,
                SupervisorUserId = assignment.SupervisorUserId,
                SupervisorName = supervisor?.FirstName + " " + supervisor?.LastName ?? "Unknown",
                SubordinateUserId = assignment.SubordinateUserId,
                SubordinateName = subordinate?.FirstName + " " + subordinate?.LastName ?? "Unknown",
                Department = assignment.Department,
                SupervisorRoleId = assignment.SupervisorRoleId,
                SupervisorRole = role?.DisplayName ?? "Unknown",
                TeamSize = assignment.TeamSize,
                AssignedAt = assignment.AssignedAt
            };
        }

        #endregion
    }
}
