using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NPhies_FHIR_Integration.ApiService.DataExplorer;
using NPhies_FHIR_Integration.ApiService.MasterData;
using NPhies_FHIR_Integration.ApiService.Security;
using NPhies_FHIR_Integration.ApiService.Security.Authorization;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Generic master-data maintenance for every table that feeds a NPHIES request bundle.
/// Rows are listed through <c>GET api/data/{table}</c> (hospital / date filters, paging, export).
/// </summary>
[ApiController]
[Authorize]
[Route("api/masters")]
public sealed class MastersController : ControllerBase
{
    private readonly ApplicationDbContext _db;
    private readonly MasterCrudService _crud;
    private readonly IAuthorizationService _authorization;

    public MastersController(ApplicationDbContext db, IAuthorizationService authorization)
    {
        _db = db;
        _crud = new MasterCrudService(db);
        _authorization = authorization;
    }

    /// <summary>All master tables the user can read, grouped, with what they feed in the claim JSON and row counts.</summary>
    [HttpGet("catalog")]
    public async Task<ActionResult<IEnumerable<MasterTableInfo>>> Catalog([FromQuery] string? hospitalId, [FromQuery] bool counts = true, CancellationToken ct = default)
    {
        var hospital = ForcedHospital() ?? NullIfEmpty(hospitalId);
        var result = new List<MasterTableInfo>();
        foreach (var def in MasterCatalog.All)
        {
            var table = _crud.Registry.Find(def.Table);
            if (table is null || !await CanAsync(table.Permission)) continue;
            var info = await DescribeAsync(def, table);
            if (counts) info.Count = await _crud.CountAsync(def, hospital, ct);
            result.Add(info);
        }
        return Ok(result);
    }

    [HttpGet("{table}/meta")]
    public async Task<ActionResult<MasterTableInfo>> Meta(string table)
    {
        var (def, meta, error) = await ResolveAsync(table, null);
        return def is null ? error! : Ok(await DescribeAsync(def, meta!));
    }

    [HttpGet("{table}/{id}")]
    public async Task<IActionResult> Get(string table, string id, CancellationToken ct)
    {
        var (def, meta, error) = await ResolveAsync(table, null);
        if (def is null) return error!;
        var record = await new DataExplorerService(_db).GetAsync(meta!, id, ForcedHospital(), ct);
        return record is null ? NotFound() : Ok(record);
    }

    [HttpPost("{table}")]
    public async Task<IActionResult> Create(string table, [FromBody] Dictionary<string, JsonElement> values, CancellationToken ct)
    {
        var (def, _, error) = await ResolveAsync(table, d => d.CreatePermission);
        if (def is null) return error!;
        return await Run(async () =>
        {
            var row = await _crud.CreateAsync(def, values, ForcedHospital(), User.Identity?.Name, ct);
            return StatusCode(StatusCodes.Status201Created, row);
        });
    }

    [HttpPut("{table}/{id}")]
    public async Task<IActionResult> Update(string table, string id, [FromBody] Dictionary<string, JsonElement> values, CancellationToken ct)
    {
        var (def, _, error) = await ResolveAsync(table, d => d.UpdatePermission);
        if (def is null) return error!;
        return await Run(async () =>
        {
            var row = await _crud.UpdateAsync(def, id, values, ForcedHospital(), User.Identity?.Name, ct);
            return row is null ? NotFound() : Ok(row);
        });
    }

    /// <summary>Deletes the row, or deactivates it (IsActive=false) when other records still reference it.</summary>
    [HttpDelete("{table}/{id}")]
    public async Task<IActionResult> Delete(string table, string id, CancellationToken ct)
    {
        var (def, _, error) = await ResolveAsync(table, d => d.DeletePermission);
        if (def is null) return error!;
        return await Run(async () =>
        {
            var result = await _crud.DeleteAsync(def, id, ForcedHospital(), User.Identity?.Name, ct);
            return result is null ? NotFound() : Ok(result);
        });
    }

    /// <summary>
    /// Pick list for a foreign-key select: <c>?search=</c>, <c>hospitalId</c>, <c>take</c> and <c>f.column=value</c> filters.
    /// Works for any readable table (master or not).
    /// </summary>
    [HttpGet("lookup/{table}")]
    public async Task<ActionResult<IEnumerable<MasterLookupItem>>> Lookup(string table, [FromQuery] string? search, [FromQuery] string? hospitalId,
        [FromQuery] int take = 50, CancellationToken ct = default)
    {
        var meta = _crud.Registry.Find(table);
        if (meta is null) return NotFound(new { message = $"Unknown table '{table}'." });
        if (!await CanAsync(meta.Permission)) return Forbid();
        var filters = Request.Query.Where(q => q.Key.StartsWith("f.", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(q.Value))
            .ToDictionary(q => q.Key[2..], q => q.Value.ToString(), StringComparer.OrdinalIgnoreCase);
        try
        {
            var hospital = meta.HospitalScoped ? ForcedHospital() ?? NullIfEmpty(hospitalId) : null;
            return Ok(await _crud.LookupAsync(meta, MasterCatalog.Find(table), NullIfEmpty(search), hospital, filters, take, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private async Task<IActionResult> Run(Func<Task<IActionResult>> action)
    {
        try
        {
            return await action();
        }
        catch (MasterValidationException ex)
        {
            return BadRequest(new { message = "Validation failed.", errors = ex.Errors });
        }
        catch (MasterForbiddenException ex)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new { message = ex.Message });
        }
    }

    private async Task<MasterTableInfo> DescribeAsync(MasterDefinition def, ExplorerTable table)
    {
        var info = _crud.Describe(def);
        info.CanCreate = await CanAsync(def.CreatePermission);
        info.CanUpdate = await CanAsync(def.UpdatePermission);
        info.CanDelete = await CanAsync(def.DeletePermission);
        return info;
    }

    private async Task<(MasterDefinition? Def, ExplorerTable? Table, ActionResult? Error)> ResolveAsync(string name, Func<MasterDefinition, string>? writePermission)
    {
        var def = MasterCatalog.Find(name);
        var table = def is null ? null : _crud.Registry.Find(def.Table);
        if (def is null || table is null) return (null, null, NotFound(new { message = $"'{name}' is not a master table." }));
        if (!await CanAsync(table.Permission)) return (null, null, Forbid());
        if (writePermission is not null && !await CanAsync(writePermission(def))) return (null, null, Forbid());
        return (def, table, null);
    }

    private string? ForcedHospital()
    {
        var org = User.FindFirst(SecurityConstants.ORGANIZATION_CLAIM)?.Value;
        return !string.IsNullOrWhiteSpace(org) && !User.IsInRole(SecurityConstants.ADMIN_ROLE) ? org : null;
    }

    private async Task<bool> CanAsync(string permission)
        => (await _authorization.AuthorizeAsync(User, PermissionPolicyProvider.PolicyName(permission))).Succeeded;

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;
}
