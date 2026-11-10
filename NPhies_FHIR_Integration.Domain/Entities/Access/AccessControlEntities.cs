namespace NPhies_FHIR_Integration.Domain.Entities.Access;

/// <summary>
/// An application role. The <see cref="Name"/> matches the values stored in
/// <c>User.Roles</c>, which is what the JWT role claims are built from.
/// This keeps role administration data-driven without changing authentication.
/// </summary>
public class AppRole : BaseEntity
{
    /// <summary>Unique machine name, e.g. "ClaimsProcessor". Matches User.Roles entries.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Human friendly label shown in the UI.</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>What this role is for.</summary>
    public string? Description { get; set; }

    /// <summary>
    /// System roles ship with the product and cannot be renamed or deleted
    /// (their permissions can still be edited).
    /// </summary>
    public bool IsSystemRole { get; set; }

    public ICollection<AppRolePermission> RolePermissions { get; set; } = new List<AppRolePermission>();
}

/// <summary>
/// A single granular permission, e.g. "claims:submit".
/// </summary>
public class AppPermission : BaseEntity
{
    /// <summary>Unique machine name in "category:action" form, e.g. "claims:approve".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Grouping used by the UI, e.g. "Claims", "Users", "MasterData".</summary>
  public string Category { get; set; } = string.Empty;

    /// <summary>The verb, e.g. "read", "create", "approve", "submit".</summary>
    public string Action { get; set; } = string.Empty;

    public string? Description { get; set; }

    /// <summary>System permissions are referenced by code and cannot be deleted.</summary>
    public bool IsSystemPermission { get; set; }

    public ICollection<AppRolePermission> RolePermissions { get; set; } = new List<AppRolePermission>();
}

/// <summary>
/// Join table granting a permission to a role.
/// </summary>
public class AppRolePermission
{
    public string RoleId { get; set; } = string.Empty;
public string PermissionId { get; set; } = string.Empty;

public DateTime GrantedAt { get; set; } = DateTime.UtcNow;
    public string? GrantedBy { get; set; }

    public AppRole? Role { get; set; }
    public AppPermission? Permission { get; set; }
}
