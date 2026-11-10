using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NPhies_FHIR_Integration.ApiService.Security;
using NPhies_FHIR_Integration.ApiService.Security.Authorization;
using NPhies_FHIR_Integration.ApiService.Security.Models;
using NPhies_FHIR_Integration.ApiService.Security.Services;
using NPhies_FHIR_Integration.Application.Services.RBAC;
using NPhies_FHIR_Integration.Domain.DTOs;

namespace NPhies_FHIR_Integration.Tests.Nphies.Security;

/// <summary>
/// Permission-based authorization: legacy role strings are gone, and the audit API is reachable by the
/// Auditor role (via <c>audit:read</c>) but forbidden to roles without that permission.
/// </summary>
[TestClass]
public class PermissionAuthorizationTests
{
    private static readonly string[] AuditEndpoints =
    [
        "/api/audit/user/1",
        "/api/audit/resource/Claim/1",
        "/api/audit/action/Login",
        "/api/audit/range?fromDate=2026-01-01&toDate=2026-02-01",
        "/api/audit/unauthorized-attempts",
        "/api/audit/high-risk-actions",
        "/api/audit/compliance-summary",
    ];

    private static ApiServiceTestHost _host = null!;

    [ClassInitialize]
    public static void Start(TestContext _) => _host = ApiServiceTestHost.Start("Development", services =>
    {
        services.RemoveAll<IPermissionAuditService>();
        services.AddSingleton<IPermissionAuditService, EmptyPermissionAuditService>();
    });

    [ClassCleanup]
    public static void Stop() => _host?.Dispose();

    [TestMethod]
    public void NoEndpoint_UsesRoleStringAuthorization()
    {
        var offenders = Endpoints()
            .Where(e => e.Metadata.GetOrderedMetadata<IAuthorizeData>().Any(a => !string.IsNullOrWhiteSpace(a.Roles)))
            .Select(e => e.RoutePattern.RawText)
            .Distinct()
            .OrderBy(r => r)
            .ToList();

        Assert.AreEqual(0, offenders.Count, "Endpoints still authorize by role string; use [HasPermission]:\n" + string.Join("\n", offenders));
    }

    [TestMethod]
    public void EveryAuditEndpoint_RequiresAuditRead()
    {
        var auditEndpoints = Endpoints()
            .Where(e => (e.RoutePattern.RawText ?? "").StartsWith("api/Audit", StringComparison.OrdinalIgnoreCase))
            .ToList();

        Assert.AreEqual(AuditEndpoints.Length, auditEndpoints.Count, "Unexpected number of audit endpoints.");
        var expected = PermissionPolicyProvider.PolicyName(Permissions.AuditRead);
        foreach (var endpoint in auditEndpoints)
        {
            var policies = endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Select(a => a.Policy).ToList();
            CollectionAssert.Contains(policies, expected, endpoint.RoutePattern.RawText);
        }
    }

    [TestMethod]
    public void AuditRead_IsSeededForAuditorAndComplianceOfficer()
    {
        CollectionAssert.Contains(AccessControlSeeder.GetDefaultGrants(SecurityConstants.AUDITOR_ROLE).ToList(), Permissions.AuditRead);
        CollectionAssert.Contains(AccessControlSeeder.GetDefaultGrants(SecurityConstants.COMPLIANCE_OFFICER_ROLE).ToList(), Permissions.AuditRead);
        Assert.IsTrue(Permissions.Catalogue.Any(p => p.Name == Permissions.AuditRead), "audit:read must be in the seeded catalogue.");
    }

    [TestMethod]
    public async Task Auditor_CanReachAllAuditEndpoints()
    {
        await AssertAuditStatusAsync(SecurityConstants.AUDITOR_ROLE, HttpStatusCode.OK);
    }

    [TestMethod]
    public async Task Admin_CanReachAuditEndpoints_WithoutExplicitGrant()
    {
        await AssertAuditStatusAsync(SecurityConstants.ADMIN_ROLE, HttpStatusCode.OK);
    }

