using AutoMapper;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Domain.Entities;
using Microsoft.Extensions.Logging;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.Application.Services.MasterDataServices;

#region Service Code Master Service
public class ServiceCodeMasterService : MasterDataServiceBase<ServiceCodeMaster, DTOs.ServiceCodeMasterDto>, IServiceCodeMasterService
{
    public ServiceCodeMasterService(ApplicationDbContext context, IMapper mapper, ILogger<ServiceCodeMasterService> logger)
        : base(context, mapper, logger) { }

    protected override DbSet<ServiceCodeMaster> GetDbSet() => _context.ServiceCodeMasters;

    public async Task<List<DTOs.ServiceCodeMasterDto>> GetByServiceCategoryAsync(string category)
    {
 var items = await GetDbSet().Where(s => s.ServiceCategory == category).ToListAsync();
      return _mapper.Map<List<DTOs.ServiceCodeMasterDto>>(items);
    }

    public async Task<List<DTOs.ServiceCodeMasterDto>> GetNphiesMappedAsync()
    {
    var items = await GetDbSet().Where(s => s.IsNphiesMapped && s.IsActive).ToListAsync();
        return _mapper.Map<List<DTOs.ServiceCodeMasterDto>>(items);
    }

    public async Task<DTOs.ServiceCodeMasterDto?> GetByServiceCodeAsync(string serviceCode)
    {
    var item = await GetDbSet().FirstOrDefaultAsync(s => s.ServiceCode == serviceCode);
    return item == null ? null : _mapper.Map<DTOs.ServiceCodeMasterDto>(item);
    }

    protected override IQueryable<ServiceCodeMaster> ApplySearchFilter(IQueryable<ServiceCodeMaster> query, string searchTerm)
    {
        return query.Where(s => s.ServiceCode.Contains(searchTerm) || s.ServiceName.Contains(searchTerm) || s.ServiceCategory.Contains(searchTerm));
    }
}
#endregion

#region Medication Code Master Service
public class MedicationCodeMasterService : MasterDataServiceBase<MedicationCodeMaster, DTOs.MedicationCodeMasterDto>, IMedicationCodeMasterService
{
    public MedicationCodeMasterService(ApplicationDbContext context, IMapper mapper, ILogger<MedicationCodeMasterService> logger)
    : base(context, mapper, logger) { }

    protected override DbSet<MedicationCodeMaster> GetDbSet() => _context.MedicationCodeMasters;

    public async Task<List<DTOs.MedicationCodeMasterDto>> GetByFormAsync(string form)
    {
var items = await GetDbSet().Where(m => m.Form == form).ToListAsync();
        return _mapper.Map<List<DTOs.MedicationCodeMasterDto>>(items);
    }

  public async Task<List<DTOs.MedicationCodeMasterDto>> GetControlledSubstancesAsync()
    {
      var items = await GetDbSet().Where(m => m.IsControlledSubstance && m.IsActive).ToListAsync();
    return _mapper.Map<List<DTOs.MedicationCodeMasterDto>>(items);
    }

    public async Task<DTOs.MedicationCodeMasterDto?> GetByMedicationCodeAsync(string medicationCode)
    {
   var item = await GetDbSet().FirstOrDefaultAsync(m => m.MedicationCode == medicationCode);
        return item == null ? null : _mapper.Map<DTOs.MedicationCodeMasterDto>(item);
 }

    protected override IQueryable<MedicationCodeMaster> ApplySearchFilter(IQueryable<MedicationCodeMaster> query, string searchTerm)
    {
        return query.Where(m => m.MedicationCode.Contains(searchTerm) || m.MedicationName.Contains(searchTerm) || m.Manufacturer!.Contains(searchTerm));
  }
}
#endregion

#region Medical Device Code Master Service
public class MedicalDeviceCodeMasterService : MasterDataServiceBase<MedicalDeviceCodeMaster, DTOs.MedicalDeviceCodeMasterDto>, IMedicalDeviceCodeMasterService
{
    public MedicalDeviceCodeMasterService(ApplicationDbContext context, IMapper mapper, ILogger<MedicalDeviceCodeMasterService> logger)
      : base(context, mapper, logger) { }

    protected override DbSet<MedicalDeviceCodeMaster> GetDbSet() => _context.MedicalDeviceCodeMasters;

    public async Task<List<DTOs.MedicalDeviceCodeMasterDto>> GetByDeviceTypeAsync(string deviceType)
  {
        var items = await GetDbSet().Where(d => d.DeviceType == deviceType).ToListAsync();
        return _mapper.Map<List<DTOs.MedicalDeviceCodeMasterDto>>(items);
    }

