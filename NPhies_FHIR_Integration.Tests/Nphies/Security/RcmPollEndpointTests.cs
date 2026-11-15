using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NPhies_FHIR_Integration.ApiService.Security;
using NPhies_FHIR_Integration.ApiService.Security.Models;
using NPhies_FHIR_Integration.ApiService.Security.Services;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;

namespace NPhies_FHIR_Integration.Tests.Nphies.Security;

/// <summary>
/// <c>POST api/rcm/messages/poll</c>: the filter body is optional (the console posts without one), it reaches the poll
/// service, and users bound to a hospital can only poll for that hospital.
/// </summary>
[TestClass]
public class RcmPollEndpointTests
{
    private const string PollPath = "/api/rcm/messages/poll";
    private static ApiServiceTestHost _host = null!;
    private static readonly RecordingPollService Recorder = new();

    [ClassInitialize]
    public static void Start(TestContext _) => _host = ApiServiceTestHost.Start("Development", services =>
    {
        services.RemoveAll<IRcmPollService>();
        services.AddSingleton<IRcmPollService>(Recorder);
    });

    [ClassCleanup]
    public static void Stop() => _host?.Dispose();

    [TestInitialize]
    public void Reset() => Recorder.Last = null;

    [TestMethod]
    public async Task Poll_without_a_body_still_works()
    {
        var response = await PostAsync(null, Token(organizationId: null));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        Assert.IsNotNull(Recorder.Last);
        Assert.AreEqual(0, Recorder.Last!.IncludeMessageTypes.Count);
        Assert.IsNull(Recorder.Last.FocusClaimId);
    }

    [TestMethod]
    public async Task Poll_filters_reach_the_poll_service()
    {
        var response = await PostAsync("""{"count":5,"includeMessageTypes":["communication-request"],"focusClaimId":"claim-1"}""", Token(organizationId: null));

        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode, await response.Content.ReadAsStringAsync());
        Assert.AreEqual(5, Recorder.Last!.Count);
        CollectionAssert.AreEqual(new[] { "communication-request" }, Recorder.Last.IncludeMessageTypes);
        Assert.AreEqual("claim-1", Recorder.Last.FocusClaimId);
    }

    [TestMethod]
    public async Task A_hospital_user_polls_only_for_their_hospital()
    {
        var own = await PostAsync(null, Token(organizationId: "hospital-a"));
        Assert.AreEqual(HttpStatusCode.OK, own.StatusCode);
        Assert.AreEqual("hospital-a", Recorder.Last!.ProviderId);

        Recorder.Last = null;
        var other = await PostAsync("""{"providerId":"hospital-b"}""", Token(organizationId: "hospital-a"));
        Assert.AreEqual(HttpStatusCode.NotFound, other.StatusCode);
        Assert.IsNull(Recorder.Last, "The poll must not run for another hospital.");
    }

    private static async Task<HttpResponseMessage> PostAsync(string? json, string token)
    {
        using var client = _host.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Post, PollPath);
        if (json != null) request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return await client.SendAsync(request);
    }

    private static string Token(string? organizationId)
    {
        using var scope = _host.Services.CreateScope();
        return scope.ServiceProvider.GetRequiredService<IJwtService>().GenerateToken(new UserSecurityContext
        {
            UserId = "poll-test-user",
            Username = "poll-test",
            LoginTime = DateTime.UtcNow,
            OrganizationId = organizationId,
            Roles = new List<string> { "Billing" },
            Permissions = new List<string> { Permissions.ClaimsSubmit },
        }).AccessToken;
    }

    private sealed class RecordingPollService : IRcmPollService
    {
        public PollRequestDto? Last { get; set; }

        public Task<PollRunResultDto> PollOnceAsync(string? providerId = null, CancellationToken ct = default) =>
            PollAsync(new PollRequestDto { ProviderId = providerId }, ct);

        public Task<PollRunResultDto> PollAsync(PollRequestDto request, CancellationToken ct = default)
        {
            Last = request;
            return Task.FromResult(new PollRunResultDto());
        }
    }
}
