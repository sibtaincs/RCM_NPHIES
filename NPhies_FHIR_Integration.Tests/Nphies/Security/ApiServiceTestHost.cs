using System.Diagnostics;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NPhies_FHIR_Integration.ApiService.Security.Models;
using NPhies_FHIR_Integration.ApiService.Security.Services;

namespace NPhies_FHIR_Integration.Tests.Nphies.Security;

/// <summary>
/// Runs the real ApiService <c>Program</c> in-process on Kestrel (loopback, random port).
/// Microsoft.AspNetCore.Mvc.Testing / TestHost can't be restored on the corporate network, so this mirrors what
/// WebApplicationFactory does: it captures the built <see cref="IHost"/> through the hosting DiagnosticListener.
/// </summary>
public sealed class ApiServiceTestHost : IDisposable
{
    public const string JwtSecret = "integration-test-signing-key-0123456789-abcdefghijklmnop";

    // Unreachable server: Development startup migration/seeding fails fast and is logged, not thrown.
    private const string UnreachableDatabase =
        "Server=tcp:127.0.0.1,1;Database=NPhiesAuthTests;User Id=test;Password=test;Connect Timeout=1;TrustServerCertificate=true";

    private readonly Thread _thread;
    private Exception? _startupError;

    public IHost Host { get; }
    public IServiceProvider Services => Host.Services;
    public Uri BaseAddress { get; }

    private ApiServiceTestHost(string environment)
    {
        var capture = new HostCapture();
        using var subscription = DiagnosticListener.AllListeners.Subscribe(capture);

        var args = new[]
        {
            "--urls=http://127.0.0.1:0",
            $"--environment={environment}",
            // MVC discovers controllers from the application assembly; the test host is otherwise the entry assembly.
            $"--applicationName={typeof(Program).Assembly.GetName().Name}",
            $"--JwtSettings:Secret={JwtSecret}",
            $"--ConnectionStrings:DefaultConnection={UnreachableDatabase}",
            "--Rcm:PollWorkerEnabled=false",
            "--Nphies:Sandbox:SeedActivity=false",
            "--Logging:LogLevel:Default=Warning",
        };

        var entryPoint = typeof(Program).Assembly.EntryPoint
            ?? throw new InvalidOperationException("ApiService has no entry point.");

        _thread = new Thread(() =>
        {
            try
            {
                entryPoint.Invoke(null, new object[] { args });
            }
            catch (Exception ex)
            {
                _startupError = ex;
                capture.HostBuilt.TrySetException(ex);
            }
        })
        { IsBackground = true, Name = "ApiServiceTestHost" };
        _thread.Start();

        try
        {
            Host = capture.HostBuilt.Task.WaitAsync(TimeSpan.FromSeconds(60)).GetAwaiter().GetResult();
        }
        finally
        {
            capture.Dispose();
        }

        var lifetime = Host.Services.GetRequiredService<IHostApplicationLifetime>();
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (!lifetime.ApplicationStarted.IsCancellationRequested)
        {
            if (_startupError != null) throw new InvalidOperationException("ApiService failed to start.", _startupError);
            if (DateTime.UtcNow > deadline) throw new TimeoutException("ApiService did not start within 90 seconds.");
            Thread.Sleep(50);
        }

        var address = Host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        BaseAddress = new Uri(address);
    }

    public static ApiServiceTestHost Start(string environment = "Development") => new(environment);

    public HttpClient CreateClient() => new() { BaseAddress = BaseAddress, Timeout = TimeSpan.FromSeconds(30) };

    public void Dispose()
    {
        Host.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
        _thread.Join(TimeSpan.FromSeconds(30));
    }

    private sealed class HostCapture : IObserver<DiagnosticListener>, IObserver<KeyValuePair<string, object?>>, IDisposable
    {
        private readonly List<IDisposable> _subscriptions = new();
        public TaskCompletionSource<IHost> HostBuilt { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public void OnNext(DiagnosticListener listener)
        {
            if (listener.Name == "Microsoft.Extensions.Hosting")
                lock (_subscriptions) _subscriptions.Add(listener.Subscribe(this));
        }

        public void OnNext(KeyValuePair<string, object?> value)
        {
            // Same hook WebApplicationFactory uses for ConfigureServices: replace the DB-backed request
            // middleware dependencies so the pipeline runs without SQL Server.
            if (value.Key == "HostBuilding" && value.Value is IHostBuilder builder)
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IRateLimitingService>();
                    services.AddSingleton<IRateLimitingService, NoopRateLimitingService>();
                    services.RemoveAll<IAuditLoggingService>();
                    services.AddSingleton<IAuditLoggingService, NoopAuditLoggingService>();
                });

            if (value.Key == "HostBuilt" && value.Value is IHost host)
                HostBuilt.TrySetResult(host);
        }

        public void OnCompleted() { }
        public void OnError(Exception error) { }

        public void Dispose()
        {
            lock (_subscriptions)
            {
                _subscriptions.ForEach(s => s.Dispose());
                _subscriptions.Clear();
            }
        }
    }

    private sealed class NoopRateLimitingService : IRateLimitingService
    {
        public Task<bool> IsRateLimitedAsync(string identifier, string endpoint, int maxRequests = 100, int windowMinutes = 1)
            => Task.FromResult(false);

        public Task RecordRequestAsync(string? userId, string ipAddress, string endpoint, string httpMethod) => Task.CompletedTask;

        public Task<RateLimitStatus> GetRateLimitStatusAsync(string identifier, string endpoint)
            => Task.FromResult(new RateLimitStatus { MaxRequests = 100, RemainingRequests = 100, ResetTime = DateTime.UtcNow.AddMinutes(1) });
    }

    private sealed class NoopAuditLoggingService : IAuditLoggingService
    {
        public Task LogActionAsync(AuditLogEntry entry) => Task.CompletedTask;
        public Task LogAuthenticationAsync(string username, bool success, string? userId = null, string? reason = null) => Task.CompletedTask;
        public Task LogEntityChangeAsync(string entityType, string entityId, object? oldValues, object? newValues, string action) => Task.CompletedTask;
        public Task LogSecurityEventAsync(string eventType, string message, string severity = "Warning") => Task.CompletedTask;
    }
}
