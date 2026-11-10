using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.ApiService.Security;
using NPhies_FHIR_Integration.ApiService.Security.Authorization;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Application.Services;

namespace NPhies_FHIR_Integration.ApiService.Controllers.MasterData;

/// <summary>
/// API Controller for NPHIES Code Mapping
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[HasPermission(Permissions.MasterDataRead)]
[Tags("Master Data - Code Mappings")]
public class NphiesCodeMappingsController : ControllerBase
{
    private readonly INphiesCodeMappingService _service;
  private readonly ILogger<NphiesCodeMappingsController> _logger;

    public NphiesCodeMappingsController(INphiesCodeMappingService service, ILogger<NphiesCodeMappingsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
 public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 10, [FromQuery] string? searchTerm = null)
    {
        var (items, total) = await _service.GetAllAsync(skip, take, searchTerm);
        return Ok(new { items, total });
    }

    [HttpGet("{id}")]
 public async Task<IActionResult> GetById(string id) => Ok(await _service.GetByIdAsync(id));

    [HttpGet("by-code-type/{codeType}")]
    public async Task<IActionResult> GetByCodeType(string codeType) => Ok(await _service.GetByCodeTypeAsync(codeType));

    [HttpGet("valid-mappings")]
  public async Task<IActionResult> GetValidMappings() => Ok(await _service.GetValidMappingsAsync());

    [HttpPost]
public async Task<IActionResult> Create([FromBody] NphiesCodeMappingDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] NphiesCodeMappingDto dto) => Ok(await _service.UpdateAsync(id, dto));

 [HttpDelete("{id}")]
   public async Task<IActionResult> Delete(string id)
 {
 await _service.DeleteAsync(id);
   return NoContent();
    }
}

/// <summary>
/// API Controller for Payer Master
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[HasPermission(Permissions.MasterDataRead)]
[Tags("Master Data - Payers")]
public class PayerMastersController : ControllerBase
{
    private readonly IPayerMasterService _service;
    private readonly ILogger<PayerMastersController> _logger;

 public PayerMastersController(IPayerMasterService service, ILogger<PayerMastersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 10, [FromQuery] string? searchTerm = null)
    {
        var (items, total) = await _service.GetAllAsync(skip, take, searchTerm);
        return Ok(new { items, total });
    }

 [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id) => Ok(await _service.GetByIdAsync(id));

    [HttpGet("nphies-members")]
    public async Task<IActionResult> GetNphiesMembers() => Ok(await _service.GetNphiesMembersAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PayerMasterDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] PayerMasterDto dto) => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
 await _service.DeleteAsync(id);
      return NoContent();
    }
}

/// <summary>
/// API Controller for Payer Policy Master
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[HasPermission(Permissions.MasterDataRead)]
[Tags("Master Data - Policies")]
public class PayerPolicyMastersController : ControllerBase
{
    private readonly IPayerPolicyMasterService _service;
    private readonly ILogger<PayerPolicyMastersController> _logger;

    public PayerPolicyMastersController(IPayerPolicyMasterService service, ILogger<PayerPolicyMastersController> logger)
    {
        _service = service;
      _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 10, [FromQuery] string? searchTerm = null)
    {
        var (items, total) = await _service.GetAllAsync(skip, take, searchTerm);
        return Ok(new { items, total });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id) => Ok(await _service.GetByIdAsync(id));

    [HttpGet("by-payer/{payerMasterId}")]
    public async Task<IActionResult> GetByPayer(string payerMasterId) => Ok(await _service.GetByPayerAsync(payerMasterId));

    [HttpGet("active-policies")]
    public async Task<IActionResult> GetActivePolicies() => Ok(await _service.GetActivePoliciesAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PayerPolicyMasterDto dto)
    {
   var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] PayerPolicyMasterDto dto) => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}

/// <summary>
/// API Controller for Policy Benefit Coverage
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[HasPermission(Permissions.MasterDataRead)]
[Tags("Master Data - Benefit Coverage")]
public class PolicyBenefitCoveragesController : ControllerBase
{
    private readonly IPolicyBenefitCoverageService _service;
    private readonly ILogger<PolicyBenefitCoveragesController> _logger;

    public PolicyBenefitCoveragesController(IPolicyBenefitCoverageService service, ILogger<PolicyBenefitCoveragesController> logger)
    {
        _service = service;
    _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 10, [FromQuery] string? searchTerm = null)
    {
        var (items, total) = await _service.GetAllAsync(skip, take, searchTerm);
        return Ok(new { items, total });
 }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id) => Ok(await _service.GetByIdAsync(id));

    [HttpGet("by-policy/{policyMasterId}")]
    public async Task<IActionResult> GetByPolicy(string policyMasterId) => Ok(await _service.GetBypolicyAsync(policyMasterId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] PolicyBenefitCoverageDto dto)
    {
        var result = await _service.CreateAsync(dto);
 return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] PolicyBenefitCoverageDto dto) => Ok(await _service.UpdateAsync(id, dto));

 [HttpDelete("{id}")]
