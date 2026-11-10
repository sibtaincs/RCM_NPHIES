using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.Services;
using NPhies_FHIR_Integration.Common.Constants;
using NPhies_FHIR_Integration.Common.Models;
using System.ComponentModel.DataAnnotations;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Payments Controller
/// Handles payment calculation and payment summary operations for claims
/// </summary>
[ApiController]
[Route(AppConstants.ApiRoutePrefix + "/[controller]")]
public class PaymentsController : BaseController
{
  private readonly IPaymentService _paymentService;
    private readonly ILogger<PaymentsController> _logger;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public PaymentsController(IPaymentService paymentService, ILogger<PaymentsController> logger)
    {
        _paymentService = paymentService ?? throw new ArgumentNullException(nameof(paymentService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Calculate payment for a claim
    /// POST /api/v1/payments/calculate
    /// </summary>
    /// <param name="request">Payment calculation request with ClaimId and optional CoverageId</param>
    /// <returns>Payment calculation result with amounts and responsibility breakdown</returns>
 /// <response code="200">Payment calculated successfully</response>
    /// <response code="400">Invalid request or calculation failed</response>
    /// <response code="404">Claim not found</response>
    /// <response code="500">Internal server error</response>
    [HttpPost("calculate")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CalculatePayment([FromBody] dynamic request)
    {
      try
        {
 _logger.LogInformation("Received payment calculation request");

        // Validate request
        if (request == null)
 {
_logger.LogWarning("Payment calculation request is null");
      return BadRequest("Payment calculation request is required");
    }

         // Extract ClaimId
      int claimId = request.claimId;

      if (claimId <= 0)
         {
              _logger.LogWarning("Invalid claim ID: {ClaimId}", claimId);
       return BadRequest("Valid claim ID is required");
   }

        // Create payment calculation request
            var paymentRequest = new PaymentCalculationRequest
     {
ClaimId = claimId,
       CoverageId = request.coverageId,
        ForceRecalculation = request.forceRecalculation ?? false
       };

            // Calculate payment
            var result = await _paymentService.CalculatePaymentAsync(paymentRequest);

       if (!result.IsSuccessful)
   {
  _logger.LogWarning("Payment calculation failed for claim {ClaimId}: {Error}",
          claimId, result.ErrorMessage);
       return BadRequest(result.ErrorMessage, result.ValidationErrors);
       }

 _logger.LogInformation("Payment calculated successfully for claim {ClaimId}", claimId);
     return Ok(result, "Payment calculated successfully");
   }
        catch (Exception ex)
        {
   _logger.LogError(ex, "Unexpected error calculating payment");
            return InternalServerError($"Error calculating payment: {ex.Message}");
        }
    }

    /// <summary>
    /// Get payment summary for a claim
    /// GET /api/v1/payments/{id}/summary
    /// </summary>
    /// <param name="id">Claim ID</param>
    /// <returns>Payment summary with totals and breakdown</returns>
    /// <response code="200">Payment summary retrieved successfully</response>
    /// <response code="404">Claim not found or no payment calculated</response>
    /// <response code="500">Internal server error</response>
 [HttpGet("{id}/summary")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentSummary([FromRoute] [Range(1, int.MaxValue)] int id)
    {
        try
        {
         _logger.LogInformation("Retrieving payment summary for claim {ClaimId}", id);

          var summary = await _paymentService.GetPaymentSummaryAsync(id);

            if (summary == null)
            {
     _logger.LogWarning("Payment summary not found for claim {ClaimId}", id);
        return NotFound($"Payment summary not found for claim {id}");
    }

        _logger.LogInformation("Payment summary retrieved successfully for claim {ClaimId}", id);
   return Ok(summary, "Payment summary retrieved successfully");
        }
    catch (Exception ex)
      {
 _logger.LogError(ex, "Error retrieving payment summary for claim {ClaimId}", id);
   return InternalServerError($"Error retrieving payment summary: {ex.Message}");
        }
    }

 /// <summary>
    /// Get detailed payment information for a claim
 /// GET /api/v1/payments/{id}/details
    /// </summary>
    /// <param name="id">Claim ID</param>
    /// <returns>Detailed payment information with item-level breakdown</returns>
    /// <response code="200">Payment details retrieved successfully</response>
    /// <response code="404">Claim not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}/details")]
    [ProducesResponseType(StatusCodes.Status200OK)]
  [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPaymentDetails([FromRoute] [Range(1, int.MaxValue)] int id)
    {
      try
        {
  _logger.LogInformation("Retrieving payment details for claim {ClaimId}", id);

         var details = await _paymentService.GetPaymentDetailsAsync(id);

            if (details == null)
      {
  _logger.LogWarning("Payment details not found for claim {ClaimId}", id);
                return NotFound($"Payment details not found for claim {id}");
       }

  _logger.LogInformation("Payment details retrieved successfully for claim {ClaimId}", id);
            return Ok(details, "Payment details retrieved successfully");
        }
        catch (Exception ex)
        {
  _logger.LogError(ex, "Error retrieving payment details for claim {ClaimId}", id);
       return InternalServerError($"Error retrieving payment details: {ex.Message}");
    }
    }
}
