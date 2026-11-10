using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Repositories;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Coverage API Controller
/// Manages patient insurance coverage operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class CoverageController : ControllerBase
{
    private readonly ICoverageRepository _coverageRepository;
    private readonly IPatientRepository _patientRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<CoverageController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public CoverageController(
        ICoverageRepository coverageRepository,
        IPatientRepository patientRepository,
 IMapper mapper,
        ILogger<CoverageController> logger)
    {
        _coverageRepository = coverageRepository ?? throw new ArgumentNullException(nameof(coverageRepository));
        _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all active coverages with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>Paginated list of coverages</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<CoverageDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<CoverageDto>>>> GetAll(
     [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var paginationParams = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };
            if (!paginationParams.Validate(out var validationError))
            {
                _logger.LogWarning($"Pagination validation failed: {validationError}");
                return BadRequest(ApiResponse<PaginatedResponse<CoverageDto>>.ErrorResponse(validationError, 400));
            }

            var allCoverages = await _coverageRepository.GetAllAsync();
            var totalCount = allCoverages.Count();
            var paginatedCoverages = allCoverages
           .Skip((pageNumber - 1) * pageSize)
               .Take(pageSize)
               .ToList();

            var coverageDtos = _mapper.Map<List<CoverageDto>>(paginatedCoverages);
            var paginatedResult = PaginatedResponse<CoverageDto>.CreatePaginatedResponse(coverageDtos, pageNumber, pageSize, totalCount);

            _logger.LogInformation($"Retrieved {coverageDtos.Count} coverages (Page {pageNumber})");
            return Ok(ApiResponse<PaginatedResponse<CoverageDto>>.SuccessResponse(paginatedResult, "Coverages retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving coverages");
            return StatusCode(500, ApiResponse<PaginatedResponse<CoverageDto>>.ErrorResponse("An error occurred while retrieving coverages", 500));
        }
    }

    /// <summary>
    /// Get coverage by ID
    /// </summary>
    /// <param name="id">Coverage ID</param>
    /// <returns>Coverage details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CoverageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CoverageDto>>> GetById(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(ApiResponse<CoverageDto>.ErrorResponse("Coverage ID is required", 400));
            }

            var coverage = await _coverageRepository.GetByIdAsync(id);
            if (coverage == null)
            {
                _logger.LogWarning($"Coverage not found: {id}");
                return NotFound(ApiResponse<CoverageDto>.ErrorResponse("Coverage not found", 404));
            }

            var coverageDto = _mapper.Map<CoverageDto>(coverage);
            _logger.LogInformation($"Retrieved coverage: {id}");
            return Ok(ApiResponse<CoverageDto>.SuccessResponse(coverageDto, "Coverage retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving coverage: {id}");
            return StatusCode(500, ApiResponse<CoverageDto>.ErrorResponse("An error occurred while retrieving the coverage", 500));
        }
    }

    /// <summary>
    /// Get coverage by policy number
    /// </summary>
    /// <param name="policyNumber">Policy number</param>
    /// <returns>Coverage details</returns>
    [HttpGet("policy/{policyNumber}")]
    [ProducesResponseType(typeof(ApiResponse<CoverageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CoverageDto>>> GetByPolicyNumber(string policyNumber)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(policyNumber))
            {
                return BadRequest(ApiResponse<CoverageDto>.ErrorResponse("Policy number is required", 400));
            }

            var coverage = await _coverageRepository.GetByPolicyNumberAsync(policyNumber);
            if (coverage == null)
            {
                _logger.LogWarning($"Coverage not found with policy number: {policyNumber}");
                return NotFound(ApiResponse<CoverageDto>.ErrorResponse("Coverage not found", 404));
            }

            var coverageDto = _mapper.Map<CoverageDto>(coverage);
            _logger.LogInformation($"Retrieved coverage by policy: {policyNumber}");
            return Ok(ApiResponse<CoverageDto>.SuccessResponse(coverageDto, "Coverage retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving coverage by policy: {policyNumber}");
            return StatusCode(500, ApiResponse<CoverageDto>.ErrorResponse("An error occurred while retrieving the coverage", 500));
        }
    }

    /// <summary>
    /// Get all active coverages for a patient
    /// </summary>
    /// <param name="patientId">Patient ID</param>
    /// <returns>List of patient's active coverages</returns>
    [HttpGet("patient/{patientId}")]
    [ProducesResponseType(typeof(ApiResponse<List<CoverageDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<CoverageDto>>>> GetByPatient(string patientId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(patientId))
            {
                return BadRequest(ApiResponse<List<CoverageDto>>.ErrorResponse("Patient ID is required", 400));
            }

            var patient = await _patientRepository.GetByIdAsync(patientId);
            if (patient == null)
            {
                return NotFound(ApiResponse<List<CoverageDto>>.ErrorResponse("Patient not found", 404));
            }

            var coverages = await _coverageRepository.GetActiveByPatientIdAsync(patientId);
            var coverageDtos = _mapper.Map<List<CoverageDto>>(coverages);

            _logger.LogInformation($"Retrieved {coverageDtos.Count} coverages for patient: {patientId}");
            return Ok(ApiResponse<List<CoverageDto>>.SuccessResponse(coverageDtos, "Coverages retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving coverages for patient: {patientId}");
            return StatusCode(500, ApiResponse<List<CoverageDto>>.ErrorResponse("An error occurred while retrieving coverages", 500));
        }
    }

    /// <summary>
    /// Get coverages expiring soon
    /// </summary>
    /// <param name="daysFromNow">Number of days to check (default: 30)</param>
    /// <returns>List of expiring coverages</returns>
    [HttpGet("expiring")]
    [ProducesResponseType(typeof(ApiResponse<List<ExpiringCoverageDto>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<ExpiringCoverageDto>>>> GetExpiring(
           [FromQuery] int daysFromNow = 30)
    {
        try
        {
            var expiryDate = DateTime.UtcNow.AddDays(daysFromNow);
            var allCoverages = await _coverageRepository.GetAllAsync();
            var expiringCoverages = allCoverages.Where(c =>
                    c.Status == "active" &&
              c.CoverageEndDate <= expiryDate &&
                          c.CoverageEndDate > DateTime.UtcNow).ToList();

            var expiringDtos = new List<ExpiringCoverageDto>();

            foreach (var coverage in expiringCoverages)
            {
                var patient = await _patientRepository.GetByIdAsync(coverage.PatientId);
                expiringDtos.Add(new ExpiringCoverageDto
                {
                    Id = coverage.Id,
                    PolicyNumber = coverage.PolicyNumber,
                    PatientName = patient != null ? $"{patient.FirstName} {patient.LastName}" : "Unknown",
                    CoverageEndDate = coverage.CoverageEndDate
                });
            }

            _logger.LogInformation($"Retrieved {expiringDtos.Count} expiring coverages (within {daysFromNow} days)");
            return Ok(ApiResponse<List<ExpiringCoverageDto>>.SuccessResponse(expiringDtos, "Expiring coverages retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving expiring coverages");
            return StatusCode(500, ApiResponse<List<ExpiringCoverageDto>>.ErrorResponse("An error occurred while retrieving expiring coverages", 500));
        }
    }

    /// <summary>
    /// Create new coverage
    /// </summary>
    /// <param name="createCoverageDto">Coverage creation data</param>
    /// <returns>Created coverage</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CoverageDto>), StatusCodes.Status201Created)]
    public async Task<ActionResult<ApiResponse<CoverageDto>>> Create([FromBody] CreateCoverageDto createCoverageDto)
    {
        try
        {
            if (createCoverageDto == null)
            {
                return BadRequest(ApiResponse<CoverageDto>.ErrorResponse("Coverage data is required", 400));
            }

            // Verify patient exists
            var patient = await _patientRepository.GetByIdAsync(createCoverageDto.PatientId);
            if (patient == null)
            {
                return BadRequest(ApiResponse<CoverageDto>.ErrorResponse("Patient not found", 400));
            }

            var coverage = _mapper.Map<Coverage>(createCoverageDto);
            await _coverageRepository.AddAsync(coverage);
            await _coverageRepository.SaveChangesAsync();

            var coverageDto = _mapper.Map<CoverageDto>(coverage);
            _logger.LogInformation($"Created new coverage: {coverage.Id}");

            return CreatedAtAction(nameof(GetById), new { id = coverage.Id },
                  ApiResponse<CoverageDto>.SuccessResponse(coverageDto, "Coverage created successfully", 201));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating coverage");
            return StatusCode(500, ApiResponse<CoverageDto>.ErrorResponse("An error occurred while creating the coverage", 500));
        }
    }

    /// <summary>
    /// Update existing coverage
    /// </summary>
    /// <param name="id">Coverage ID</param>
    /// <param name="updateCoverageDto">Updated coverage data</param>
    /// <returns>Updated coverage</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<CoverageDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<CoverageDto>>> Update(string id, [FromBody] UpdateCoverageDto updateCoverageDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(ApiResponse<CoverageDto>.ErrorResponse("Coverage ID is required", 400));
            }

            var coverage = await _coverageRepository.GetByIdAsync(id);
            if (coverage == null)
            {
                return NotFound(ApiResponse<CoverageDto>.ErrorResponse("Coverage not found", 404));
            }

            _mapper.Map(updateCoverageDto, coverage);
            _coverageRepository.Update(coverage);
            await _coverageRepository.SaveChangesAsync();

            var coverageDto = _mapper.Map<CoverageDto>(coverage);
            _logger.LogInformation($"Updated coverage: {id}");

            return Ok(ApiResponse<CoverageDto>.SuccessResponse(coverageDto, "Coverage updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating coverage: {id}");
            return StatusCode(500, ApiResponse<CoverageDto>.ErrorResponse("An error occurred while updating the coverage", 500));
        }
    }

    /// <summary>
    /// Delete coverage
    /// </summary>
    /// <param name="id">Coverage ID</param>
    /// <returns>Deletion status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse>> Delete(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(ApiResponse.ErrorResponse("Coverage ID is required", 400));
            }

            var coverage = await _coverageRepository.GetByIdAsync(id);
            if (coverage == null)
            {
                return NotFound(ApiResponse.ErrorResponse("Coverage not found", 404));
            }

            _coverageRepository.Delete(coverage);
            await _coverageRepository.SaveChangesAsync();

            _logger.LogInformation($"Deleted coverage: {id}");
            return Ok(ApiResponse.SuccessResponse("Coverage deleted successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting coverage: {id}");
            return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while deleting the coverage", 500));
        }
    }
}
