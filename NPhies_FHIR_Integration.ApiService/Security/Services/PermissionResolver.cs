using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.ApiService.Security.Services;

/// <summary>
/// Resolves the effective permission set for a user based on the roles
/// stored on <c>User.Roles</c> and the grants in the AppRolePermissions table.
/// </summary>
public interface IPermissionResolver
{
    /// <summary>All permission names granted to the given role names.</summary>
    Task<List<string>> GetPermissionsForRolesAsync(IEnumerable<string> roleNames);
}

/// <inheritdoc />
public class PermissionResolver : IPermissionResolver
{
    private readonly ApplicationDbContext _db;

 public PermissionResolver(ApplicationDbContext db) => _db = db;

    public async Task<List<string>> GetPermissionsForRolesAsync(IEnumerable<string> roleNames)
    {
  var names = roleNames?.Distinct().ToList() ?? [];
        if (names.Count == 0) return [];

        return await _db.AppRolePermissions
    .Where(rp => rp.Role != null
       && names.Contains(rp.Role.Name)
       && rp.Role.IsActive
   && rp.Permission != null
         && rp.Permission.IsActive)
      .Select(rp => rp.Permission!.Name)
    .Distinct()
     .ToListAsync();
 }
}
