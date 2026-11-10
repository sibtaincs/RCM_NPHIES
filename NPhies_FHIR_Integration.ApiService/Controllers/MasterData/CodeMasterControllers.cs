using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.ApiService.Security;
using NPhies_FHIR_Integration.ApiService.Security.Authorization;
using NPhies_FHIR_Integration.Application.DTOs;
using NPhies_FHIR_Integration.Application.Services;

namespace NPhies_FHIR_Integration.ApiService.Controllers.MasterData;

/// <summary>
/// API Controller for Service Code Master
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Tags("Master Data - Service Codes")]
public class ServiceCodeMastersController : ControllerBase
{
    private readonly IServiceCodeMasterService _service;
    private readonly ILogger<ServiceCodeMastersController> _logger;

    public ServiceCodeMastersController(IServiceCodeMasterService service, ILogger<ServiceCodeMastersController> logger)
    {
        _service = service;
        _logger = logger;
    }

    [HttpGet]
    [HasPermission(Permissions.MasterDataRead)]
    public async Task<IActionResult> GetAll([FromQuery] int skip = 0, [FromQuery] int take = 10, [FromQuery] string? searchTerm = null)
    {
        var (items, total) = await _service.GetAllAsync(skip, take, searchTerm);
        return Ok(new { items, total });
    }

    [HttpGet("{id}")]
    [HasPermission(Permissions.MasterDataRead)]
    public async Task<IActionResult> GetById(string id) => Ok(await _service.GetByIdAsync(id));

    [HttpGet("by-category/{category}")]
    [HasPermission(Permissions.MasterDataRead)]
    public async Task<IActionResult> GetByCategory(string category) => Ok(await _service.GetByServiceCategoryAsync(category));

    [HttpGet("nphies-mapped")]
    [HasPermission(Permissions.MasterDataRead)]
    public async Task<IActionResult> GetNphiesMapped() => Ok(await _service.GetNphiesMappedAsync());

    [HttpPost]
    [HasPermission(Permissions.MasterDataManage)]
    public async Task<IActionResult> Create([FromBody] ServiceCodeMasterDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [HasPermission(Permissions.MasterDataManage)]
    public async Task<IActionResult> Update(string id, [FromBody] ServiceCodeMasterDto dto) => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id}")]
    [HasPermission(Permissions.MasterDataManage)]
    public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
        return NoContent();
    }
}

/// <summary>
/// API Controller for Medication Code Master
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[HasPermission(Permissions.MasterDataRead)]
[Tags("Master Data - Medications")]
public class MedicationCodeMastersController : ControllerBase
{
    private readonly IMedicationCodeMasterService _service;
    private readonly ILogger<MedicationCodeMastersController> _logger;

    public MedicationCodeMastersController(IMedicationCodeMasterService service, ILogger<MedicationCodeMastersController> logger)
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

    [HttpGet("by-form/{form}")]
    public async Task<IActionResult> GetByForm(string form) => Ok(await _service.GetByFormAsync(form));

    [HttpGet("controlled-substances")]
    public async Task<IActionResult> GetControlledSubstances() => Ok(await _service.GetControlledSubstancesAsync());

    [HttpPost]
    [HasPermission(Permissions.MasterDataManage)]
    public async Task<IActionResult> Create([FromBody] MedicationCodeMasterDto dto)
    {
        var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [HasPermission(Permissions.MasterDataManage)]
    public async Task<IActionResult> Update(string id, [FromBody] MedicationCodeMasterDto dto) => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id}")]
    [HasPermission(Permissions.MasterDataManage)]
  public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
   return NoContent();
    }
}

/// <summary>
/// API Controller for Medical Device Code Master
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[HasPermission(Permissions.MasterDataRead)]
[Tags("Master Data - Medical Devices")]
public class MedicalDeviceCodeMastersController : ControllerBase
{
    private readonly IMedicalDeviceCodeMasterService _service;
    private readonly ILogger<MedicalDeviceCodeMastersController> _logger;

    public MedicalDeviceCodeMastersController(IMedicalDeviceCodeMasterService service, ILogger<MedicalDeviceCodeMastersController> logger)
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

    [HttpGet("by-type/{deviceType}")]
    public async Task<IActionResult> GetByDeviceType(string deviceType) => Ok(await _service.GetByDeviceTypeAsync(deviceType));

