using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.Services;
using NPhies_FHIR_Integration.Application.Services.RCM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Claim Status Controller
/// Handles NPHIES claim status inquiry operations
/// </summary>
[ApiController]
[Route("api/claims/status")]
[Authorize]
public class ClaimStatusController : BaseController
{
  private readonly IClaimStatusTracker _statusTracker;
    private readonly IClaimService _claimService;
    private readonly ILogger<ClaimStatusController> _logger;

    public ClaimStatusController(
        IClaimStatusTracker statusTracker,
        IClaimService claimService,
        ILogger<ClaimStatusController> logger)
    {
  _statusTracker = statusTracker ?? throw new ArgumentNullException(nameof(statusTracker));
        _claimService = claimService ?? throw new ArgumentNullException(nameof(claimService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Inquiry claim status (NPHIES status-check pattern)
    /// POST /api/claims/status/inquiry
    /// </summary>
    /// <param name="request">Status inquiry request</param>
    /// <returns>Current claim status with history</returns>
    [HttpPost("inquiry")]
    [ProducesResponseType(typeof(ClaimStatusInquiryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> InquiryClaimStatus([FromBody] ClaimStatusInquiryRequest request)
    {
        try
     {
    _logger.LogInformation("Status inquiry for claim {ClaimNumber}", request.ClaimNumber);

     if (string.IsNullOrEmpty(request.ClaimNumber))
  {
           return BadRequest(new { error = "ClaimNumber is required" });
   }

         // Get claim by number
            var claim = await _claimService.GetClaimByNumberAsync(request.ClaimNumber);
    
            if (claim == null)
            {
  _logger.LogWarning("Claim not found: {ClaimNumber}", request.ClaimNumber);
                return NotFound(new { error = "Claim not found", claimNumber = request.ClaimNumber });
        }

// Verify provider if specified
            if (!string.IsNullOrEmpty(request.ProviderId) && claim.ProviderId != request.ProviderId)
     {
   _logger.LogWarning("Provider mismatch for claim {ClaimNumber}", request.ClaimNumber);
    return BadRequest(new { error = "Claim does not belong to specified provider" });
       }

            // Get status information
          var status = await _statusTracker.GetClaimStatusAsync(claim.Id.ToString());
  var history = await _statusTracker.GetStatusHistoryAsync(claim.Id.ToString());

  var response = new ClaimStatusInquiryResponse
  {
            ClaimId = claim.Id.ToString(),
       ClaimNumber = claim.ClaimNumber,
       CurrentStatus = status?.CurrentStatus.ToString() ?? claim.Status,
   LastUpdated = status?.LastUpdated ?? claim.UpdatedAt ?? claim.CreatedAt,
  DaysInCurrentStatus = status?.DaysInCurrentStatus ?? 
   (int)(DateTime.UtcNow - (claim.UpdatedAt ?? claim.CreatedAt)).TotalDays,
    Description = status?.Description ?? $"Claim status: {claim.Status}",
      PatientId = claim.PatientId,
      ProviderId = claim.ProviderId,
      InsurerId = claim.InsurerId,
        SubmittedDate = claim.CreatedAt,
    TotalAmount = claim.Total,
    StatusHistory = history?.Select(h => new StatusHistoryItem
         {
    Status = h.Status.ToString(),
    StatusDate = h.StatusDate,
        Reason = h.Reason
    }).ToList() ?? new List<StatusHistoryItem>()
    };

  _logger.LogInformation("Status inquiry completed for claim {ClaimNumber}: {Status}", 
             request.ClaimNumber, response.CurrentStatus);

       return Ok(response);
   }
        catch (Exception ex)
{
          _logger.LogError(ex, "Error during status inquiry for claim {ClaimNumber}", request.ClaimNumber);
        return StatusCode(500, new { error = "An error occurred while inquiring claim status" });
   }
    }

    /// <summary>
    /// Batch status inquiry
    /// POST /api/claims/status/batch-inquiry
    /// </summary>
    /// <param name="request">Batch inquiry request</param>
    /// <returns>Status for multiple claims</returns>
    [HttpPost("batch-inquiry")]
    [ProducesResponseType(typeof(BatchStatusInquiryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BatchStatusInquiry([FromBody] BatchStatusInquiryRequest request)
    {
        try
        {
            _logger.LogInformation("Batch status inquiry for {Count} claims", request.ClaimNumbers?.Count ?? 0);

            if (request.ClaimNumbers == null || !request.ClaimNumbers.Any())
       {
    return BadRequest(new { error = "ClaimNumbers list is required and cannot be empty" });
            }

            if (request.ClaimNumbers.Count > 100)
            {
  return BadRequest(new { error = "Maximum 100 claims per batch inquiry" });
          }

          var results = new List<ClaimStatusSummary>();
         var notFound = new List<string>();

            foreach (var claimNumber in request.ClaimNumbers)
            {
    try
    {
        var claim = await _claimService.GetClaimByNumberAsync(claimNumber);
 
          if (claim == null)
                    {
        notFound.Add(claimNumber);
            continue;
              }

               // Verify provider if specified
       if (!string.IsNullOrEmpty(request.ProviderId) && claim.ProviderId != request.ProviderId)
    {
               continue; // Skip claims not belonging to provider
        }

     var status = await _statusTracker.GetClaimStatusAsync(claim.Id.ToString());

          results.Add(new ClaimStatusSummary
           {
      ClaimNumber = claimNumber,
       CurrentStatus = status?.CurrentStatus.ToString() ?? claim.Status,
   LastUpdated = status?.LastUpdated ?? claim.UpdatedAt ?? claim.CreatedAt,
            TotalAmount = claim.Total
        });
   }
    catch (Exception ex)
            {
           _logger.LogWarning(ex, "Error retrieving status for claim {ClaimNumber}", claimNumber);
        // Continue with other claims
     }
            }

        var response = new BatchStatusInquiryResponse
            {
     TotalRequested = request.ClaimNumbers.Count,
                TotalFound = results.Count,
            TotalNotFound = notFound.Count,
          Results = results,
NotFoundClaimNumbers = notFound
     };

       _logger.LogInformation("Batch status inquiry completed: {Found}/{Total} claims found", 
      results.Count, request.ClaimNumbers.Count);

            return Ok(response);
        }
        catch (Exception ex)
    {
       _logger.LogError(ex, "Error during batch status inquiry");
     return StatusCode(500, new { error = "An error occurred during batch status inquiry" });
        }
 }

    /// <summary>
    /// Get detailed status for a specific claim by ID
    /// GET /api/claims/status/{claimId}
    /// </summary>
    /// <param name="claimId">Claim ID</param>
    /// <returns>Detailed claim status</returns>
    [HttpGet("{claimId}")]
    [ProducesResponseType(typeof(ClaimStatusInquiryResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClaimStatusById(string claimId)
  {
     try
        {
   _logger.LogInformation("Getting status for claim ID {ClaimId}", claimId);

            var claim = await _claimService.GetClaimByIdAsync(claimId);
        
            if (claim == null)
     {
return NotFound(new { error = "Claim not found", claimId });
            }

        var status = await _statusTracker.GetClaimStatusAsync(claimId);
   var history = await _statusTracker.GetStatusHistoryAsync(claimId);

    var response = new ClaimStatusInquiryResponse
   {
    ClaimId = claim.Id.ToString(),
ClaimNumber = claim.ClaimNumber,
 CurrentStatus = status?.CurrentStatus.ToString() ?? claim.Status,
  LastUpdated = status?.LastUpdated ?? claim.UpdatedAt ?? claim.CreatedAt,
  DaysInCurrentStatus = status?.DaysInCurrentStatus ?? 
     (int)(DateTime.UtcNow - (claim.UpdatedAt ?? claim.CreatedAt)).TotalDays,
 Description = status?.Description ?? $"Claim status: {claim.Status}",
    PatientId = claim.PatientId,
        ProviderId = claim.ProviderId,
 InsurerId = claim.InsurerId,
    SubmittedDate = claim.CreatedAt,
   TotalAmount = claim.Total,
     StatusHistory = history?.Select(h => new StatusHistoryItem
        {
Status = h.Status.ToString(),
  StatusDate = h.StatusDate,
        Reason = h.Reason
   }).ToList() ?? new List<StatusHistoryItem>()
     };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting status for claim ID {ClaimId}", claimId);
            return StatusCode(500, new { error = "An error occurred while retrieving claim status" });
        }
    }

    /// <summary>
    /// Get status statistics
    /// GET /api/claims/status/statistics
    /// </summary>
    /// <returns>Overall status statistics</returns>
    [HttpGet("statistics")]
    [ProducesResponseType(typeof(ClaimStatusStatistics), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetStatusStatistics()
    {
        try
        {
            _logger.LogInformation("Retrieving claim status statistics");

            var stats = await _statusTracker.GetStatusStatisticsAsync();

     return Ok(stats);
        }
        catch (Exception ex)
        {
      _logger.LogError(ex, "Error retrieving status statistics");
            return StatusCode(500, new { error = "An error occurred while retrieving statistics" });
        }
    }

    /// <summary>
    /// Get status history for a claim
    /// GET /api/claims/status/{claimId}/history
  /// </summary>
    /// <param name="claimId">Claim ID</param>
    /// <returns>Status change history</returns>
    [HttpGet("{claimId}/history")]
    [ProducesResponseType(typeof(List<StatusHistoryItem>), StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetStatusHistory(string claimId)
    {
    try
        {
         _logger.LogInformation("Getting status history for claim {ClaimId}", claimId);

   var claim = await _claimService.GetClaimByIdAsync(claimId);
            
     if (claim == null)
  {
  return NotFound(new { error = "Claim not found", claimId });
  }

var history = await _statusTracker.GetStatusHistoryAsync(claimId);

     var historyItems = history?.Select(h => new StatusHistoryItem
            {
        Status = h.Status.ToString(),
     StatusDate = h.StatusDate,
 Reason = h.Reason
            }).OrderByDescending(h => h.StatusDate).ToList() ?? new List<StatusHistoryItem>();

            return Ok(historyItems);
      }
        catch (Exception ex)
   {
            _logger.LogError(ex, "Error getting status history for claim {ClaimId}", claimId);
         return StatusCode(500, new { error = "An error occurred while retrieving status history" });
        }
    }
}

#region DTOs

/// <summary>
/// Claim status inquiry request
/// </summary>
public class ClaimStatusInquiryRequest
{
    public string ClaimNumber { get; set; }
    public string? ProviderId { get; set; }
}

/// <summary>
/// Claim status inquiry response
/// </summary>
public class ClaimStatusInquiryResponse
{
    public string ClaimId { get; set; }
    public string ClaimNumber { get; set; }
    public string CurrentStatus { get; set; }
    public DateTime LastUpdated { get; set; }
    public int DaysInCurrentStatus { get; set; }
    public string Description { get; set; }
 public string PatientId { get; set; }
    public string ProviderId { get; set; }
    public string InsurerId { get; set; }
    public DateTime SubmittedDate { get; set; }
    public decimal? TotalAmount { get; set; }
 public List<StatusHistoryItem> StatusHistory { get; set; } = new();
}

/// <summary>
/// Batch status inquiry request
/// </summary>
public class BatchStatusInquiryRequest
{
 public List<string> ClaimNumbers { get; set; } = new();
    public string? ProviderId { get; set; }
}

/// <summary>
/// Batch status inquiry response
/// </summary>
public class BatchStatusInquiryResponse
{
    public int TotalRequested { get; set; }
    public int TotalFound { get; set; }
    public int TotalNotFound { get; set; }
    public List<ClaimStatusSummary> Results { get; set; } = new();
    public List<string> NotFoundClaimNumbers { get; set; } = new();
}

/// <summary>
/// Claim status summary (for batch operations)
/// </summary>
public class ClaimStatusSummary
{
    public string ClaimNumber { get; set; }
    public string CurrentStatus { get; set; }
    public DateTime LastUpdated { get; set; }
    public decimal? TotalAmount { get; set; }
}

/// <summary>
/// Status history item
/// </summary>
public class StatusHistoryItem
{
    public string Status { get; set; }
    public DateTime StatusDate { get; set; }
    public string Reason { get; set; }
}

#endregion
