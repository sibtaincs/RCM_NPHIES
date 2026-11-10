using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Domain.Entities.Access;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

namespace NPhies_FHIR_Integration.ApiService.Security;

/// <summary>
/// Seeds the role catalogue, the permission catalogue and the default
/// role-to-permission grants. Idempotent: safe to run on every startup.
/// New permissions added to <see cref="Permissions.Catalogue"/> are inserted
/// automatically on the next run.
/// </summary>
public static class AccessControlSeeder
{
    /// <summary>Role definitions mirroring <see cref="SecurityConstants"/>.</summary>
    private static readonly (string Name, string Display, string Description)[] Roles =
    [
        (SecurityConstants.ADMIN_ROLE, "Administrator", "Full system and user management"),
     (SecurityConstants.RCM_PROCESSOR_ROLE, "RCM Processor", "Processes RCM workflows"),
(SecurityConstants.RCM_VIEWER_ROLE, "RCM Viewer", "Read-only access to RCM data"),
        (SecurityConstants.CLAIMS_PROCESSOR_ROLE, "Claims Processor", "Creates and submits claims"),
        (SecurityConstants.CLAIMS_REVIEWER_ROLE, "Claims Reviewer", "Reviews and approves claims"),
    (SecurityConstants.TECHNICAL_REVIEWER_ROLE, "Technical Reviewer", "Technical review of claims"),
   (SecurityConstants.MEDICAL_REVIEWER_ROLE, "Medical Reviewer", "Clinical review of claims"),
   (SecurityConstants.PAYER_USER_ROLE, "Payer User", "Payer-side access"),
        (SecurityConstants.PROVIDER_USER_ROLE, "Provider User", "Provider-side access"),
 (SecurityConstants.COMPLIANCE_OFFICER_ROLE, "Compliance Officer", "Compliance oversight"),
        (SecurityConstants.AUDITOR_ROLE, "Auditor", "Reads audit trails"),
     (SecurityConstants.ACCOUNTANT_ROLE, "Accountant", "Payments and reconciliation"),
        (SecurityConstants.USER_ROLE, "User", "Base authenticated user")
    ];

    /// <summary>Default permission grants per role (Admin receives everything).</summary>
    private static readonly Dictionary<string, string[]> DefaultGrants = new()
    {
        [SecurityConstants.RCM_PROCESSOR_ROLE] =
     [
     Permissions.ClaimsRead, Permissions.ClaimsCreate, Permissions.ClaimsUpdate,
   Permissions.PreAuthRead, Permissions.PreAuthCreate,
    Permissions.EligibilityRead, Permissions.EligibilityCheck,
  Permissions.PatientsRead, Permissions.PatientsCreate, Permissions.PatientsUpdate,
     Permissions.MasterDataRead, Permissions.ReportsRead
        ],
        [SecurityConstants.RCM_VIEWER_ROLE] =
        [
    Permissions.ClaimsRead, Permissions.PreAuthRead, Permissions.EligibilityRead,
     Permissions.PatientsRead, Permissions.MasterDataRead, Permissions.ReportsRead
        ],
        [SecurityConstants.CLAIMS_PROCESSOR_ROLE] =
        [
       Permissions.ClaimsRead, Permissions.ClaimsCreate, Permissions.ClaimsUpdate,
   Permissions.ClaimsSubmit,
    Permissions.PreAuthRead, Permissions.PreAuthCreate, Permissions.PreAuthSubmit,
   Permissions.EligibilityRead, Permissions.EligibilityCheck,
  Permissions.PatientsRead, Permissions.MasterDataRead, Permissions.ReportsRead
        ],
     [SecurityConstants.CLAIMS_REVIEWER_ROLE] =
    [
     Permissions.ClaimsRead, Permissions.PreAuthRead, Permissions.PreAuthApprove,
   Permissions.PatientsRead, Permissions.MasterDataRead, Permissions.ReportsRead
  ],
   [SecurityConstants.TECHNICAL_REVIEWER_ROLE] =
        [
   Permissions.ClaimsRead, Permissions.ClaimsTechnicalReview,
       Permissions.PreAuthRead, Permissions.PatientsRead,
    Permissions.MasterDataRead, Permissions.ReportsRead
        ],
  [SecurityConstants.MEDICAL_REVIEWER_ROLE] =
  [
   Permissions.ClaimsRead, Permissions.ClaimsMedicalReview,
       Permissions.PreAuthRead, Permissions.PatientsRead,
        Permissions.MasterDataRead, Permissions.ReportsRead
        ],
     [SecurityConstants.ACCOUNTANT_ROLE] =
    [
    Permissions.PaymentsRead, Permissions.PaymentsManage,
   Permissions.ClaimsRead, Permissions.MasterDataRead,
        Permissions.ReportsRead, Permissions.ReportsExport
        ],
      [SecurityConstants.PAYER_USER_ROLE] =
      [
     Permissions.ClaimsRead, Permissions.PreAuthRead,
       Permissions.EligibilityRead, Permissions.MasterDataRead, Permissions.ReportsRead
    ],
        [SecurityConstants.PROVIDER_USER_ROLE] =
  [
      Permissions.ClaimsRead, Permissions.ClaimsCreate,
   Permissions.PreAuthRead, Permissions.PreAuthCreate,
       Permissions.EligibilityRead, Permissions.EligibilityCheck,
    Permissions.PatientsRead, Permissions.PatientsCreate,
      Permissions.MasterDataRead
        ],
        [SecurityConstants.COMPLIANCE_OFFICER_ROLE] =
      [
  Permissions.ClaimsRead, Permissions.PreAuthRead, Permissions.PatientsRead,
     Permissions.MasterDataRead, Permissions.ReportsRead, Permissions.AuditRead
        ],
   [SecurityConstants.AUDITOR_ROLE] =
        [
      Permissions.ClaimsRead, Permissions.MasterDataRead,
     Permissions.ReportsRead, Permissions.AuditRead
        ],
        [SecurityConstants.USER_ROLE] = [Permissions.MasterDataRead]
    };

