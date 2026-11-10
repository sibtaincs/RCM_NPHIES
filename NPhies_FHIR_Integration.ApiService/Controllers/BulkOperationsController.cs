using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.Services;
using NPhies_FHIR_Integration.Application.Services.Batch;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Domain.DTOs;
using NPhies_FHIR_Integration.Application.Rcm.Services;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Bulk Operations Controller - NPHIES Batch Processing
/// Handles bulk eligibility checks, claim submissions, and status inquiries
/// </summary>
[ApiController]
[Route("api/bulk")]
[Authorize]
[Produces("application/json")]
public class BulkOperationsController : BaseController
{
    private readonly IEligibilityService _eligibilityService;
    private readonly IClaimService _claimService;
    private readonly IRcmClaimWorkflowService _rcmClaims;
 private readonly ILogger<BulkOperationsController> _logger;
    private const int MaxBatchSize = 1000;

    public BulkOperationsController(
        IEligibilityService eligibilityService,
   IClaimService claimService,
        IRcmClaimWorkflowService rcmClaims,
        ILogger<BulkOperationsController> logger)
    {
        _eligibilityService = eligibilityService ?? throw new ArgumentNullException(nameof(eligibilityService));
        _claimService = claimService ?? throw new ArgumentNullException(nameof(claimService));
        _rcmClaims = rcmClaims ?? throw new ArgumentNullException(nameof(rcmClaims));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Bulk eligibility check for multiple patients
    /// POST /api/bulk/eligibility
    /// </summary>
 /// <param name="request">Bulk eligibility request with multiple patient records</param>
    /// <returns>Batch results with individual eligibility responses</returns>
    [HttpPost("eligibility")]
    [ProducesResponseType(typeof(BulkEligibilityResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
  public async Task<IActionResult> BulkEligibilityCheck([FromBody] BulkEligibilityRequest request)
    {
        try
        {
  _logger.LogInformation("Processing bulk eligibility check for {Count} requests", 
                request.Requests?.Count ?? 0);

            // Validate request
      if (request.Requests == null || !request.Requests.Any())
      return BadRequest("At least one eligibility request is required");

   if (request.Requests.Count > MaxBatchSize)
        return BadRequest($"Maximum {MaxBatchSize} requests allowed per batch");

    var batchId = Guid.NewGuid().ToString();
         var startTime = DateTime.UtcNow;
            var results = new List<BulkEligibilityResult>();

    // Process each request
        foreach (var eligibilityRequest in request.Requests)
    {
          try
   {
        var eligibilityResponse = await _eligibilityService.CheckEligibilityAsync(eligibilityRequest);
      
  results.Add(new BulkEligibilityResult
      {
    RequestId = eligibilityRequest.RequestId,
   PatientId = eligibilityRequest.PatientId,
     IsSuccess = true,
         IsEligible = eligibilityResponse?.EligibilityStatus == "active",
 EligibilityStatus = eligibilityResponse?.EligibilityStatus,
       CoverageActive = true, // Default to true for successful responses
   Message = "Eligibility check completed successfully"
          });
     }
     catch (Exception ex)
      {
      _logger.LogError(ex, "Error processing eligibility for patient {PatientId}", 
  eligibilityRequest.PatientId);
       
     results.Add(new BulkEligibilityResult
   {
  RequestId = eligibilityRequest.RequestId,
   PatientId = eligibilityRequest.PatientId,
      IsSuccess = false,
 ErrorMessage = ex.Message
    });
      }
    }

            var response = new BulkEligibilityResponse
        {
      BatchId = batchId,
            TotalRequests = request.Requests.Count,
SuccessCount = results.Count(r => r.IsSuccess),
          FailureCount = results.Count(r => !r.IsSuccess),
         EligibleCount = results.Count(r => r.IsEligible),
       Results = results,
  ProcessingTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
ProcessedAt = DateTime.UtcNow
   };

          _logger.LogInformation("Bulk eligibility check completed: {Success}/{Total} successful", 
                response.SuccessCount, response.TotalRequests);

            return Ok(response);
        }
   catch (Exception ex)
        {
     _logger.LogError(ex, "Error processing bulk eligibility check");
            return StatusCode(500, new 
         { 
       error = "An error occurred while processing bulk eligibility check", 
             details = ex.Message 
            });
        }
    }

  /// <summary>
    /// Bulk claim submission
    /// POST /api/bulk/claims
    /// </summary>
    /// <param name="request">Bulk claim submission request</param>
    /// <returns>Batch results with individual claim submission responses</returns>
    [HttpPost("claims")]
    [ProducesResponseType(typeof(BulkClaimResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkClaimSubmission([FromBody] BulkClaimRequest request)
    {
try
        {
    _logger.LogInformation("Processing bulk claim submission for {Count} claims", 
  request.Claims?.Count ?? 0);

  // Validate request
            if (request.Claims == null || !request.Claims.Any())
           return BadRequest("At least one claim is required");

         if (request.Claims.Count > MaxBatchSize)
   return BadRequest($"Maximum {MaxBatchSize} claims allowed per batch");

            var batchId = Guid.NewGuid().ToString();
            var startTime = DateTime.UtcNow;
        var results = new List<BulkClaimResult>();

            // Process each claim
   foreach (var claimDto in request.Claims)
            {
        try
          {
       var createdClaim = await _claimService.CreateClaimAsync(claimDto);
   
                  results.Add(new BulkClaimResult
              {
              ClaimId = createdClaim.Id.ToString(),
           ClaimNumber = createdClaim.ClaimNumber,
          PatientId = claimDto.PatientId,
       IsSuccess = true,
  TotalAmount = claimDto.Total,
   Message = "Claim submitted successfully"
            });
     }
           catch (Exception ex)
             {
         _logger.LogError(ex, "Error processing claim for patient {PatientId}", 
   claimDto.PatientId);
  
 results.Add(new BulkClaimResult
              {
       PatientId = claimDto.PatientId,
               IsSuccess = false,
    ErrorMessage = ex.Message
    });
     }
 }

            var response = new BulkClaimResponse
    {
     BatchId = batchId,
              TotalClaims = request.Claims.Count,
                SubmittedCount = results.Count(r => r.IsSuccess),
              RejectedCount = results.Count(r => !r.IsSuccess),
      TotalAmount = results.Where(r => r.IsSuccess).Sum(r => r.TotalAmount ?? 0),
       Results = results,
                ProcessingTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
                ProcessedAt = DateTime.UtcNow
  };

       _logger.LogInformation("Bulk claim submission completed: {Submitted}/{Total} successful", 
          response.SubmittedCount, response.TotalClaims);

            return Ok(response);
        }
     catch (Exception ex)
    {
            _logger.LogError(ex, "Error processing bulk claim submission");
         return StatusCode(500, new 
            { 
       error = "An error occurred while processing bulk claim submission", 
       details = ex.Message 
          });
}
    }

    /// <summary>
    /// Bulk status inquiry for multiple claims
    /// POST /api/bulk/status
 /// </summary>
    /// <param name="request">Bulk status inquiry request with claim numbers</param>
    /// <returns>Status for all requested claims</returns>
    [HttpPost("status")]
    [ProducesResponseType(typeof(BulkStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> BulkStatusInquiry([FromBody] BulkStatusRequest request)
    {
        try
        {
            _logger.LogInformation("Processing bulk status inquiry for {Count} claims", 
                request.ClaimNumbers?.Count ?? 0);

  // Validate request
     if (request.ClaimNumbers == null || !request.ClaimNumbers.Any())
          return BadRequest("At least one claim number is required");

    if (request.ClaimNumbers.Count > MaxBatchSize)
            return BadRequest($"Maximum {MaxBatchSize} claim numbers allowed per batch");

      var batchId = Guid.NewGuid().ToString();
      var startTime = DateTime.UtcNow;
            var results = new List<BulkStatusResult>();
            var allClaims = await _claimService.GetAllClaimsAsync();

  foreach (var claimNumber in request.ClaimNumbers)
            {
  try
    {
        var claim = allClaims.FirstOrDefault(c => c.ClaimNumber == claimNumber);

      if (claim == null)
        {
          results.Add(new BulkStatusResult
    {
  ClaimNumber = claimNumber,
IsSuccess = false,
      ErrorMessage = "Claim not found"
          });
     continue;
      }

         results.Add(new BulkStatusResult
           {
        ClaimNumber = claimNumber,
     ClaimId = claim.Id.ToString(),
    CurrentStatus = claim.Status,
   LastUpdated = claim.UpdatedAt ?? claim.CreatedAt,
      PatientId = claim.PatientId,
   TotalAmount = claim.Total,
       IsSuccess = true
    });
        }
     catch (Exception ex)
      {
  _logger.LogError(ex, "Error retrieving status for claim {ClaimNumber}", claimNumber);
  
          results.Add(new BulkStatusResult
 {
   ClaimNumber = claimNumber,
     IsSuccess = false,
        ErrorMessage = ex.Message
       });
        }
       }

        var response = new BulkStatusResponse
 {
   BatchId = batchId,
                TotalRequests = request.ClaimNumbers.Count,
  SuccessCount = results.Count(r => r.IsSuccess),
         NotFoundCount = results.Count(r => !r.IsSuccess),
 Results = results,
   ProcessingTimeMs = (DateTime.UtcNow - startTime).TotalMilliseconds,
          ProcessedAt = DateTime.UtcNow
       };

            _logger.LogInformation("Bulk status inquiry completed: {Success}/{Total} successful", 
 response.SuccessCount, response.TotalRequests);

return Ok(response);
 }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing bulk status inquiry");
            return StatusCode(500, new 
   { 
            error = "An error occurred while processing bulk status inquiry", 
    details = ex.Message 
  });
        }
    }

    /// <summary>
    /// Get batch processing status
 /// GET /api/bulk/batch/{batchId}
 /// </summary>
    /// <param name="batchId">Batch ID to check</param>
    /// <returns>Batch processing status</returns>
    [HttpGet("batch/{batchId}")]
    [ProducesResponseType(typeof(BatchStatusInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
  public async Task<IActionResult> GetBatchStatus(string batchId)
    {
        try
        {
 _logger.LogInformation("Retrieving batch status for {BatchId}", batchId);

            var batch = await _rcmClaims.GetBatchAsync(batchId, HttpContext.RequestAborted);
            if (batch == null)
                return NotFound($"Batch '{batchId}' was not found");

            var decided = batch.Claims.Count(c => c.Outcome is not null && c.Outcome != "queued");
            var failed = batch.Claims.Count(c => c.ClaimStatus is "Rejected" or "Error" or "ValidationFailed");
            var status = new BatchStatusInfo
            {
                BatchId = batch.BatchIdentifier,
                Status = batch.TransactionStatus,
                TotalItems = batch.ClaimCount,
                ProcessedItems = decided,
                SuccessItems = decided - failed,
                FailedItems = failed,
                StartedAt = batch.SubmittedAt ?? DateTime.UtcNow,
                CompletedAt = batch.CompletedAt,
                ProcessingTimeMs = batch.SubmittedAt.HasValue && batch.CompletedAt.HasValue
                    ? (batch.CompletedAt.Value - batch.SubmittedAt.Value).TotalMilliseconds
                    : 0
            };

            return Ok(status);
        }
        catch (Exception ex)
      {
   _logger.LogError(ex, "Error retrieving batch status for {BatchId}", batchId);
        return StatusCode(500, new 
 { 
         error = "An error occurred while retrieving batch status", 
     details = ex.Message 
            });
        }
    }
}

#region DTOs

// Bulk Eligibility DTOs
public class BulkEligibilityRequest
{
    public List<CoverageEligibilityRequestDto> Requests { get; set; } = new();
}

public class BulkEligibilityResponse
{
    public string BatchId { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int SuccessCount { get; set; }
    public int FailureCount { get; set; }
    public int EligibleCount { get; set; }
    public List<BulkEligibilityResult> Results { get; set; } = new();
    public double ProcessingTimeMs { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public class BulkEligibilityResult
{
    public string RequestId { get; set; } = string.Empty;
    public string PatientId { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public bool IsEligible { get; set; }
    public string? EligibilityStatus { get; set; }
    public bool CoverageActive { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

// Bulk Claim DTOs
public class BulkClaimRequest
{
    public List<CreateClaimDto> Claims { get; set; } = new();
}

public class BulkClaimResponse
{
    public string BatchId { get; set; } = string.Empty;
    public int TotalClaims { get; set; }
    public int SubmittedCount { get; set; }
    public int RejectedCount { get; set; }
    public decimal TotalAmount { get; set; }
    public List<BulkClaimResult> Results { get; set; } = new();
    public double ProcessingTimeMs { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public class BulkClaimResult
{
    public string? ClaimId { get; set; }
    public string? ClaimNumber { get; set; }
public string PatientId { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
public decimal? TotalAmount { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

// Bulk Status DTOs
public class BulkStatusRequest
{
    public List<string> ClaimNumbers { get; set; } = new();
}

public class BulkStatusResponse
{
    public string BatchId { get; set; } = string.Empty;
    public int TotalRequests { get; set; }
    public int SuccessCount { get; set; }
    public int NotFoundCount { get; set; }
    public List<BulkStatusResult> Results { get; set; } = new();
    public double ProcessingTimeMs { get; set; }
    public DateTime ProcessedAt { get; set; }
}

public class BulkStatusResult
{
    public string ClaimNumber { get; set; } = string.Empty;
    public string? ClaimId { get; set; }
    public string? CurrentStatus { get; set; }
    public DateTime? LastUpdated { get; set; }
    public string? PatientId { get; set; }
    public decimal? TotalAmount { get; set; }
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
}

// Batch Status Info
public class BatchStatusInfo
{
    public string BatchId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalItems { get; set; }
    public int ProcessedItems { get; set; }
    public int SuccessItems { get; set; }
    public int FailedItems { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public double ProcessingTimeMs { get; set; }
}

#endregion