    public async Task<List<DTOs.MedicalDeviceCodeMasterDto>> GetImplantableDevicesAsync()
    {
var items = await GetDbSet().Where(d => d.IsImplantable && d.IsActive).ToListAsync();
        return _mapper.Map<List<DTOs.MedicalDeviceCodeMasterDto>>(items);
    }

    public async Task<List<DTOs.MedicalDeviceCodeMasterDto>> GetReusableDevicesAsync()
    {
        var items = await GetDbSet().Where(d => d.IsReusable && d.IsActive).ToListAsync();
      return _mapper.Map<List<DTOs.MedicalDeviceCodeMasterDto>>(items);
    }

    protected override IQueryable<MedicalDeviceCodeMaster> ApplySearchFilter(IQueryable<MedicalDeviceCodeMaster> query, string searchTerm)
    {
        return query.Where(d => d.DeviceCode.Contains(searchTerm) || d.DeviceName.Contains(searchTerm) || d.DeviceType!.Contains(searchTerm));
    }
}
#endregion

#region Diagnosis Code Master Service
public class DiagnosisCodeMasterService : MasterDataServiceBase<DiagnosisCodeMaster, DTOs.DiagnosisCodeMasterDto>, IDiagnosisCodeMasterService
{
    public DiagnosisCodeMasterService(ApplicationDbContext context, IMapper mapper, ILogger<DiagnosisCodeMasterService> logger)
        : base(context, mapper, logger) { }

    protected override DbSet<DiagnosisCodeMaster> GetDbSet() => _context.DiagnosisCodeMasters;

    public async Task<List<DTOs.DiagnosisCodeMasterDto>> GetByCategoryAsync(string category)
    {
        var items = await GetDbSet().Where(d => d.DiagnosisCategory == category).ToListAsync();
        return _mapper.Map<List<DTOs.DiagnosisCodeMasterDto>>(items);
    }

    public async Task<List<DTOs.DiagnosisCodeMasterDto>> GetOnAdmissionDiagnosesAsync()
    {
   var items = await GetDbSet().Where(d => d.IsOnAdmission && d.IsActive).ToListAsync();
    return _mapper.Map<List<DTOs.DiagnosisCodeMasterDto>>(items);
    }

    public async Task<DTOs.DiagnosisCodeMasterDto?> GetByDiagnosisCodeAsync(string diagnosisCode)
 {
 var item = await GetDbSet().FirstOrDefaultAsync(d => d.DiagnosisCode == diagnosisCode);
 return item == null ? null : _mapper.Map<DTOs.DiagnosisCodeMasterDto>(item);
    }

    protected override IQueryable<DiagnosisCodeMaster> ApplySearchFilter(IQueryable<DiagnosisCodeMaster> query, string searchTerm)
 {
        return query.Where(d => d.DiagnosisCode.Contains(searchTerm) || d.DiagnosisName.Contains(searchTerm) || d.DiagnosisCategory!.Contains(searchTerm));
    }
}
#endregion

#region Modifier Code Master Service
public class ModifierCodeMasterService : MasterDataServiceBase<ModifierCodeMaster, DTOs.ModifierCodeMasterDto>, IModifierCodeMasterService
{
    public ModifierCodeMasterService(ApplicationDbContext context, IMapper mapper, ILogger<ModifierCodeMasterService> logger)
   : base(context, mapper, logger) { }

    protected override DbSet<ModifierCodeMaster> GetDbSet() => _context.ModifierCodeMasters;

  public async Task<List<DTOs.ModifierCodeMasterDto>> GetByModifierTypeAsync(string modifierType)
    {
        var items = await GetDbSet().Where(m => m.ModifierType == modifierType).ToListAsync();
        return _mapper.Map<List<DTOs.ModifierCodeMasterDto>>(items);
 }

    public async Task<DTOs.ModifierCodeMasterDto?> GetByModifierCodeAsync(string modifierCode)
    {
        var item = await GetDbSet().FirstOrDefaultAsync(m => m.ModifierCode == modifierCode);
   return item == null ? null : _mapper.Map<DTOs.ModifierCodeMasterDto>(item);
    }

 protected override IQueryable<ModifierCodeMaster> ApplySearchFilter(IQueryable<ModifierCodeMaster> query, string searchTerm)
    {
  return query.Where(m => m.ModifierCode.Contains(searchTerm) || m.ModifierName.Contains(searchTerm));
    }
}
#endregion

#region Benefit Code Master Service
public class BenefitCodeMasterService : MasterDataServiceBase<BenefitCodeMaster, DTOs.BenefitCodeMasterDto>, IBenefitCodeMasterService
{
  public BenefitCodeMasterService(ApplicationDbContext context, IMapper mapper, ILogger<BenefitCodeMasterService> logger)
        : base(context, mapper, logger) { }

