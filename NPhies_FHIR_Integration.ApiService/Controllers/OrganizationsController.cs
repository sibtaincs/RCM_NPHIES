using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.Domain.DTOs;
using NPhies_FHIR_Integration.Infrastructure.Repositories;
using AutoMapper;
using NPhies_FHIR_Integration.Common.Models;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Controller for managing healthcare organizations (providers and insurers)
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Produces("application/json")]
public class OrganizationsController : BaseController
{
    private readonly IOrganizationRepository _organizationRepository;
  private readonly IMapper _mapper;
    private readonly ILogger<OrganizationsController> _logger;

    /// <summary>
    /// Constructor with dependency injection
    /// </summary>
    public OrganizationsController(IOrganizationRepository organizationRepository, IMapper mapper, ILogger<OrganizationsController> logger)
    {
  _organizationRepository = organizationRepository ?? throw new ArgumentNullException(nameof(organizationRepository));
    _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
}

    /// <summary>
    /// Get all organizations
    /// </summary>
    /// <returns>List of all organizations</returns>
    /// <response code="200">Organizations retrieved successfully</response>
 /// <response code="500">Internal server error</response>
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<OrganizationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetAllOrganizations()
    {
        try
 {
    _logger.LogInformation("Retrieving all organizations");

            var organizations = await _organizationRepository.GetAllAsync();
       var organizationDtos = _mapper.Map<IEnumerable<OrganizationDto>>(organizations);

      return Ok(organizationDtos, "Organizations retrieved successfully");
  }
        catch (Exception ex)
   {
         _logger.LogError(ex, "Error retrieving organizations");
      return InternalServerError("Failed to retrieve organizations");
      }
    }

