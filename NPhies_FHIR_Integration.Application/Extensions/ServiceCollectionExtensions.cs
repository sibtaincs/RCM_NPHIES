using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.HealthChecks;
using NPhies_FHIR_Integration.Application.Mapping;
using NPhies_FHIR_Integration.Application.Nphies.Builders;
using NPhies_FHIR_Integration.Application.Nphies.Messaging;
using NPhies_FHIR_Integration.Application.Nphies.Parsing;
using NPhies_FHIR_Integration.Application.Nphies.Sandbox;
using NPhies_FHIR_Integration.Application.Nphies.Validation;
using NPhies_FHIR_Integration.Infrastructure.Configuration;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;
using NPhies_FHIR_Integration.Infrastructure.Nphies;
using Polly;
using Polly.Extensions.Http;

namespace NPhies_FHIR_Integration.Application.Extensions;

/// <summary>
/// Extension methods for configuring NPHIES services in the DI container
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds the NPHIES integration services: the messaging core and its health checks.
    /// </summary>
    public static IServiceCollection AddNphiesIntegration(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddNphiesMessagingCore(configuration);
        services.AddMemoryCache();
        services.AddNphiesHealthChecks();
        return services;
    }

    /// <summary>
    /// Registers the NPHIES messaging core: configuration, mTLS transport, bundle builder, validator,
    /// response parser, audit store and <see cref="INphiesMessagingClient"/>.
    /// </summary>
    public static IServiceCollection AddNphiesMessagingCore(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<NphiesConfiguration>(configuration.GetSection(NphiesConfiguration.SectionName));
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IValidateOptions<NphiesConfiguration>, NphiesConfigurationValidator>());

        services.TryAddSingleton<INphiesCertificateProvider, NphiesCertificateProvider>();
        services.TryAddSingleton<INphiesErrorCatalog>(NphiesErrorCatalog.Instance);
        services.TryAddSingleton<INphiesResponseParser, NphiesResponseParser>();
        services.TryAddSingleton<INphiesProfileValidator>(sp =>
        {
            var path = sp.GetRequiredService<IOptions<NphiesConfiguration>>().Value.ProfileValidation.PackagePath;
            return new NphiesProfileValidator(string.IsNullOrWhiteSpace(path) ? NphiesConformancePackage.Embedded : NphiesConformancePackage.Load(path));
        });
        services.TryAddSingleton<INphiesBundleValidator>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<NphiesConfiguration>>().Value;
            var rules = new NphiesBundleValidator(config.BatchMaxClaims, config.PollMaxCount, config.MaxMessageSizeBytes);
            var mode = config.ProfileValidation.Mode;
            return new CompositeNphiesBundleValidator(rules, mode == NphiesProfileValidationMode.Off ? null : sp.GetRequiredService<INphiesProfileValidator>(), mode);
        });
        services.TryAddSingleton<INphiesBundleBuilder, NphiesBundleBuilder>();

        services.TryAddScoped<INphiesMessageAuditStore>(sp =>
        {
            var config = sp.GetRequiredService<IOptions<NphiesConfiguration>>().Value;
            var db = config.EnableMessageAudit ? sp.GetService<ApplicationDbContext>() : null;
            return db is null
                ? new NullNphiesMessageAuditStore()
                : new EfNphiesMessageAuditStore(db, sp.GetRequiredService<ILogger<EfNphiesMessageAuditStore>>(), config.AuditStripStoredAttachments);
        });
        services.TryAddScoped<INphiesMessagingClient, NphiesMessagingClient>();

        // INphiesTransport goes through a router so the in-process simulator can answer when Nphies:Sandbox:Enabled is set.
        services.Configure<NphiesSandboxOptions>(configuration.GetSection(NphiesSandboxOptions.SectionName));
        services.TryAddSingleton<NphiesSandboxTransport>();
        services.TryAddTransient<INphiesTransport, NphiesTransportRouter>();
        services.AddHttpClient<NphiesTransport>(NphiesTransport.HttpClientName)
            .ConfigureHttpClient(client =>
            {
                // The transport enforces the NPHIES session timeout per attempt; don't let HttpClient cut retries short.
                client.Timeout = Timeout.InfiniteTimeSpan;
                client.DefaultRequestHeaders.Accept.ParseAdd("application/fhir+json");
                client.DefaultRequestHeaders.UserAgent.ParseAdd("NPHIES-FHIR-Integration/1.0");
            })
            .ConfigurePrimaryHttpMessageHandler(sp =>
            {
                var handler = new SocketsHttpHandler
                {
                    PooledConnectionLifetime = TimeSpan.FromMinutes(5),
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                };
                handler.SslOptions.EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13;
                var certificate = sp.GetRequiredService<INphiesCertificateProvider>().GetClientCertificate();
                if (certificate != null)
                    handler.SslOptions.ClientCertificates = new X509CertificateCollection { certificate };
                return handler;
            })
            .AddPolicyHandler(GetRetryPolicy);

        return services;
    }

    private static IServiceCollection AddNphiesHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck<NphiesCertificateHealthCheck>("nphies_certificate", HealthStatus.Degraded, new[] { "nphies", "mtls", "security", "ready" })
            .AddCheck<DatabaseHealthCheck>("database", HealthStatus.Unhealthy, new[] { "database", "sql", "data", "ready" });
        return services;
    }

    /// <summary>
    /// Retries are only safe when resending cannot create a duplicate business transaction.
    /// Idempotent messages (poll, status-check) retry on transient HTTP errors; everything else retries
    /// only when the connection could not be established, i.e. the request never reached NPHIES.
    /// </summary>
    internal static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider sp, HttpRequestMessage request)
    {
        var config = sp.GetRequiredService<IOptions<NphiesConfiguration>>().Value;
        if (config.RetryCount <= 0)
            return Policy.NoOpAsync<HttpResponseMessage>();

        TimeSpan Delay(int attempt) => TimeSpan.FromSeconds(config.RetryDelaySeconds * Math.Pow(2, attempt - 1));

        var idempotent = request.Options.TryGetValue(NphiesTransport.IdempotentOption, out var value) && value;
        if (idempotent)
        {
            return HttpPolicyExtensions
                .HandleTransientHttpError()
                .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                .WaitAndRetryAsync(config.RetryCount, Delay);
        }

        return Policy<HttpResponseMessage>
            .Handle<HttpRequestException>(ex => ex.HttpRequestError is HttpRequestError.ConnectionError or HttpRequestError.NameResolutionError)
            .WaitAndRetryAsync(config.RetryCount, Delay);
    }
}