    /// <summary>Default permissions seeded for a role (empty for Admin, which receives everything).</summary>
    public static IReadOnlyCollection<string> GetDefaultGrants(string roleName) =>
        DefaultGrants.TryGetValue(roleName, out var grants) ? grants : [];

    public static async Task SeedAsync(ApplicationDbContext db, ILogger logger)
    {
        try
    {
      var permissionsByName = await SeedPermissionsAsync(db, logger);
     var rolesByName = await SeedRolesAsync(db, logger);
   await SeedGrantsAsync(db, rolesByName, permissionsByName, logger);
        }
        catch (Exception ex)
    {
   logger.LogError(ex, "? Access-control seeding failed.");
        }
    }

    private static async Task<Dictionary<string, AppPermission>> SeedPermissionsAsync(
        ApplicationDbContext db, ILogger logger)
    {
        var existing = await db.AppPermissions.ToDictionaryAsync(p => p.Name);
        var added = 0;

        foreach (var (name, category, action, description) in Permissions.Catalogue)
    {
    if (existing.ContainsKey(name)) continue;

  var permission = new AppPermission
   {
     Name = name,
Category = category,
     Action = action,
   Description = description,
     IsSystemPermission = true
        };
       db.AppPermissions.Add(permission);
  existing[name] = permission;
      added++;
    }

        if (added > 0)
      {
     await db.SaveChangesAsync();
   logger.LogInformation("? Seeded {Count} permission(s).", added);
        }

        return existing;
    }

    private static async Task<Dictionary<string, AppRole>> SeedRolesAsync(
    ApplicationDbContext db, ILogger logger)
    {
        var existing = await db.AppRoles.ToDictionaryAsync(r => r.Name);
        var added = 0;

        foreach (var (name, display, description) in Roles)
     {
    if (existing.ContainsKey(name)) continue;

    var role = new AppRole
     {
    Name = name,
      DisplayName = display,
       Description = description,
   IsSystemRole = true
   };
   db.AppRoles.Add(role);
   existing[name] = role;
      added++;
        }

        if (added > 0)
        {
    await db.SaveChangesAsync();
        logger.LogInformation("? Seeded {Count} role(s).", added);
 }

        return existing;
    }

    private static async Task SeedGrantsAsync(
        ApplicationDbContext db,
      Dictionary<string, AppRole> roles,
  Dictionary<string, AppPermission> permissions,
        ILogger logger)
 {
    // Only seed grants the first time, so administrators can freely
      // revoke defaults without them reappearing on restart.
      if (await db.AppRolePermissions.AnyAsync())
   {
   logger.LogInformation("??  Role permissions already configured. Skipping grant seed.");
        await BackfillAdminGrantsAsync(db, roles, permissions, logger);
        return;
      }

        var grants = new List<AppRolePermission>();

   // Admin gets every permission.
        if (roles.TryGetValue(SecurityConstants.ADMIN_ROLE, out var admin))
        {
   grants.AddRange(permissions.Values.Select(p => new AppRolePermission
      {
    RoleId = admin.Id,
    PermissionId = p.Id,
     GrantedBy = "seeder"
      }));
        }

        foreach (var (roleName, permissionNames) in DefaultGrants)
        {
      if (!roles.TryGetValue(roleName, out var role)) continue;

    foreach (var permissionName in permissionNames.Distinct())
     {
    if (!permissions.TryGetValue(permissionName, out var permission)) continue;

  grants.Add(new AppRolePermission
     {
       RoleId = role.Id,
      PermissionId = permission.Id,
      GrantedBy = "seeder"
     });
     }
        }

   db.AppRolePermissions.AddRange(grants);
 await db.SaveChangesAsync();
        logger.LogInformation("? Seeded {Count} role-permission grant(s).", grants.Count);
    }

    /// <summary>
    /// Keeps the Admin role's explicit grants complete when new permissions are added to the catalogue
    /// (Admin already passes every check implicitly; this keeps the Effective Access view accurate).
    /// </summary>
    private static async Task BackfillAdminGrantsAsync(
        ApplicationDbContext db,
        Dictionary<string, AppRole> roles,
        Dictionary<string, AppPermission> permissions,
        ILogger logger)
    {
        if (!roles.TryGetValue(SecurityConstants.ADMIN_ROLE, out var admin)) return;

        var granted = await db.AppRolePermissions
            .Where(rp => rp.RoleId == admin.Id)
            .Select(rp => rp.PermissionId)
            .ToListAsync();

        var missing = permissions.Values
            .Where(p => !granted.Contains(p.Id))
            .Select(p => new AppRolePermission { RoleId = admin.Id, PermissionId = p.Id, GrantedBy = "seeder" })
            .ToList();

        if (missing.Count == 0) return;

        db.AppRolePermissions.AddRange(missing);
        await db.SaveChangesAsync();
        logger.LogInformation("? Granted {Count} new permission(s) to the Admin role.", missing.Count);
    }
}
