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
    /// Implementation of claim routing and assignment service based on complexity
    /// </summary>
    public class ClaimRoutingService : IClaimRoutingService
    {
        private readonly IRepository<ClaimAssignment> _claimAssignmentRepository;
        private readonly IRepository<Claim> _claimRepository;
        private readonly IRepository<User> _userRepository;
        private readonly IRepository<UserRole> _userRoleRepository;
        private readonly IRepository<Role> _roleRepository;
        private readonly IRepository<QAReview> _qaReviewRepository;
        private readonly IUserRoleService _userRoleService;
        private readonly ISupervisorService _supervisorService;
        private readonly IPermissionAuditService _auditService;

        public ClaimRoutingService(
        IRepository<ClaimAssignment> claimAssignmentRepository,
             IRepository<Claim> claimRepository,
      IRepository<User> userRepository,
                 IRepository<UserRole> userRoleRepository,
     IRepository<Role> roleRepository,
        IRepository<QAReview> qaReviewRepository,
           IUserRoleService userRoleService,
            ISupervisorService supervisorService,
       IPermissionAuditService auditService)
        {
            _claimAssignmentRepository = claimAssignmentRepository;
            _claimRepository = claimRepository;
            _userRepository = userRepository;
            _userRoleRepository = userRoleRepository;
            _roleRepository = roleRepository;
            _qaReviewRepository = qaReviewRepository;
            _userRoleService = userRoleService;
            _supervisorService = supervisorService;
            _auditService = auditService;
        }

        #region Claim Assignment

        public async Task<ClaimAssignmentDto> AssignClaimAsync(AssignClaimDto dto)
        {
            // Calculate complexity score
            var claim = await _claimRepository.GetByIdAsync(dto.ClaimId.ToString());
            if (claim == null)
                throw new InvalidOperationException($"Claim {dto.ClaimId} not found");

            var complexityScore = CalculateComplexityScore(claim);

            // Find available reviewer
            int assignedToUserId;
            if (dto.PreferredReviewerId.HasValue)
            {
                assignedToUserId = dto.PreferredReviewerId.Value;
            }
            else
            {
                assignedToUserId = await FindAvailableReviewerAsync(dto.ReviewType, complexityScore);
            }

            if (assignedToUserId == 0)
                throw new InvalidOperationException($"No available reviewers for {dto.ReviewType} review with complexity {complexityScore}");

            // Get user's role
            var userRoles = await _userRoleService.GetUserRolesAsync(assignedToUserId);
            var primaryRole = userRoles.FirstOrDefault(ur => ur.IsPrimary);
            if (primaryRole == null)
                throw new InvalidOperationException($"User {assignedToUserId} has no primary role");

            // Create assignment
            var assignment = new ClaimAssignment
            {
                ClaimId = dto.ClaimId,
                ComplexityScore = complexityScore,
                ReviewType = dto.ReviewType,
                AssignedToUserId = assignedToUserId,
                AssignedRole = primaryRole.RoleName,
                AssignedAt = DateTime.UtcNow,
                IsQAReview = ShouldApplyQASampling(complexityScore)
            };

            var result = await _claimAssignmentRepository.AddAsync(assignment);
            await _claimAssignmentRepository.SaveChangesAsync();

            // Audit log
            await _auditService.LogActionAsync(
                0,
                "CLAIM_ASSIGNED",
                "CLAIM",
                       dto.ClaimId,
                  $"Assigned to {assignedToUserId} for {dto.ReviewType} review");

            return MapToDto(result);
        }

        public async Task<ClaimAssignmentDto?> GetClaimAssignmentAsync(int claimAssignmentId)
        {
            var assignment = await _claimAssignmentRepository.GetByIdAsync(claimAssignmentId.ToString());
            return assignment is null ? null : MapToDto(assignment);
        }

        public async Task<List<ClaimAssignmentDto>> GetUserQueueAsync(int userId, string reviewType)
        {
            var assignments = await _claimAssignmentRepository.GetAllAsync();
            var userQueue = assignments
               .Where(ca => ca.AssignedToUserId == userId &&
           ca.CompletedAt == null &&
          (string.IsNullOrEmpty(reviewType) || ca.ReviewType == reviewType))
        .ToList();

            var result = new List<ClaimAssignmentDto>();
            foreach (var assignment in userQueue)
            {
                result.Add(MapToDto(assignment));
            }

            return result;
        }

        public async Task<List<ClaimAssignmentDto>> GetPendingClaimsAsync(string? reviewType = null)
        {
            var assignments = await _claimAssignmentRepository.GetAllAsync();
            var pending = assignments
            .Where(ca => ca.CompletedAt == null &&
           (string.IsNullOrEmpty(reviewType) || ca.ReviewType == reviewType))
           .ToList();

            var result = new List<ClaimAssignmentDto>();
            foreach (var assignment in pending)
            {
                result.Add(MapToDto(assignment));
            }

            return result;
        }

        #endregion

        #region Complexity Scoring

        public int CalculateComplexityScore(Claim claim)
        {
            int score = 0;

            // Check number of diagnoses (more = more complex)
            if (claim.Diagnoses != null)
            {
                score += Math.Min(claim.Diagnoses.Count, 5);
            }

            // Check number of items (more = more complex)
            if (claim.Items != null)
            {
                score += Math.Min(claim.Items.Count, 5);
            }

            // Check for high-value claims
            if (claim.Total > 10000) score += 3;
            else if (claim.Total > 5000) score += 2;
            else if (claim.Total > 1000) score += 1;

            // Check for emergency/urgent
            if (claim.ClaimSubType == "er") score += 2;

            // Check for multiple provider types (Insurance has multiple coverage)
            if (claim.Coverage != null) score += 1;

            // Check for appeal/resubmission
            if (!string.IsNullOrEmpty(claim.Status) && claim.Status.Contains("appeal")) score += 3;

            // Cap at 10
            return Math.Min(score, 10);
        }

        public string GetTargetRoleForComplexity(string reviewType, int complexityScore)
        {
            if (complexityScore <= 3)
            {
                return reviewType == "TECHNICAL" ? "TECHNICAL_REVIEWER" : "MEDICAL_REVIEWER";
            }
            else if (complexityScore <= 6)
            {
                return reviewType == "TECHNICAL" ? "SENIOR_TECHNICAL_REVIEWER" : "SENIOR_MEDICAL_REVIEWER";
            }
            else if (complexityScore <= 9)
            {
                return reviewType == "TECHNICAL" ? "TECHNICAL_REVIEW_SUPERVISOR" : "MEDICAL_REVIEW_SUPERVISOR";
            }
            else
            {
                return reviewType == "TECHNICAL" ? "TECHNICAL_REVIEW_MANAGER" : "MEDICAL_REVIEW_MANAGER";
            }
        }

        #endregion

        #region Reviewer Selection

        public async Task<int> FindAvailableReviewerAsync(string reviewType, int complexityScore)
        {
            var targetRole = GetTargetRoleForComplexity(reviewType, complexityScore);
            var roles = await _roleRepository.GetAllAsync();
            var role = roles.FirstOrDefault(r => r.Name == targetRole);

            if (role == null)
                return 0;

            // Get users with this role
            var userRoles = await _userRoleRepository.GetAllAsync();
            var usersWithRole = userRoles
              .Where(ur => ur.RoleId == int.Parse(role.Id) && ur.RemovedAt == null)
                    .Select(ur => ur.UserId)
              .ToList();

            if (!usersWithRole.Any())
                return 0;

            // Find reviewer with least pending claims
            var assignments = await _claimAssignmentRepository.GetAllAsync();
            var reviewerLoads = usersWithRole
   .Select(userId => new
   {
       UserId = userId,
       PendingCount = assignments.Count(ca =>
      ca.AssignedToUserId == userId &&
          ca.CompletedAt == null &&
          ca.ReviewType == reviewType)
   })
     .OrderBy(x => x.PendingCount)
     .ToList();

            return reviewerLoads.FirstOrDefault()?.UserId ?? 0;
        }

        public async Task<int> FindAvailableSupervisorAsync(string department, int complexityScore)
        {
            // Get supervisor role for department
            string supervisorRoleName = department == "TECHNICAL" ? "TECHNICAL_REVIEW_SUPERVISOR" : "MEDICAL_REVIEW_SUPERVISOR";
            var roles = await _roleRepository.GetAllAsync();
            var supervisorRole = roles.FirstOrDefault(r => r.Name == supervisorRoleName);

            if (supervisorRole == null)
                return 0;

            // Get users with supervisor role
            var userRoles = await _userRoleRepository.GetAllAsync();
            var supervisorIds = userRoles
     .Where(ur => ur.RoleId == int.Parse(supervisorRole.Id) && ur.RemovedAt == null)
.Select(ur => ur.UserId)
    .ToList();

    if (!supervisorIds.Any())
      return 0;

   // Find supervisor with least pending QA reviews
   var qaReviews = await _qaReviewRepository.GetAllAsync();
 var supervisorLoads = supervisorIds
       .Select(supId => new
  {
        UserId = supId,
      PendingCount = qaReviews.Count(qa =>
  qa.ReviewedByUserId == supId &&
    qa.Result == null) // Not completed
     })
    .OrderBy(x => x.PendingCount)
.ToList();

    return supervisorLoads.FirstOrDefault()?.UserId ?? 0;
   }

        #endregion

        #region Review Submission

        public async Task<bool> SubmitReviewAsync(int claimAssignmentId, SubmitReviewDto dto, int reviewerId)
        {
            var assignment = await _claimAssignmentRepository.GetByIdAsync(claimAssignmentId.ToString());
            if (assignment == null)
                return false;

            assignment.Result = dto.Result;
            assignment.Comments = dto.Comments;
            assignment.CompletedAt = DateTime.UtcNow;

            if (dto.EscalatedToUserId.HasValue)
            {
                assignment.EscalatedToUserId = dto.EscalatedToUserId;
            }

            _claimAssignmentRepository.Update(assignment);
            await _claimAssignmentRepository.SaveChangesAsync();

            // Audit log
            await _auditService.LogActionAsync(
                      reviewerId,
                "CLAIM_REVIEW_SUBMITTED",
            "CLAIM",
                assignment.ClaimId,
                   $"Result: {dto.Result}");

            return true;
        }

        public async Task<bool> EscalateClaimAsync(int claimAssignmentId, int escalatedToUserId, string reason)
        {
            var assignment = await _claimAssignmentRepository.GetByIdAsync(claimAssignmentId.ToString());
            if (assignment == null)
                return false;

            assignment.EscalatedToUserId = escalatedToUserId;
            assignment.Comments = $"ESCALATED: {reason}";

            _claimAssignmentRepository.Update(assignment);
            await _claimAssignmentRepository.SaveChangesAsync();

            // Audit log
            await _auditService.LogActionAsync(
0,
   "CLAIM_ESCALATED",
      "CLAIM",
 assignment.ClaimId,
 $"Escalated to {escalatedToUserId}: {reason}");

            return true;
        }

        #endregion

        #region QA Queue Management

        public async Task<List<ClaimAssignmentDto>> GetQAQueueAsync(int supervisorId)
        {
            var assignments = await _claimAssignmentRepository.GetAllAsync();
            var qaQueue = assignments
             .Where(ca => ca.IsQAReview && ca.CompletedAt.HasValue)
                .ToList();

            var result = new List<ClaimAssignmentDto>();
            foreach (var assignment in qaQueue)
            {
                result.Add(MapToDto(assignment));
            }

            return result;
        }

        public async Task<bool> SubmitQAReviewAsync(int claimAssignmentId, SubmitQAReviewDto dto, int reviewerId)
        {
            var assignment = await _claimAssignmentRepository.GetByIdAsync(claimAssignmentId.ToString());
            if (assignment == null)
                return false;

            assignment.QAResult = dto.Result;

            var qaReview = new QAReview
            {
                ClaimAssignmentId = claimAssignmentId,
                ReviewedByUserId = reviewerId,
                Findings = dto.Findings,
                Result = dto.Result,
                IssuesFound = dto.IssuesFound,
                ReviewedAt = DateTime.UtcNow
            };

            await _qaReviewRepository.AddAsync(qaReview);
            _claimAssignmentRepository.Update(assignment);
            await _claimAssignmentRepository.SaveChangesAsync();

            // Audit log
            await _auditService.LogActionAsync(
          reviewerId,
            "QA_REVIEW_SUBMITTED",
                   "CLAIM",
                   assignment.ClaimId,
              $"QA Result: {dto.Result}");

            return true;
        }

        #endregion

        #region Metrics

        public async Task<List<ClaimAssignmentDto>> GetReviewerMetricsAsync(int reviewerId, int days = 7)
        {
            var assignments = await _claimAssignmentRepository.GetAllAsync();
            var fromDate = DateTime.UtcNow.AddDays(-days);

            var metrics = assignments
                   .Where(ca => ca.AssignedToUserId == reviewerId && ca.AssignedAt >= fromDate)
              .ToList();

            var result = new List<ClaimAssignmentDto>();
            foreach (var assignment in metrics)
            {
                result.Add(MapToDto(assignment));
            }

            return result;
        }

        #endregion

        #region Helper Methods

        private bool ShouldApplyQASampling(int complexityScore)
        {
            // Always QA review for complex claims
            if (complexityScore >= 7) return true;

            // Sample 10-15% of moderate claims
            if (complexityScore >= 4)
            {
                var random = new Random();
                return random.Next(100) < 15;
            }

            // Sample 5% of simple claims
            var rand = new Random();
            return rand.Next(100) < 5;
        }

        private ClaimAssignmentDto MapToDto(ClaimAssignment assignment)
        {

            return new ClaimAssignmentDto
            {
                Id = assignment.Id,
                ClaimId = assignment.ClaimId,
                ComplexityScore = assignment.ComplexityScore,
                ReviewType = assignment.ReviewType,
                AssignedToUserId = assignment.AssignedToUserId,
                AssignedRole = assignment.AssignedRole,
                AssignedAt = assignment.AssignedAt,
                CompletedAt = assignment.CompletedAt,
                Result = assignment.Result,
                Comments = assignment.Comments,
                EscalatedToUserId = assignment.EscalatedToUserId,
                IsQAReview = assignment.IsQAReview,
                QAResult = assignment.QAResult
            };
        }

        #endregion
    }
}