/// <summary>
/// Validator for NPHIES configuration
/// </summary>
public class NphiesConfigurationValidator : IValidateOptions<NphiesConfiguration>
{
    public ValidateOptionsResult Validate(string? name, NphiesConfiguration options)
    {
        var failures = new List<string>();

        if (!Uri.TryCreate(options.BaseUrl, UriKind.Absolute, out _))
            failures.Add($"Nphies base URL for environment '{options.Environment}' must be an absolute URL.");
        if (options.Timeout <= 0)
            failures.Add("Nphies:Timeout must be greater than 0.");
        if (options.RetryCount < 0)
            failures.Add("Nphies:RetryCount must be greater than or equal to 0.");
        if (options.Polling.IntervalSeconds <= 0)
            failures.Add("Nphies:Polling:IntervalSeconds must be greater than 0.");
        if (options.Polling.MaxDurationMinutes <= 0)
            failures.Add("Nphies:Polling:MaxDurationMinutes must be greater than 0.");
        if (options.IsProduction && options.Certificate.Source == NphiesCertificateSource.None)
            failures.Add("Nphies:Certificate is required in Production (mutual TLS).");
        if (options.IsProduction && string.IsNullOrWhiteSpace(options.Sender.ProviderLicense))
            failures.Add("Nphies:Sender:ProviderLicense is required in Production.");

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }
}