public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
  }
}

/// <summary>
/// API Controller for Clinic Master
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[HasPermission(Permissions.MasterDataRead)]
[Tags("Master Data - Clinics")]
public class ClinicMastersController : ControllerBase
{
    private readonly IClinicMasterService _service;
    private readonly ILogger<ClinicMastersController> _logger;

    public ClinicMastersController(IClinicMasterService service, ILogger<ClinicMastersController> logger)
 {
        _service = service;
 _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 10, [FromQuery] string? searchTerm = null)
    {
        var (items, total) = await _service.GetAllAsync(skip, take, searchTerm);
        return Ok(new { items, total });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id) => Ok(await _service.GetByIdAsync(id));

    [HttpGet("by-organization/{organizationId}")]
    public async Task<IActionResult> GetByOrganization(string organizationId) => Ok(await _service.GetByOrganizationAsync(organizationId));

[HttpPost]
    public async Task<IActionResult> Create([FromBody] ClinicMasterDto dto)
    {
        var result = await _service.CreateAsync(dto);
  return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

 [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ClinicMasterDto dto) => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}

/// <summary>
/// API Controller for Doctor Master
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[HasPermission(Permissions.MasterDataRead)]
[Tags("Master Data - Doctors")]
public class DoctorMastersController : ControllerBase
{
    private readonly IDoctorMasterService _service;
    private readonly ILogger<DoctorMastersController> _logger;

    public DoctorMastersController(IDoctorMasterService service, ILogger<DoctorMastersController> logger)
    {
   _service = service;
      _logger = logger;
    }

   [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 10, [FromQuery] string? searchTerm = null)
    {
        var (items, total) = await _service.GetAllAsync(skip, take, searchTerm);
        return Ok(new { items, total });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id) => Ok(await _service.GetByIdAsync(id));

    [HttpGet("by-specialization/{specialization}")]
    public async Task<IActionResult> GetBySpecialization(string specialization) => Ok(await _service.GetBySpecializationAsync(specialization));

    [HttpGet("available-for-appointments")]
    public async Task<IActionResult> GetAvailableForAppointments() => Ok(await _service.GetAvailableForAppointmentsAsync());

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DoctorMasterDto dto)
    {
        var result = await _service.CreateAsync(dto);
     return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] DoctorMasterDto dto) => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
     await _service.DeleteAsync(id);
        return NoContent();
    }
}

/// <summary>
/// API Controller for Doctor Qualification
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[HasPermission(Permissions.MasterDataRead)]
[Tags("Master Data - Doctor Qualifications")]
public class DoctorQualificationsController : ControllerBase
{
    private readonly IDoctorQualificationService _service;
    private readonly ILogger<DoctorQualificationsController> _logger;

    public DoctorQualificationsController(IDoctorQualificationService service, ILogger<DoctorQualificationsController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 10, [FromQuery] string? searchTerm = null)
    {
        var (items, total) = await _service.GetAllAsync(skip, take, searchTerm);
        return Ok(new { items, total });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id) => Ok(await _service.GetByIdAsync(id));

    [HttpGet("by-doctor/{doctorMasterId}")]
    public async Task<IActionResult> GetByDoctor(string doctorMasterId) => Ok(await _service.GetByDoctorAsync(doctorMasterId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] DoctorQualificationDto dto)
    {
    var result = await _service.CreateAsync(dto);
 return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
 public async Task<IActionResult> Update(string id, [FromBody] DoctorQualificationDto dto) => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}

/// <summary>
/// API Controller for Claim Submission Rules
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[HasPermission(Permissions.MasterDataRead)]
[Tags("Master Data - Claim Rules")]
public class ClaimSubmissionRulesController : ControllerBase
{
    private readonly IClaimSubmissionRulesService _service;
    private readonly ILogger<ClaimSubmissionRulesController> _logger;

    public ClaimSubmissionRulesController(IClaimSubmissionRulesService service, ILogger<ClaimSubmissionRulesController> logger)
    {
        _service = service;
        _logger = logger;
    }

[HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 10, [FromQuery] string? searchTerm = null)
    {
        var (items, total) = await _service.GetAllAsync(skip, take, searchTerm);
      return Ok(new { items, total });
}

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id) => Ok(await _service.GetByIdAsync(id));

    [HttpGet("by-payer/{payerMasterId}")]
    public async Task<IActionResult> GetByPayer(string payerMasterId) => Ok(await _service.GetByPayerAsync(payerMasterId));

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ClaimSubmissionRulesDto dto)
    {
     var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, [FromBody] ClaimSubmissionRulesDto dto) => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
      await _service.DeleteAsync(id);
   return NoContent();
    }
}