    /// <summary>
    /// Get organization by ID
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>Organization details</returns>
    /// <response code="200">Organization found</response>
    /// <response code="404">Organization not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("{id}")]
  [ProducesResponseType(typeof(ApiResponse<OrganizationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
 public async Task<IActionResult> GetOrganizationById(string id)
    {
  try
        {
            if (string.IsNullOrEmpty(id))
return BadRequest("Organization ID is required");

     _logger.LogInformation("Retrieving organization {OrganizationId}", id);

          var organization = await _organizationRepository.GetWithDetailsAsync(id);

      if (organization == null)
            return NotFound($"Organization with ID {id} not found");

            var organizationDto = _mapper.Map<OrganizationDto>(organization);
        return Ok(organizationDto, "Organization retrieved successfully");
        }
        catch (Exception ex)
        {
   _logger.LogError(ex, "Error retrieving organization {OrganizationId}", id);
    return InternalServerError("Failed to retrieve organization");
        }
  }

    /// <summary>
    /// Get organization by license number
    /// </summary>
    /// <param name="licenseNumber">Organization license number</param>
    /// <returns>Organization details</returns>
    /// <response code="200">Organization found</response>
    /// <response code="404">Organization not found</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("license/{licenseNumber}")]
    [ProducesResponseType(typeof(ApiResponse<OrganizationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
 [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetOrganizationByLicense(string licenseNumber)
    {
try
        {
   if (string.IsNullOrEmpty(licenseNumber))
  return BadRequest("License number is required");

   _logger.LogInformation("Retrieving organization by license {LicenseNumber}", licenseNumber);

    var organization = await _organizationRepository.GetByLicenseNumberAsync(licenseNumber);

    if (organization == null)
     return NotFound($"Organization with license {licenseNumber} not found");

        var organizationDto = _mapper.Map<OrganizationDto>(organization);
     return Ok(organizationDto, "Organization retrieved successfully");
        }
   catch (Exception ex)
     {
   _logger.LogError(ex, "Error retrieving organization by license {LicenseNumber}", licenseNumber);
            return InternalServerError("Failed to retrieve organization");
        }
    }

    /// <summary>
    /// Get all active providers
    /// </summary>
    /// <returns>List of provider organizations</returns>
    /// <response code="200">Providers retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("providers/active")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<OrganizationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetActiveProviders()
    {
        try
        {
    _logger.LogInformation("Retrieving all active providers");

          var providers = await _organizationRepository.GetProvidersAsync();
            var providerDtos = _mapper.Map<IEnumerable<OrganizationDto>>(providers);

    return Ok(providerDtos, "Providers retrieved successfully");
        }
        catch (Exception ex)
        {
      _logger.LogError(ex, "Error retrieving providers");
  return InternalServerError("Failed to retrieve providers");
   }
    }

    /// <summary>
    /// Get all active insurers
    /// </summary>
    /// <returns>List of insurer organizations</returns>
    /// <response code="200">Insurers retrieved successfully</response>
    /// <response code="500">Internal server error</response>
    [HttpGet("insurers/active")]
    [ProducesResponseType(typeof(ApiResponse<IEnumerable<OrganizationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetActiveInsurers()
    {
        try
        {
     _logger.LogInformation("Retrieving all active insurers");

            var insurers = await _organizationRepository.GetInsurersAsync();
    var insurerDtos = _mapper.Map<IEnumerable<OrganizationDto>>(insurers);

  return Ok(insurerDtos, "Insurers retrieved successfully");
        }
  catch (Exception ex)
        {
          _logger.LogError(ex, "Error retrieving insurers");
  return InternalServerError("Failed to retrieve insurers");
   }
    }

    /// <summary>
    /// Create a new organization
    /// </summary>
    /// <param name="organizationDto">Organization data</param>
    /// <returns>Created organization with ID</returns>
    /// <response code="201">Organization created successfully</response>
    /// <response code="400">Invalid organization data</response>
    /// <response code="500">Internal server error</response>
    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<OrganizationDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOrganization([FromBody] OrganizationDto organizationDto)
    {
try
        {
        if (organizationDto == null)
       return BadRequest("Organization data is required");

  if (string.IsNullOrEmpty(organizationDto.OrganizationName))
           return BadRequest("Organization name is required");

         if (string.IsNullOrEmpty(organizationDto.LicenseNumber))
return BadRequest("License number is required");

        if (string.IsNullOrEmpty(organizationDto.OrganizationType))
        return BadRequest("Organization type is required");

    _logger.LogInformation("Creating new organization {OrganizationName}", organizationDto.OrganizationName);

            var organization = _mapper.Map<Domain.Entities.Organization>(organizationDto);
        var createdOrganization = await _organizationRepository.AddAsync(organization);
 await _organizationRepository.SaveChangesAsync();

      var createdOrganizationDto = _mapper.Map<OrganizationDto>(createdOrganization);
 return Created($"/api/v1/organizations/{createdOrganization.Id}", createdOrganizationDto);
    }
        catch (Exception ex)
        {
     _logger.LogError(ex, "Error creating organization");
     return InternalServerError("Failed to create organization");
       }
    }

    /// <summary>
 /// Update an existing organization
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <param name="organizationDto">Updated organization data</param>
  /// <returns>Updated organization</returns>
    /// <response code="200">Organization updated successfully</response>
    /// <response code="400">Invalid organization data</response>
    /// <response code="404">Organization not found</response>
    /// <response code="500">Internal server error</response>
 [HttpPut("{id}")]
    [ProducesResponseType(typeof(ApiResponse<OrganizationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdateOrganization(string id, [FromBody] OrganizationDto organizationDto)
    {
  try
 {
   if (string.IsNullOrEmpty(id))
         return BadRequest("Organization ID is required");

 if (organizationDto == null)
          return BadRequest("Organization data is required");

   _logger.LogInformation("Updating organization {OrganizationId}", id);

        var existingOrganization = await _organizationRepository.GetByIdAsync(id);
    if (existingOrganization == null)
 return NotFound($"Organization with ID {id} not found");

// Map only non-null values
    if (!string.IsNullOrEmpty(organizationDto.OrganizationName))
  existingOrganization.OrganizationName = organizationDto.OrganizationName;
      if (!string.IsNullOrEmpty(organizationDto.Email))
    existingOrganization.Email = organizationDto.Email;
  if (!string.IsNullOrEmpty(organizationDto.PhoneNumber))
         existingOrganization.PhoneNumber = organizationDto.PhoneNumber;
            if (!string.IsNullOrEmpty(organizationDto.Status))
 existingOrganization.Status = organizationDto.Status;

  var updatedOrganization = _organizationRepository.Update(existingOrganization);
            await _organizationRepository.SaveChangesAsync();

        var updatedOrganizationDto = _mapper.Map<OrganizationDto>(updatedOrganization);
       return Ok(updatedOrganizationDto, "Organization updated successfully");
        }
        catch (Exception ex)
        {
  _logger.LogError(ex, "Error updating organization {OrganizationId}", id);
    return InternalServerError("Failed to update organization");
        }
    }

    /// <summary>
    /// Delete an organization
    /// </summary>
    /// <param name="id">Organization ID</param>
    /// <returns>No content</returns>
    /// <response code="204">Organization deleted successfully</response>
    /// <response code="404">Organization not found</response>
    /// <response code="500">Internal server error</response>
    [HttpDelete("{id}")]
[ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> DeleteOrganization(string id)
    {
 try
        {
   if (string.IsNullOrEmpty(id))
         return BadRequest("Organization ID is required");

     _logger.LogInformation("Deleting organization {OrganizationId}", id);

    var organization = await _organizationRepository.GetByIdAsync(id);
   if (organization == null)
          return NotFound($"Organization with ID {id} not found");

  _organizationRepository.Delete(organization);
    await _organizationRepository.SaveChangesAsync();

  return NoContent();
        }
      catch (Exception ex)
    {
   _logger.LogError(ex, "Error deleting organization {OrganizationId}", id);
         return InternalServerError("Failed to delete organization");
  }
    }
}