    [DataTestMethod]
    [DataRow(SecurityConstants.CLAIMS_PROCESSOR_ROLE)]
    [DataRow(SecurityConstants.TECHNICAL_REVIEWER_ROLE)]
    [DataRow(SecurityConstants.MEDICAL_REVIEWER_ROLE)]
    [DataRow(SecurityConstants.RCM_VIEWER_ROLE)]
    [DataRow(SecurityConstants.ACCOUNTANT_ROLE)]
    [DataRow(SecurityConstants.USER_ROLE)]
    public async Task RolesWithoutAuditRead_AreForbidden(string role)
    {
        Assert.IsFalse(AccessControlSeeder.GetDefaultGrants(role).Contains(Permissions.AuditRead), $"{role} unexpectedly holds audit:read.");
        await AssertAuditStatusAsync(role, HttpStatusCode.Forbidden);
    }

    [TestMethod]
    public async Task LegacyRoleNames_NoLongerGrantAccess()
    {
        // The former guard accepted these (non-existent) role names; they must not bypass the permission check.
        using var client = _host.CreateClient();
        var token = Token(["TECHNICAL_REVIEW_MANAGER", "MEDICAL_REVIEW_MANAGER"], []);
        Assert.AreEqual(HttpStatusCode.Forbidden, (await GetAsync(client, AuditEndpoints[0], token)).StatusCode);
    }

    private static async Task AssertAuditStatusAsync(string role, HttpStatusCode expected)
    {
        using var client = _host.CreateClient();
        var token = Token([role], AccessControlSeeder.GetDefaultGrants(role));
        var failures = new List<string>();
        foreach (var path in AuditEndpoints)
        {
            var status = (await GetAsync(client, path, token)).StatusCode;
            if (status != expected) failures.Add($"{path} -> {(int)status} {status}");
        }
        Assert.AreEqual(0, failures.Count, $"{role}: expected {expected}:\n" + string.Join("\n", failures));
    }

    private static async Task<HttpResponseMessage> GetAsync(HttpClient client, string path, string token)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private static string Token(IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        using var scope = _host.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateToken(new UserSecurityContext
        {
            UserId = "perm-test-user",
            Username = "perm-test",
            LoginTime = DateTime.UtcNow,
            Roles = roles.ToList(),
            Permissions = permissions.ToList(),
        }).AccessToken;
    }

    private static IEnumerable<RouteEndpoint> Endpoints() =>
        _host.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();

    private sealed class EmptyPermissionAuditService : IPermissionAuditService
    {
        private static Task<List<PermissionAuditLogDto>> Empty() => Task.FromResult(new List<PermissionAuditLogDto>());

        public Task LogActionAsync(int userId, string action, string resourceType, int resourceId, string details, string? ipAddress = null) => Task.CompletedTask;
        public Task LogPermissionCheckAsync(int userId, string permission, bool granted, string? ipAddress = null) => Task.CompletedTask;
        public Task LogRoleAssignmentAsync(int userId, int roleId, bool assigned, string? ipAddress = null) => Task.CompletedTask;
        public Task<List<PermissionAuditLogDto>> GetUserAuditLogsAsync(int userId) => Empty();
        public Task<List<PermissionAuditLogDto>> GetResourceAuditLogsAsync(string resourceType, int resourceId) => Empty();
        public Task<List<PermissionAuditLogDto>> GetAuditLogsByActionAsync(string action) => Empty();
        public Task<List<PermissionAuditLogDto>> GetAuditLogsAsync(DateTime fromDate, DateTime toDate) => Empty();
        public Task<List<PermissionAuditLogDto>> GetUnauthorizedAccessAttemptsAsync(int days = 30) => Empty();
        public Task<List<PermissionAuditLogDto>> GetHighRiskActionsAsync() => Empty();
    }
}
