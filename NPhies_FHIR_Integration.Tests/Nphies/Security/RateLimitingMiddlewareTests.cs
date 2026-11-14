using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using NPhies_FHIR_Integration.ApiService.Security.Middleware;
using NPhies_FHIR_Integration.ApiService.Security.Services;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Security;

/// <summary>
/// The rate-limit headers must never fail a request. A lookup failure returns a status whose reset time was
/// DateTime.MinValue, and converting that as local time threw on servers east of UTC (e.g. Saudi Arabia, UTC+3),
/// turning every API call into a 500.
/// </summary>
[TestClass]
public class RateLimitingMiddlewareTests
{
    [TestMethod]
    public void Unix_seconds_treats_unspecified_times_as_utc_and_never_throws()
    {
        var utc = new DateTime(2026, 11, 13, 12, 0, 0, DateTimeKind.Utc);
        var unspecified = DateTime.SpecifyKind(utc, DateTimeKind.Unspecified); // how EF returns datetime2 values

        Assert.AreEqual(new DateTimeOffset(utc).ToUnixTimeSeconds(), RateLimitingMiddleware.ToUnixSeconds(unspecified));
        Assert.AreEqual(new DateTimeOffset(utc).ToUnixTimeSeconds(), RateLimitingMiddleware.ToUnixSeconds(utc));
        Assert.AreEqual(DateTimeOffset.MinValue.ToUnixTimeSeconds(), RateLimitingMiddleware.ToUnixSeconds(DateTime.MinValue));
        Assert.AreEqual(DateTimeOffset.MaxValue.ToUnixTimeSeconds(), RateLimitingMiddleware.ToUnixSeconds(DateTime.MaxValue));
    }

    [TestMethod]
    public async Task Request_succeeds_when_the_status_lookup_returns_an_empty_reset_time()
    {
        var called = false;
        var middleware = new RateLimitingMiddleware(_ => { called = true; return Task.CompletedTask; }, NullLogger<RateLimitingMiddleware>.Instance);
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";
        context.Request.Path = "/api/rcm/claims";

        await middleware.InvokeAsync(context, new FailingStatusService());

        Assert.IsTrue(called, "The request must reach the next middleware.");
        Assert.AreEqual(DateTimeOffset.MinValue.ToUnixTimeSeconds().ToString(), context.Response.Headers["X-RateLimit-Reset"].ToString());
    }

    [TestMethod]
    public void Failed_status_lookup_reports_a_reset_time_in_the_next_minute()
    {
        var status = RateLimitStatus.Fallback();
        Assert.IsFalse(status.IsRateLimited);
        Assert.AreEqual(DateTimeKind.Utc, status.ResetTime.Kind);
        Assert.IsTrue(status.ResetTime > DateTime.UtcNow && status.ResetTime <= DateTime.UtcNow.AddMinutes(1).AddSeconds(5));
    }

    /// <summary>Mimics a lookup failure from before the fix: a default status with ResetTime = DateTime.MinValue.</summary>
    private sealed class FailingStatusService : IRateLimitingService
    {
        public Task<bool> IsRateLimitedAsync(string identifier, string endpoint, int maxRequests = 100, int windowMinutes = 1) => Task.FromResult(false);
        public Task RecordRequestAsync(string? userId, string ipAddress, string endpoint, string httpMethod) => Task.CompletedTask;
        public Task<RateLimitStatus> GetRateLimitStatusAsync(string identifier, string endpoint) =>
            Task.FromResult(new RateLimitStatus { IsRateLimited = false, MaxRequests = 100 });
    }
}
