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
    /// API controller for supervisor and team management
    /// </summary>
    [ApiController]
  [Route("api/[controller]")]
    [Authorize]
  [Produces("application/json")]
    public class SupervisorsController : ControllerBase
    {
        private readonly ISupervisorService _supervisorService;
      private readonly ILogger<SupervisorsController> _logger;

   public SupervisorsController(ISupervisorService supervisorService, ILogger<SupervisorsController> logger)
        {
          _supervisorService = supervisorService;
    _logger = logger;
        }

     /// <summary>
     /// Create supervisor assignment
        /// </summary>
     [HttpPost("assign")]
      [Authorize(Roles = "TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_MANAGER")]
        [ProducesResponseType(typeof(SupervisorAssignmentDto), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SupervisorAssignmentDto>> CreateSupervisorAssignment([FromBody] CreateSupervisorAssignmentDto dto)
    {
       try
      {
      if (!ModelState.IsValid)
        return BadRequest(ModelState);

              _logger.LogInformation("Creating supervisor assignment for supervisor {SupervisorId} and subordinate {SubordinateId}", 
   dto.SupervisorUserId, dto.SubordinateUserId);
         var assignment = await _supervisorService.CreateSupervisorAssignmentAsync(dto);
       return CreatedAtAction(nameof(GetSupervisorAssignment), 
       new { supervisorId = assignment.SupervisorUserId, subordinateId = assignment.SubordinateUserId }, assignment);
  }
   catch (InvalidOperationException ex)
     {
   _logger.LogWarning(ex, "Invalid supervisor assignment");
    return BadRequest(new { message = ex.Message });
           }
  catch (Exception ex)
      {
  _logger.LogError(ex, "Error creating supervisor assignment");
         return StatusCode(StatusCodes.Status500InternalServerError, 
         new { message = "Error creating assignment", error = ex.Message });
  }
   }

   /// <summary>
        /// Get supervisor assignment
     /// </summary>
        [HttpGet("assignment/{supervisorId}/{subordinateId}")]
     [ProducesResponseType(typeof(SupervisorAssignmentDto), StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
     public async Task<ActionResult<SupervisorAssignmentDto>> GetSupervisorAssignment(int supervisorId, int subordinateId)
      {
    try
     {
    _logger.LogInformation("Getting supervisor assignment for supervisor {SupervisorId} and subordinate {SubordinateId}", 
    supervisorId, subordinateId);
  var assignment = await _supervisorService.GetSupervisorAssignmentAsync(supervisorId, subordinateId);
           if (assignment == null)
  return NotFound(new { message = "Supervisor assignment not found" });
     return Ok(assignment);
     }
  catch (Exception ex)
         {
      _logger.LogError(ex, "Error getting supervisor assignment");
    return StatusCode(StatusCodes.Status500InternalServerError, 
       new { message = "Error retrieving assignment", error = ex.Message });
       }
        }

 /// <summary>
        /// Get supervisor's team
    /// </summary>
       [HttpGet("team/{supervisorId}")]
        [ProducesResponseType(typeof(List<SupervisorAssignmentDto>), StatusCodes.Status200OK)]
 public async Task<ActionResult<List<SupervisorAssignmentDto>>> GetSupervisorTeam(int supervisorId)
    {
         try
    {
 _logger.LogInformation("Getting team for supervisor {SupervisorId}", supervisorId);
       var team = await _supervisorService.GetSupervisorTeamAsync(supervisorId);
      return Ok(team);
             }
     catch (Exception ex)
    {
    _logger.LogError(ex, "Error getting supervisor team");
       return StatusCode(StatusCodes.Status500InternalServerError, 
         new { message = "Error retrieving team", error = ex.Message });
        }
  }

        /// <summary>
 /// Remove supervisor assignment
    /// </summary>
     [HttpDelete("assignment/{supervisorId}/{subordinateId}")]
     [Authorize(Roles = "TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_MANAGER")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
   public async Task<IActionResult> RemoveSupervisorAssignment(int supervisorId, int subordinateId)
      {
    try
      {
  _logger.LogInformation("Removing supervisor assignment for supervisor {SupervisorId} and subordinate {SubordinateId}", 
           supervisorId, subordinateId);
      var result = await _supervisorService.RemoveSupervisorAssignmentAsync(supervisorId, subordinateId);
 if (!result)
          return NotFound(new { message = "Supervisor assignment not found" });
       return NoContent();
    }
         catch (Exception ex)
     {
      _logger.LogError(ex, "Error removing supervisor assignment");
        return StatusCode(StatusCodes.Status500InternalServerError, 
 new { message = "Error removing assignment", error = ex.Message });
  }
      }

    /// <summary>
       /// Get team size
        /// </summary>
        [HttpGet("team-size/{supervisorId}")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
      public async Task<ActionResult<int>> GetTeamSize(int supervisorId)
      {
      try
 {
_logger.LogInformation("Getting team size for supervisor {SupervisorId}", supervisorId);
        var teamSize = await _supervisorService.GetTeamSizeAsync(supervisorId);
 return Ok(teamSize);
  }
   catch (Exception ex)
        {
  _logger.LogError(ex, "Error getting team size");
       return StatusCode(StatusCodes.Status500InternalServerError, 
        new { message = "Error retrieving team size", error = ex.Message });
           }
        }

       /// <summary>
        /// Check if user is supervisor of another
        /// </summary>
 [HttpGet("is-supervisor-of/{supervisorId}/{subordinateId}")]
   [ProducesResponseType(typeof(bool), StatusCodes.Status200OK)]
       public async Task<ActionResult<bool>> IsSupervisorOf(int supervisorId, int subordinateId)
        {
    try
           {
_logger.LogInformation("Checking if {SupervisorId} is supervisor of {SubordinateId}", supervisorId, subordinateId);
var result = await _supervisorService.IsSupervisorOfAsync(supervisorId, subordinateId);
          return Ok(result);
     }
    catch (Exception ex)
          {
_logger.LogError(ex, "Error checking supervisor relationship");
  return StatusCode(StatusCodes.Status500InternalServerError, 
      new { message = "Error checking relationship", error = ex.Message });
            }
        }

       /// <summary>
        /// Get all subordinates recursively (entire hierarchy)
 /// </summary>
        [HttpGet("subordinates-recursive/{supervisorId}")]
        [ProducesResponseType(typeof(List<int>), StatusCodes.Status200OK)]
       public async Task<ActionResult<List<int>>> GetAllSubordinatesRecursive(int supervisorId)
        {
    try
          {
_logger.LogInformation("Getting all subordinates for supervisor {SupervisorId}", supervisorId);
   var subordinates = await _supervisorService.GetAllSubordinatesRecursiveAsync(supervisorId);
           return Ok(subordinates);
         }
     catch (Exception ex)
        {
_logger.LogError(ex, "Error getting subordinates");
    return StatusCode(StatusCodes.Status500InternalServerError, 
    new { message = "Error retrieving subordinates", error = ex.Message });
  }
        }

     /// <summary>
   /// Get team metrics
      /// </summary>
   [HttpGet("metrics/{supervisorId}")]
      [ProducesResponseType(typeof(TeamMetricsDto), StatusCodes.Status200OK)]
    [Authorize(Roles = "TECHNICAL_REVIEW_SUPERVISOR,TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_SUPERVISOR,MEDICAL_REVIEW_MANAGER")]
    public async Task<ActionResult<TeamMetricsDto>> GetTeamMetrics(int supervisorId)
        {
   try
  {
_logger.LogInformation("Getting team metrics for supervisor {SupervisorId}", supervisorId);
var metrics = await _supervisorService.GetTeamMetricsAsync(supervisorId);
  return Ok(metrics);
 }
    catch (Exception ex)
              {
    _logger.LogError(ex, "Error getting team metrics");
   return StatusCode(StatusCodes.Status500InternalServerError, 
         new { message = "Error retrieving metrics", error = ex.Message });
         }
        }

     /// <summary>
  /// Get team performance
        /// </summary>
      [HttpGet("performance/{supervisorId}")]
 [ProducesResponseType(typeof(List<ReviewerPerformanceDto>), StatusCodes.Status200OK)]
  [Authorize(Roles = "TECHNICAL_REVIEW_SUPERVISOR,TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_SUPERVISOR,MEDICAL_REVIEW_MANAGER")]
       public async Task<ActionResult<List<ReviewerPerformanceDto>>> GetTeamPerformance(int supervisorId)
        {
    try
     {
_logger.LogInformation("Getting team performance for supervisor {SupervisorId}", supervisorId);
      var performance = await _supervisorService.GetTeamPerformanceAsync(supervisorId);
             return Ok(performance);
         }
   catch (Exception ex)
     {
_logger.LogError(ex, "Error getting team performance");
    return StatusCode(StatusCodes.Status500InternalServerError, 
   new { message = "Error retrieving performance", error = ex.Message });
    }
    }

    /// <summary>
        /// Get department metrics
     /// </summary>
        [HttpGet("department-metrics/{managerId}")]
     [ProducesResponseType(typeof(DepartmentMetricsDto), StatusCodes.Status200OK)]
        [Authorize(Roles = "TECHNICAL_REVIEW_MANAGER,MEDICAL_REVIEW_MANAGER")]
       public async Task<ActionResult<DepartmentMetricsDto>> GetDepartmentMetrics(int managerId)
      {
       try
 {
_logger.LogInformation("Getting department metrics for manager {ManagerId}", managerId);
 var metrics = await _supervisorService.GetDepartmentMetricsAsync(managerId);
    return Ok(metrics);
            }
 catch (Exception ex)
  {
  _logger.LogError(ex, "Error getting department metrics");
          return StatusCode(StatusCodes.Status500InternalServerError, 
   new { message = "Error retrieving metrics", error = ex.Message });
    }
        }
    }
}
