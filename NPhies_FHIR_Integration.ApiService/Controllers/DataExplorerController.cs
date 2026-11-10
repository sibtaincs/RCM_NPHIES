using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.ApiService.DataExplorer;
using NPhies_FHIR_Integration.ApiService.Security;
using NPhies_FHIR_Integration.ApiService.Security.Authorization;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.ApiService.Controllers;

/// <summary>
/// Generic read-only explorer over every table, with hospital (provider) scoping, date range,
/// search, column filters, paging, master-detail and CSV export.
/// </summary>
[ApiController]
[Authorize]
[Route("api/data")]
public sealed class DataExplorerController : ControllerBase
{
    private readonly DataExplorerService _explorer;
    private readonly IAuthorizationService _authorization;

    public DataExplorerController(ApplicationDbContext db, IAuthorizationService authorization)
    {
        _explorer = new DataExplorerService(db);
        _authorization = authorization;
    }

    [HttpGet("tables")]
    public async Task<ActionResult<IEnumerable<ExplorerTable>>> Tables()
    {
        var allowed = new List<ExplorerTable>();
        var cache = new Dictionary<string, bool>();
        foreach (var table in _explorer.Registry.Tables)
        {
            if (!cache.TryGetValue(table.Permission, out var ok))
                cache[table.Permission] = ok = await CanAsync(table.Permission);
            if (ok) allowed.Add(table);
        }
        return Ok(allowed);
    }

    [HttpGet("tables/{table}")]
    public async Task<ActionResult<ExplorerTable>> Table(string table)
    {
        var (meta, error) = await ResolveAsync(table);
        return meta is null ? error! : Ok(meta);
    }

    [HttpGet("{table}")]
    public async Task<ActionResult<ExplorerPage>> Query(string table, CancellationToken ct)
    {
        var (meta, error) = await ResolveAsync(table);
        if (meta is null) return error!;
        try
        {
            return Ok(await _explorer.QueryAsync(meta, BuildQuery(), null, ct));
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{table}/export")]
    public async Task<IActionResult> Export(string table, CancellationToken ct)
    {
        var (meta, error) = await ResolveAsync(table);
        if (meta is null) return error!;
        try
        {
            var page = await _explorer.QueryAsync(meta, BuildQuery(), DataExplorerService.MaxExportRows, ct);
            Response.Headers["X-Total-Count"] = page.Total.ToString();
            var csv = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(DataExplorerService.ToCsv(meta, page.Items))).ToArray();
            return File(csv, "text/csv", $"{meta.Name}-{DateTime.UtcNow:yyyyMMddHHmm}.csv");
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{table}/{id}")]
    public async Task<ActionResult<ExplorerRecord>> Get(string table, string id, CancellationToken ct)
    {
        var (meta, error) = await ResolveAsync(table);
        if (meta is null) return error!;
        try
        {
            var record = await _explorer.GetAsync(meta, id, ForcedHospital(), ct);
            return record is null ? NotFound() : Ok(record);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    private ExplorerQuery BuildQuery()
    {
        var q = Request.Query;
        var query = new ExplorerQuery
        {
            HospitalId = ForcedHospital() ?? NullIfEmpty(q["hospitalId"]),
            From = ParseDate(q["from"]),
            To = ParseDate(q["to"]),
            DateField = NullIfEmpty(q["dateField"]),
            Search = NullIfEmpty(q["search"]),
            SortBy = NullIfEmpty(q["sortBy"]),
            SortDesc = !string.Equals(q["sortDir"], "asc", StringComparison.OrdinalIgnoreCase),
            Page = int.TryParse(q["page"], out var page) ? page : 1,
            PageSize = int.TryParse(q["pageSize"], out var size) ? size : 25
        };
        foreach (var (key, value) in q)
        {
            if (key.StartsWith("f.", StringComparison.OrdinalIgnoreCase) && !string.IsNullOrEmpty(value))
                query.Filters[key[2..]] = value.ToString();
        }
        return query;
    }

    /// <summary>Non-admin users bound to an organization only see that hospital's rows.</summary>
    private string? ForcedHospital()
    {
        var org = User.FindFirst(SecurityConstants.ORGANIZATION_CLAIM)?.Value;
        return !string.IsNullOrWhiteSpace(org) && !User.IsInRole(SecurityConstants.ADMIN_ROLE) ? org : null;
    }

    private async Task<(ExplorerTable? Table, ActionResult? Error)> ResolveAsync(string name)
    {
        var meta = _explorer.Registry.Find(name);
        if (meta is null) return (null, NotFound(new { message = $"Unknown table '{name}'." }));
        if (!await CanAsync(meta.Permission)) return (null, Forbid());
        return (meta, null);
    }

    private async Task<bool> CanAsync(string permission)
        => (await _authorization.AuthorizeAsync(User, PermissionPolicyProvider.PolicyName(permission))).Succeeded;

    private static string? NullIfEmpty(string? s) => string.IsNullOrWhiteSpace(s) ? null : s;

    private static DateTime? ParseDate(string? s)
        => DateTime.TryParse(s, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var d) ? d : null;
}

/// <summary>Hospital / insurer pick lists for the global filter bar.</summary>
[ApiController]
[Authorize]
[Route("api/lookups")]
public sealed class LookupsController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public LookupsController(ApplicationDbContext db) => _db = db;

    [HttpGet("hospitals")]
    public async Task<IActionResult> Hospitals(CancellationToken ct)
    {
        var query = _db.Organizations.AsNoTracking().Where(o => o.OrganizationType == "prov");
        var org = User.FindFirst(SecurityConstants.ORGANIZATION_CLAIM)?.Value;
        if (!string.IsNullOrWhiteSpace(org) && !User.IsInRole(SecurityConstants.ADMIN_ROLE))
            query = query.Where(o => o.Id == org);
        return Ok(await Project(query).ToListAsync(ct));
    }

    [HttpGet("insurers")]
    public async Task<IActionResult> Insurers(CancellationToken ct)
        => Ok(await Project(_db.Organizations.AsNoTracking().Where(o => o.OrganizationType == "ins")).ToListAsync(ct));

    private static IQueryable<object> Project(IQueryable<Domain.Entities.Organization> q)
        => q.OrderBy(o => o.OrganizationName).Select(o => new
        {
            id = o.Id,
            name = o.OrganizationName,
            licenseNumber = o.LicenseNumber,
            nphiesId = o.NphiesProviderId ?? o.NphiesPayerId ?? o.NphiesOrganizationId,
            city = o.City,
            status = o.Status,
            isActive = o.IsActive
        });
}
