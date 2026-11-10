using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.DependencyInjection;
using NPhies_FHIR_Integration.ApiService.Security.Models;
using NPhies_FHIR_Integration.ApiService.Security.Services;
using NPhies_FHIR_Integration.Common.Constants;

namespace NPhies_FHIR_Integration.Tests.Nphies.Security;

/// <summary>
/// Secure-by-default guard: every mapped endpoint must reject anonymous callers with 401, except an explicit allow-list.
/// Adding a new public endpoint requires updating <see cref="AnonymousAllowList"/> deliberately.
/// </summary>
[TestClass]
public class EndpointAuthorizationTests
{
    private static readonly string HealthRoute = AppConstants.ApiRoutePrefix.ToLowerInvariant() + "/health";

    /// <summary>"METHOD /route-pattern" (lower-case); "*" means the endpoint accepts any method.</summary>
    private static readonly HashSet<string> AnonymousAllowList = new(StringComparer.OrdinalIgnoreCase)
    {
        "POST /api/auth/login",
        "POST /api/auth/refresh",
        "GET /api/auth/health",
        $"GET {HealthRoute}",
        // Development-only (ServiceDefaults.MapDefaultEndpoints / MapOpenApi)
        "* /health",
        "* /alive",
        "GET /openapi/{documentname}.json",
    };

    private static ApiServiceTestHost _host = null!;

    [ClassInitialize]
    public static void Start(TestContext _) => _host = ApiServiceTestHost.Start("Development");

    [ClassCleanup]
    public static void Stop() => _host?.Dispose();

    [TestMethod]
    public void OnlyAllowListedEndpoints_AreMarkedAllowAnonymous()
    {
        var endpoints = GetEndpoints();
        Assert.IsTrue(endpoints.Count > 100, $"Expected the full API surface, found {endpoints.Count} endpoints.");

        var anonymous = endpoints
            .Where(e => e.Endpoint.Metadata.GetMetadata<IAllowAnonymous>() != null)
            .Select(e => e.Key)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var unexpected = anonymous.Except(AnonymousAllowList, StringComparer.OrdinalIgnoreCase).OrderBy(k => k).ToList();
        var missing = AnonymousAllowList.Except(anonymous, StringComparer.OrdinalIgnoreCase).OrderBy(k => k).ToList();

        Assert.AreEqual(0, unexpected.Count, "Endpoints allow anonymous access but are not allow-listed:\n" + string.Join("\n", unexpected));
        Assert.AreEqual(0, missing.Count, "Allow-listed endpoints are missing or no longer anonymous:\n" + string.Join("\n", missing));
    }

    [TestMethod]
    public async Task AnonymousRequests_AreRejected_ExceptAllowList()
    {
        using var client = _host.CreateClient();
        var failures = new List<string>();
        var checkedCount = 0;

        foreach (var endpoint in GetEndpoints().Where(e => !AnonymousAllowList.Contains(e.Key)))
        {
            var response = await SendAsync(client, endpoint);
            checkedCount++;
            if (response.StatusCode != HttpStatusCode.Unauthorized)
                failures.Add($"{endpoint.Key} -> {(int)response.StatusCode} {response.StatusCode}");
        }

        Assert.IsTrue(checkedCount > 100, $"Expected to probe the full API surface, probed {checkedCount}.");
        Assert.AreEqual(0, failures.Count,
            $"{failures.Count} of {checkedCount} endpoints did not return 401 for an anonymous request:\n" + string.Join("\n", failures));
    }

