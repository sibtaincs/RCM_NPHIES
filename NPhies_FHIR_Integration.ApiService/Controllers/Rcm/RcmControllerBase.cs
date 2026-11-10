using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NPhies_FHIR_Integration.ApiService.Security;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;

namespace NPhies_FHIR_Integration.ApiService.Controllers.Rcm;

/// <summary>
/// Common base for the <c>api/rcm/*</c> controllers: authenticated, maps <see cref="RcmWorkflowException"/>
/// to 400/404/409 and applies organization (provider/facility) scoping to list queries.
/// </summary>
[ApiController]
[Authorize]
[RcmExceptionFilter]
[Produces("application/json")]
public abstract class RcmControllerBase : ControllerBase
{
    /// <summary>
    /// Users bound to an organization only see that provider's data; admins (and users without an
    /// organization claim) can query any provider.
    /// </summary>
    protected T Scope<T>(T query) where T : RcmQuery
    {
        var org = User.FindFirst(SecurityConstants.ORGANIZATION_CLAIM)?.Value;
        if (!string.IsNullOrWhiteSpace(org) && !User.IsInRole(SecurityConstants.ADMIN_ROLE))
            query.ProviderId = org;
        return query;
    }

    protected IActionResult NotFoundResult(string what, string id) => NotFound(new { message = $"{what} '{id}' was not found." });

    /// <summary>The provider the caller is bound to (null for admins / unbound users).</summary>
    protected string? ProviderScope() => Scope(new RcmQuery()).ProviderId;

    protected string? Actor() =>
        User.FindFirst(SecurityConstants.USERNAME_CLAIM)?.Value
        ?? User.Identity?.Name
        ?? User.FindFirst(SecurityConstants.USER_ID_CLAIM)?.Value
        ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
}

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RcmExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case RcmWorkflowException ex:
                var body = new { message = ex.Message, errors = ex.Errors };
                context.Result = ex.Kind switch
                {
                    RcmErrorKind.NotFound => new NotFoundObjectResult(body),
                    RcmErrorKind.Conflict => new ConflictObjectResult(body),
                    _ => new BadRequestObjectResult(body)
                };
                context.ExceptionHandled = true;
                break;
            case ArgumentException ex:
                context.Result = new BadRequestObjectResult(new { message = ex.Message, errors = new List<string> { ex.Message } });
                context.ExceptionHandled = true;
                break;
        }
    }
}