    [HttpGet("implantable")]
    public async Task<IActionResult> GetImplantableDevices() => Ok(await _service.GetImplantableDevicesAsync());

    [HttpPost]
    [HasPermission(Permissions.MasterDataManage)]
    public async Task<IActionResult> Create([FromBody] MedicalDeviceCodeMasterDto dto)
    {
  var result = await _service.CreateAsync(dto);
      return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [HasPermission(Permissions.MasterDataManage)]
    public async Task<IActionResult> Update(string id, [FromBody] MedicalDeviceCodeMasterDto dto) => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id}")]
    [HasPermission(Permissions.MasterDataManage)]
    public async Task<IActionResult> Delete(string id)
    {
      await _service.DeleteAsync(id);
        return NoContent();
    }
}

/// <summary>
/// API Controller for Diagnosis Code Master
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[HasPermission(Permissions.MasterDataRead)]
[Tags("Master Data - Diagnosis")]
public class DiagnosisCodeMastersController : ControllerBase
{
    private readonly IDiagnosisCodeMasterService _service;
    private readonly ILogger<DiagnosisCodeMastersController> _logger;

    public DiagnosisCodeMastersController(IDiagnosisCodeMasterService service, ILogger<DiagnosisCodeMastersController> logger)
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

  [HttpGet("by-category/{category}")]
    public async Task<IActionResult> GetByCategory(string category) => Ok(await _service.GetByCategoryAsync(category));

    [HttpPost]
    [HasPermission(Permissions.MasterDataManage)]
    public async Task<IActionResult> Create([FromBody] DiagnosisCodeMasterDto dto)
    {
        var result = await _service.CreateAsync(dto);
    return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [HasPermission(Permissions.MasterDataManage)]
    public async Task<IActionResult> Update(string id, [FromBody] DiagnosisCodeMasterDto dto) => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id}")]
    [HasPermission(Permissions.MasterDataManage)]
  public async Task<IActionResult> Delete(string id)
    {
        await _service.DeleteAsync(id);
  return NoContent();
    }
}

/// <summary>
/// API Controller for Modifier Code Master
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[HasPermission(Permissions.MasterDataRead)]
[Tags("Master Data - Modifiers")]
public class ModifierCodeMastersController : ControllerBase
{
    private readonly IModifierCodeMasterService _service;
    private readonly ILogger<ModifierCodeMastersController> _logger;

    public ModifierCodeMastersController(IModifierCodeMasterService service, ILogger<ModifierCodeMastersController> logger)
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

    [HttpPost]
    [HasPermission(Permissions.MasterDataManage)]
    public async Task<IActionResult> Create([FromBody] ModifierCodeMasterDto dto)
 {
     var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

 [HttpPut("{id}")]
    [HasPermission(Permissions.MasterDataManage)]
    public async Task<IActionResult> Update(string id, [FromBody] ModifierCodeMasterDto dto) => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id}")]
    [HasPermission(Permissions.MasterDataManage)]
    public async Task<IActionResult> Delete(string id)
    {
  await _service.DeleteAsync(id);
     return NoContent();
    }
}

/// <summary>
/// API Controller for Benefit Code Master
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[HasPermission(Permissions.MasterDataRead)]
[Tags("Master Data - Benefits")]
public class BenefitCodeMastersController : ControllerBase
{
    private readonly IBenefitCodeMasterService _service;
    private readonly ILogger<BenefitCodeMastersController> _logger;

    public BenefitCodeMastersController(IBenefitCodeMasterService service, ILogger<BenefitCodeMastersController> logger)
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

    [HttpPost]
    [HasPermission(Permissions.MasterDataManage)]
    public async Task<IActionResult> Create([FromBody] BenefitCodeMasterDto dto)
 {
   var result = await _service.CreateAsync(dto);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id}")]
    [HasPermission(Permissions.MasterDataManage)]
    public async Task<IActionResult> Update(string id, [FromBody] BenefitCodeMasterDto dto) => Ok(await _service.UpdateAsync(id, dto));

    [HttpDelete("{id}")]
    [HasPermission(Permissions.MasterDataManage)]
    public async Task<IActionResult> Delete(string id)
    {
   await _service.DeleteAsync(id);
     return NoContent();
    }
}