    protected override DbSet<BenefitCodeMaster> GetDbSet() => _context.BenefitCodeMasters;

    public async Task<List<DTOs.BenefitCodeMasterDto>> GetByCategoryAsync(string category)
    {
        var items = await GetDbSet().Where(b => b.BenefitCategory == category).ToListAsync();
        return _mapper.Map<List<DTOs.BenefitCodeMasterDto>>(items);
    }

    public async Task<DTOs.BenefitCodeMasterDto?> GetByBenefitCodeAsync(string benefitCode)
    {
  var item = await GetDbSet().FirstOrDefaultAsync(b => b.BenefitCode == benefitCode);
   return item == null ? null : _mapper.Map<DTOs.BenefitCodeMasterDto>(item);
    }

    protected override IQueryable<BenefitCodeMaster> ApplySearchFilter(IQueryable<BenefitCodeMaster> query, string searchTerm)
    {
  return query.Where(b => b.BenefitCode.Contains(searchTerm) || b.BenefitName.Contains(searchTerm) || b.BenefitCategory!.Contains(searchTerm));
    }
}
#endregion

#region NPHIES Code Mapping Service
public class NphiesCodeMappingService : MasterDataServiceBase<NphiesCodeMapping, DTOs.NphiesCodeMappingDto>, INphiesCodeMappingService
{
    public NphiesCodeMappingService(ApplicationDbContext context, IMapper mapper, ILogger<NphiesCodeMappingService> logger)
        : base(context, mapper, logger) { }

    protected override DbSet<NphiesCodeMapping> GetDbSet() => _context.NphiesCodeMappings;

    public async Task<List<DTOs.NphiesCodeMappingDto>> GetByCodeTypeAsync(string codeType)
    {
        var items = await GetDbSet().Where(n => n.CodeType == codeType).ToListAsync();
        return _mapper.Map<List<DTOs.NphiesCodeMappingDto>>(items);
    }

    public async Task<DTOs.NphiesCodeMappingDto?> GetByLocalCodeAsync(string localCode)
    {
        var item = await GetDbSet().FirstOrDefaultAsync(n => n.LocalCode == localCode);
      return item == null ? null : _mapper.Map<DTOs.NphiesCodeMappingDto>(item);
    }

    public async Task<DTOs.NphiesCodeMappingDto?> GetByNphiesCodeAsync(string nphiesCode)
    {
        var item = await GetDbSet().FirstOrDefaultAsync(n => n.NphiesCode == nphiesCode);
        return item == null ? null : _mapper.Map<DTOs.NphiesCodeMappingDto>(item);
    }

    public async Task<List<DTOs.NphiesCodeMappingDto>> GetValidMappingsAsync()
    {
        var items = await GetDbSet().Where(n => n.IsMappingValid && n.IsActive).ToListAsync();
        return _mapper.Map<List<DTOs.NphiesCodeMappingDto>>(items);
    }

    protected override IQueryable<NphiesCodeMapping> ApplySearchFilter(IQueryable<NphiesCodeMapping> query, string searchTerm)
    {
        return query.Where(n => n.LocalCode.Contains(searchTerm) || n.NphiesCode.Contains(searchTerm) || n.CodeType.Contains(searchTerm));
    }
}
#endregion

#region Payer Master Service
public class PayerMasterService : MasterDataServiceBase<PayerMaster, DTOs.PayerMasterDto>, IPayerMasterService
{
    public PayerMasterService(ApplicationDbContext context, IMapper mapper, ILogger<PayerMasterService> logger)
        : base(context, mapper, logger) { }

    protected override DbSet<PayerMaster> GetDbSet() => _context.PayerMasters;

    public async Task<DTOs.PayerMasterDto?> GetByPayerIdAsync(string payerId)
    {
        var item = await GetDbSet().FirstOrDefaultAsync(p => p.PayerId == payerId);
 return item == null ? null : _mapper.Map<DTOs.PayerMasterDto>(item);
    }

    public async Task<List<DTOs.PayerMasterDto>> GetNphiesMembersAsync()
    {
        var items = await GetDbSet().Where(p => p.IsNphiesMember && p.IsActive).ToListAsync();
    return _mapper.Map<List<DTOs.PayerMasterDto>>(items);
    }

    public async Task<List<DTOs.PayerMasterDto>> GetByPayerTypeAsync(string payerType)
    {
        var items = await GetDbSet().Where(p => p.PayerType == payerType).ToListAsync();
        return _mapper.Map<List<DTOs.PayerMasterDto>>(items);
    }

    protected override IQueryable<PayerMaster> ApplySearchFilter(IQueryable<PayerMaster> query, string searchTerm)
  {
        return query.Where(p => p.PayerId.Contains(searchTerm) || p.PayerName.Contains(searchTerm));
    }
}
#endregion
