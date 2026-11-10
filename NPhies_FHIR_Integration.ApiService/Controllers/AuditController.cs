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
    /// API controller for permission audit logging and compliance reporting
    /// </summary>
   [ApiController]
    [Route("api/[controller]")]
   [Authorize]
    [Produces("application/json")]
    public class AuditController : ControllerBase
    {
       private readonly IPermissionAuditService _auditService;
    private readonly ILogger<AuditController> _logger;

        public AuditController(IPermissionAuditService auditService, ILogger<AuditController> logger)
  {
      _auditService = auditService;
  _logger = logger;
  }

  /// <summary>
        /// Get audit logs for a user
    /// </summary>
        [HttpGet("user/{userId}")]
        [Authorize(Roles = "TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_MANAGER")]
     [ProducesResponseType(typeof(List<PermissionAuditLogDto>), StatusCodes.Status200OK)]
       [ProducesResponseType(StatusCodes.Status404NotFound)]
     public async Task<ActionResult<List<PermissionAuditLogDto>>> GetUserAuditLogs(int userId)
        {
    try
      {
  _logger.LogInformation("Getting audit logs for user {UserId}", userId);
   var logs = await _auditService.GetUserAuditLogsAsync(userId);
         return Ok(logs);
   }
     catch (Exception ex)
 {
      _logger.LogError(ex, "Error getting user audit logs");
  return StatusCode(StatusCodes.Status500InternalServerError, 
      new { message = "Error retrieving audit logs", error = ex.Message });
   }
        }

   /// <summary>
  /// Get audit logs for a resource
  /// </summary>
 [HttpGet("resource/{resourceType}/{resourceId}")]
      [Authorize(Roles = "TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_MANAGER")]
       [ProducesResponseType(typeof(List<PermissionAuditLogDto>), StatusCodes.Status200OK)]
       public async Task<ActionResult<List<PermissionAuditLogDto>>> GetResourceAuditLogs(string resourceType, int resourceId)
      {
  try
        {
 _logger.LogInformation("Getting audit logs for resource {ResourceType} {ResourceId}", resourceType, resourceId);
  var logs = await _auditService.GetResourceAuditLogsAsync(resourceType, resourceId);
   return Ok(logs);
   }
    catch (Exception ex)
      {
   _logger.LogError(ex, "Error getting resource audit logs");
       return StatusCode(StatusCodes.Status500InternalServerError, 
    new { message = "Error retrieving audit logs", error = ex.Message });
 }
  }

 /// <summary>
      /// Get audit logs by action
    /// </summary>
   [HttpGet("action/{action}")]
  [Authorize(Roles = "TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_MANAGER")]
 [ProducesResponseType(typeof(List<PermissionAuditLogDto>), StatusCodes.Status200OK)]
  public async Task<ActionResult<List<PermissionAuditLogDto>>> GetAuditLogsByAction(string action)
   {
try
       {
     _logger.LogInformation("Getting audit logs for action {Action}", action);
    var logs = await _auditService.GetAuditLogsByActionAsync(action);
        return Ok(logs);
 }
         catch (Exception ex)
  {
_logger.LogError(ex, "Error getting audit logs by action");
 return StatusCode(StatusCodes.Status500InternalServerError, 
        new { message = "Error retrieving audit logs", error = ex.Message });
   }
 }

  /// <summary>
        /// Get audit logs by date range
        /// </summary>
 [HttpGet("range")]
        [Authorize(Roles = "TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_MANAGER")]
 [ProducesResponseType(typeof(List<PermissionAuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PermissionAuditLogDto>>> GetAuditLogsByDateRange(
     [FromQuery] DateTime fromDate,
            [FromQuery] DateTime toDate)
 {
    try
       {
      if (fromDate > toDate)
     return BadRequest(new { message = "fromDate must be less than or equal to toDate" });

  _logger.LogInformation("Getting audit logs from {FromDate} to {ToDate}", fromDate, toDate);
    var logs = await _auditService.GetAuditLogsAsync(fromDate, toDate);
    return Ok(logs);
 }
      catch (Exception ex)
       {
      _logger.LogError(ex, "Error getting audit logs by date range");
        return StatusCode(StatusCodes.Status500InternalServerError, 
  new { message = "Error retrieving audit logs", error = ex.Message });
      }
    }

/// <summary>
      /// Get unauthorized access attempts (last 30 days by default)
  /// </summary>
 [HttpGet("unauthorized-attempts")]
[Authorize(Roles = "TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_MANAGER")]
     [ProducesResponseType(typeof(List<PermissionAuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<PermissionAuditLogDto>>> GetUnauthorizedAccessAttempts([FromQuery] int days = 30)
  {
     try
     {
    if (days <= 0)
   return BadRequest(new { message = "days must be greater than 0" });

          _logger.LogInformation("Getting unauthorized access attempts for last {Days} days", days);
     var attempts = await _auditService.GetUnauthorizedAccessAttemptsAsync(days);
      return Ok(attempts);
      }
     catch (Exception ex)
 {
 _logger.LogError(ex, "Error getting unauthorized access attempts");
      return StatusCode(StatusCodes.Status500InternalServerError, 
    new { message = "Error retrieving unauthorized attempts", error = ex.Message });
  }
      }

 /// <summary>
      /// Get high-risk actions
  /// </summary>
    [HttpGet("high-risk-actions")]
 [Authorize(Roles = "TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_MANAGER")]
        [ProducesResponseType(typeof(List<PermissionAuditLogDto>), StatusCodes.Status200OK)]
       public async Task<ActionResult<List<PermissionAuditLogDto>>> GetHighRiskActions()
       {
    try
    {
   _logger.LogInformation("Getting high-risk actions");
    var actions = await _auditService.GetHighRiskActionsAsync();
    return Ok(actions);
        }
       catch (Exception ex)
 {
       _logger.LogError(ex, "Error getting high-risk actions");
   return StatusCode(StatusCodes.Status500InternalServerError, 
   new { message = "Error retrieving high-risk actions", error = ex.Message });
          }
        }

  /// <summary>
        /// Get compliance summary report
   /// </summary>
    [HttpGet("compliance-summary")]
  [Authorize(Roles = "TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_MANAGER")]
       [ProducesResponseType(typeof(ComplianceSummaryDto), StatusCodes.Status200OK)]
  public async Task<ActionResult<ComplianceSummaryDto>> GetComplianceSummary([FromQuery] int days = 30)
   {
    try
 {
     _logger.LogInformation("Getting compliance summary for last {Days} days", days);
    var fromDate = DateTime.UtcNow.AddDays(-days);
 var toDate = DateTime.UtcNow;

        var allLogs = await _auditService.GetAuditLogsAsync(fromDate, toDate);
  var unauthorizedAttempts = await _auditService.GetUnauthorizedAccessAttemptsAsync(days);
   var highRiskActions = await _auditService.GetHighRiskActionsAsync();

         var summary = new ComplianceSummaryDto
    {
   TotalAuditLogs = allLogs.Count,
 UnauthorizedAttempts = unauthorizedAttempts.Count,
  HighRiskActions = highRiskActions.Count,
     DateRange = new { from = fromDate, to = toDate },
    ComplianceScore = ComplianceHelper.CalculateComplianceScore(allLogs.Count, unauthorizedAttempts.Count, highRiskActions.Count)
      };

return Ok(summary);
   }
       catch (Exception ex)
    {
     _logger.LogError(ex, "Error getting compliance summary");
  return StatusCode(StatusCodes.Status500InternalServerError, 
         new { message = "Error retrieving compliance summary", error = ex.Message });
       }
        }
   }

 /// <summary>
  /// Compliance summary DTO
 /// </summary>
    public class ComplianceSummaryDto
    {
      public int TotalAuditLogs { get; set; }
  public int UnauthorizedAttempts { get; set; }
     public int HighRiskActions { get; set; }
    public dynamic DateRange { get; set; }
    public double ComplianceScore { get; set; }
    }

    /// <summary>
    /// Helper class for compliance calculations
    /// </summary>
    internal static class ComplianceHelper
    {
    /// <summary>
     /// Calculate compliance score based on audit metrics
      /// </summary>
        public static double CalculateComplianceScore(int totalLogs, int unauthorizedAttempts, int highRiskActions)
  {
            // Base score = 100
       double score = 100.0;

   // Deduct for unauthorized attempts (5 points each, max 30)
        score -= Math.Min(unauthorizedAttempts * 5, 30);

            // Deduct for high-risk actions (2 points each, max 20)
    score -= Math.Min(highRiskActions * 2, 20);

            // Bonus for good logging (if more than 1000 logs in period, add 10 points)
            if (totalLogs > 1000)
  score += 10;

            // Ensure score is between 0 and 100
          return Math.Max(0, Math.Min(100, score));
        }
    }
}
