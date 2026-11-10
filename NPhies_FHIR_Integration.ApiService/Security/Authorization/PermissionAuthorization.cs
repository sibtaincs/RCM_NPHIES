using Microsoft.AspNetCore.Authorization;

namespace NPhies_FHIR_Integration.ApiService.Security.Authorization;

/// <summary>
/// Authorization requirement satisfied when the caller holds a given permission.
/// </summary>
public class PermissionRequirement : IAuthorizationRequirement
{
    public string Permission { get; }
    public PermissionRequirement(string permission) => Permission = permission;
}

/// <summary>
/// Grants access when the JWT's permission claim contains the required permission,
/// or when the caller is an Admin (Admin implicitly has everything).
/// </summary>
public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
    PermissionRequirement requirement)
    {
        if (context.User.IsInRole(SecurityConstants.ADMIN_ROLE))
{
   context.Succeed(requirement);
   return Task.CompletedTask;
        }

  var claim = context.User.FindFirst(SecurityConstants.PERMISSION_CLAIM)?.Value;
        if (!string.IsNullOrEmpty(claim))
   {
      var granted = claim.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
  if (granted.Contains(requirement.Permission, StringComparer.OrdinalIgnoreCase))
{
      context.Succeed(requirement);
     }
        }

        return Task.CompletedTask;
  }
}

/// <summary>
/// Builds permission policies on demand, so no policy registration is needed
/// for every permission name.
/// </summary>
public class PermissionPolicyProvider : IAuthorizationPolicyProvider
{
    private const string Prefix = "perm:";
    private readonly DefaultAuthorizationPolicyProvider _fallback;

    public PermissionPolicyProvider(Microsoft.Extensions.Options.IOptions<AuthorizationOptions> options)
   => _fallback = new DefaultAuthorizationPolicyProvider(options);

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync() => _fallback.GetDefaultPolicyAsync();

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync() => _fallback.GetFallbackPolicyAsync();

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (policyName.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase))
        {
      var permission = policyName[Prefix.Length..];
      var policy = new AuthorizationPolicyBuilder()
       .RequireAuthenticatedUser()
    .AddRequirements(new PermissionRequirement(permission))
   .Build();

  return Task.FromResult<AuthorizationPolicy?>(policy);
        }

   return _fallback.GetPolicyAsync(policyName);
    }

    /// <summary>Builds the policy name for a permission.</summary>
    public static string PolicyName(string permission) => $"{Prefix}{permission}";
}

/// <summary>
/// Requires the caller to hold a specific permission, e.g.
/// <c>[HasPermission(Permissions.ClaimsSubmit)]</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class HasPermissionAttribute : AuthorizeAttribute
{
    public HasPermissionAttribute(string permission)
   => Policy = PermissionPolicyProvider.PolicyName(permission);
}
