using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Repositories;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Patients API Controller
/// Manages patient-related operations
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class PatientsController : ControllerBase
{
    private readonly IPatientRepository _patientRepository;
    private readonly IMapper _mapper;
    private readonly ILogger<PatientsController> _logger;

    /// <summary>
    /// Constructor
    /// </summary>
    public PatientsController(IPatientRepository patientRepository, IMapper mapper, ILogger<PatientsController> logger)
    {
        _patientRepository = patientRepository ?? throw new ArgumentNullException(nameof(patientRepository));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Get all patients with pagination
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 10, max: 100)</param>
    /// <returns>Paginated list of patients</returns>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PaginatedResponse<PatientDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PaginatedResponse<PatientDto>>>> GetAll(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        try
        {
            var paginationParams = new PaginationParams { PageNumber = pageNumber, PageSize = pageSize };
            if (!paginationParams.Validate(out var validationError))
            {
                _logger.LogWarning($"Pagination validation failed: {validationError}");
                return BadRequest(ApiResponse<PaginatedResponse<PatientDto>>.ErrorResponse(validationError, 400));
            }

            var allPatients = await _patientRepository.FindAsync(p => p.IsActive);
            var totalCount = allPatients.Count();
            var patients = allPatients
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var patientDtos = _mapper.Map<List<PatientDto>>(patients);
            var paginatedResult = PaginatedResponse<PatientDto>.CreatePaginatedResponse(patientDtos, pageNumber, pageSize, totalCount);

            _logger.LogInformation($"Retrieved {patientDtos.Count} patients (Page {pageNumber})");
            return Ok(ApiResponse<PaginatedResponse<PatientDto>>.SuccessResponse(paginatedResult, "Patients retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving patients");
            return StatusCode(500, ApiResponse<PaginatedResponse<PatientDto>>.ErrorResponse("An error occurred while retrieving patients", 500));
        }
    }

    /// <summary>
    /// Get patient by ID
    /// </summary>
    /// <param name="id">Patient ID</param>
    /// <returns>Patient details</returns>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PatientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PatientDto>>> GetById(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(ApiResponse<PatientDto>.ErrorResponse("Patient ID is required", 400));
            }

            var patient = await _patientRepository.GetByIdAsync(id);
            if (patient == null)
            {
                _logger.LogWarning($"Patient not found: {id}");
                return NotFound(ApiResponse<PatientDto>.ErrorResponse("Patient not found", 404));
            }

            var patientDto = _mapper.Map<PatientDto>(patient);
            _logger.LogInformation($"Retrieved patient: {id}");
            return Ok(ApiResponse<PatientDto>.SuccessResponse(patientDto, "Patient retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving patient: {id}");
            return StatusCode(500, ApiResponse<PatientDto>.ErrorResponse("An error occurred while retrieving the patient", 500));
        }
    }

    /// <summary>
    /// Get patient by MRN
    /// </summary>
    /// <param name="mrn">Medical Record Number</param>
    /// <returns>Patient details</returns>
    [HttpGet("mrn/{mrn}")]
    [ProducesResponseType(typeof(ApiResponse<PatientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PatientDto>>> GetByMRN(string mrn)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(mrn))
            {
                return BadRequest(ApiResponse<PatientDto>.ErrorResponse("MRN is required", 400));
            }

            var patient = await _patientRepository.GetByMRNAsync(mrn);
            if (patient == null)
            {
                _logger.LogWarning($"Patient not found with MRN: {mrn}");
                return NotFound(ApiResponse<PatientDto>.ErrorResponse("Patient not found", 404));
            }

            var patientDto = _mapper.Map<PatientDto>(patient);
            _logger.LogInformation($"Retrieved patient by MRN: {mrn}");
            return Ok(ApiResponse<PatientDto>.SuccessResponse(patientDto, "Patient retrieved successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error retrieving patient by MRN: {mrn}");
            return StatusCode(500, ApiResponse<PatientDto>.ErrorResponse("An error occurred while retrieving the patient", 500));
        }
    }

    /// <summary>
    /// Search patients by name
    /// </summary>
    /// <param name="firstName">First name (partial match)</param>
    /// <param name="lastName">Last name (partial match)</param>
    /// <returns>List of matching patients</returns>
    [HttpGet("search")]
    [ProducesResponseType(typeof(ApiResponse<List<PatientDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<List<PatientDto>>>> Search(
        [FromQuery] string firstName = "",
        [FromQuery] string lastName = "")
    {
        try
        {
            if (string.IsNullOrWhiteSpace(firstName) && string.IsNullOrWhiteSpace(lastName))
            {
                return BadRequest(ApiResponse<List<PatientDto>>.ErrorResponse("At least one search parameter (firstName or lastName) is required", 400));
            }

            var patients = await _patientRepository.FindAsync(p => 
      (string.IsNullOrEmpty(firstName) || p.FirstName.Contains(firstName)) &&
        (string.IsNullOrEmpty(lastName) || p.LastName.Contains(lastName)) &&
  p.IsActive);

            var patientDtos = _mapper.Map<List<PatientDto>>(patients);

       _logger.LogInformation($"Found {patientDtos.Count} patients matching: {firstName} {lastName}");
  return Ok(ApiResponse<List<PatientDto>>.SuccessResponse(patientDtos, "Patients found successfully"));
        }
     catch (Exception ex)
        {
       _logger.LogError(ex, "Error searching patients");
        return StatusCode(500, ApiResponse<List<PatientDto>>.ErrorResponse("An error occurred while searching patients", 500));
        }
    }

    /// <summary>
    /// Create new patient
    /// </summary>
    /// <param name="createPatientDto">Patient creation data</param>
    /// <returns>Created patient</returns>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<PatientDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<PatientDto>>> Create([FromBody] CreatePatientDto createPatientDto)
    {
        try
        {
            if (createPatientDto == null)
            {
                return BadRequest(ApiResponse<PatientDto>.ErrorResponse("Patient data is required", 400));
            }

            // Check if MRN already exists
            if (await _patientRepository.ExistsByMRNAsync(createPatientDto.MRN))
            {
                _logger.LogWarning($"Patient with MRN already exists: {createPatientDto.MRN}");
                return BadRequest(ApiResponse<PatientDto>.ErrorResponse("A patient with this MRN already exists", 400));
            }

            var patient = _mapper.Map<Patient>(createPatientDto);
            await _patientRepository.AddAsync(patient);
            await _patientRepository.SaveChangesAsync();

            var patientDto = _mapper.Map<PatientDto>(patient);
            _logger.LogInformation($"Created new patient: {patient.Id}");

            return CreatedAtAction(nameof(GetById), new { id = patient.Id }, ApiResponse<PatientDto>.SuccessResponse(patientDto, "Patient created successfully", 201));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating patient");
            return StatusCode(500, ApiResponse<PatientDto>.ErrorResponse("An error occurred while creating the patient", 500));
        }
    }

    /// <summary>
    /// Update existing patient
    /// </summary>
    /// <param name="id">Patient ID</param>
    /// <param name="updatePatientDto">Updated patient data</param>
    /// <returns>Updated patient</returns>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<PatientDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<PatientDto>>> Update(string id, [FromBody] UpdatePatientDto updatePatientDto)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(ApiResponse<PatientDto>.ErrorResponse("Patient ID is required", 400));
            }

            var patient = await _patientRepository.GetByIdAsync(id);
            if (patient == null)
            {
                _logger.LogWarning($"Patient not found for update: {id}");
                return NotFound(ApiResponse<PatientDto>.ErrorResponse("Patient not found", 404));
            }

            _mapper.Map(updatePatientDto, patient);
            _patientRepository.Update(patient);
            await _patientRepository.SaveChangesAsync();

            var patientDto = _mapper.Map<PatientDto>(patient);
            _logger.LogInformation($"Updated patient: {id}");

            return Ok(ApiResponse<PatientDto>.SuccessResponse(patientDto, "Patient updated successfully"));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error updating patient: {id}");
            return StatusCode(500, ApiResponse<PatientDto>.ErrorResponse("An error occurred while updating the patient", 500));
        }
    }

    /// <summary>
    /// Delete patient (soft delete)
    /// </summary>
    /// <param name="id">Patient ID</param>
    /// <returns>Deletion status</returns>
    [HttpDelete("{id}")]
    [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse>> Delete(string id)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequest(ApiResponse.ErrorResponse("Patient ID is required", 400));
            }

            var patient = await _patientRepository.GetByIdAsync(id);
            if (patient == null)
            {
                _logger.LogWarning($"Patient not found for deletion: {id}");
                return NotFound(ApiResponse.ErrorResponse("Patient not found", 404));
            }

            patient.IsActive = false;
            _patientRepository.Update(patient);
            await _patientRepository.SaveChangesAsync();

            _logger.LogInformation($"Deleted patient: {id}");
            return Ok(ApiResponse.SuccessResponse("Patient deleted successfully", 200));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting patient: {id}");
            return StatusCode(500, ApiResponse.ErrorResponse("An error occurred while deleting the patient", 500));
        }
    }
}