    [TestMethod]
    public async Task AllowListedEndpoints_AreReachableAnonymously()
    {
        using var client = _host.CreateClient();

        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/api/auth/health")).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync(HealthRoute)).StatusCode);
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/alive")).StatusCode);
        // /health aggregates readiness checks (incl. the database, unreachable here), so 503 is acceptable — it must not be auth-blocked.
        var health = (await client.GetAsync("/health")).StatusCode;
        Assert.IsTrue(health is HttpStatusCode.OK or HttpStatusCode.ServiceUnavailable, $"/health returned {health}");
        Assert.AreEqual(HttpStatusCode.OK, (await client.GetAsync("/openapi/v1.json")).StatusCode);

        // Malformed bodies prove the request passed authorization and reached MVC model binding (400),
        // without needing a database for the credential check.
        foreach (var path in new[] { "/api/auth/login", "/api/auth/refresh" })
        {
            var response = await client.PostAsync(path, new StringContent("{not-json", Encoding.UTF8, "application/json"));
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, path);
        }
    }

    [TestMethod]
    public async Task AuthenticatedRequest_PassesFallbackPolicy()
    {
        string token;
        using (var scope = _host.Services.CreateScope())
        {
            token = scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateToken(new UserSecurityContext
            {
                UserId = "auth-test-user",
                Username = "auth-test",
                LoginTime = DateTime.UtcNow,
            }).AccessToken;
        }

        using var client = _host.CreateClient();
        var versionPath = HealthRoute + "/version";

        Assert.AreEqual(HttpStatusCode.Unauthorized, (await client.GetAsync(versionPath)).StatusCode);

        using var request = new HttpRequestMessage(HttpMethod.Get, versionPath);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        Assert.AreEqual(HttpStatusCode.OK, (await client.SendAsync(request)).StatusCode);
    }

    [TestMethod]
    public async Task CorsPreflight_FromReactDevOrigin_IsNotBlockedByAuthorization()
    {
        using var client = _host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/rcm/claims");
        request.Headers.Add("Origin", "http://localhost:5173");
        request.Headers.Add("Access-Control-Request-Method", "GET");
        request.Headers.Add("Access-Control-Request-Headers", "authorization");

        var response = await client.SendAsync(request);

        Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        Assert.IsTrue(response.Headers.Contains("Access-Control-Allow-Origin"), "Preflight response is missing CORS headers.");
    }

    private sealed record EndpointInfo(RouteEndpoint Endpoint, string Method, string Key);

    private static List<EndpointInfo> GetEndpoints()
    {
        var source = _host.Services.GetRequiredService<EndpointDataSource>();
        var result = new List<EndpointInfo>();
        foreach (var endpoint in source.Endpoints.OfType<RouteEndpoint>())
        {
            var pattern = "/" + (endpoint.RoutePattern.RawText ?? string.Empty).TrimStart('/').ToLowerInvariant();
            var methods = endpoint.Metadata.GetMetadata<IHttpMethodMetadata>()?.HttpMethods;
            if (methods is null || methods.Count == 0)
                result.Add(new EndpointInfo(endpoint, HttpMethods.Get, $"* {pattern}"));
            else
                result.AddRange(methods.Select(m => new EndpointInfo(endpoint, m, $"{m.ToUpperInvariant()} {pattern}")));
        }
        return result.DistinctBy(e => e.Key).ToList();
    }

    private static async Task<HttpResponseMessage> SendAsync(HttpClient client, EndpointInfo endpoint)
    {
        using var request = new HttpRequestMessage(new HttpMethod(endpoint.Method), BuildUrl(endpoint.Endpoint.RoutePattern));
        if (endpoint.Method is "POST" or "PUT" or "PATCH")
            request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        return await client.SendAsync(request);
    }

    private static string BuildUrl(RoutePattern pattern)
    {
        var segments = pattern.PathSegments.Select(segment => string.Concat(segment.Parts.Select(part => part switch
        {
            RoutePatternLiteralPart literal => literal.Content,
            RoutePatternSeparatorPart separator => separator.Content,
            RoutePatternParameterPart parameter => SampleValue(parameter),
            _ => string.Empty,
        })));
        return "/" + string.Join('/', segments);
    }

    private static string SampleValue(RoutePatternParameterPart parameter)
    {
        var constraints = parameter.ParameterPolicies.Select(p => p.Content?.ToLowerInvariant()).ToList();
        if (constraints.Contains("guid")) return Guid.NewGuid().ToString();
        if (parameter.Name.Equals("documentName", StringComparison.OrdinalIgnoreCase)) return "v1";
        return "1";
    }
}
