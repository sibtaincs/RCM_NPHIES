using System.Net;
using Hl7.Fhir.Serialization;
using Hl7Model = Hl7.Fhir.Model;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NPhies_FHIR_Integration.Application.Extensions;
using NPhies_FHIR_Integration.Application.Nphies.Constants;
using NPhies_FHIR_Integration.Application.Nphies.Messaging;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;
using NPhies_FHIR_Integration.Infrastructure.Nphies;
using Task = System.Threading.Tasks.Task;
using Claim = NPhies_FHIR_Integration.Domain.Entities.Claim;
using Organization = NPhies_FHIR_Integration.Domain.Entities.Organization;
using Patient = NPhies_FHIR_Integration.Domain.Entities.Patient;
using Coverage = NPhies_FHIR_Integration.Domain.Entities.Coverage;
using Practitioner = NPhies_FHIR_Integration.Domain.Entities.Practitioner;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

/// <summary>Fake NPHIES endpoint: records every outbound message and replies via <see cref="Respond"/>.</summary>
public sealed class FakeNphiesTransport : INphiesTransport
{
    public Func<NphiesTransportRequest, string>? Respond { get; set; }
    public List<NphiesTransportRequest> Requests { get; } = new();

    public Task<NphiesTransportResponse> SendAsync(NphiesTransportRequest request, CancellationToken cancellationToken = default)
    {
        Requests.Add(request);
        var json = (Respond ?? RcmFixtures.Acknowledgement)(request);
        return Task.FromResult(new NphiesTransportResponse(HttpStatusCode.OK, json, "https://hsb.nphies.sa/$process-message", TimeSpan.Zero));
    }

    public List<string> EventCodes => Requests.Select(r => r.EventCode ?? string.Empty).ToList();
}

public sealed class MemoryAuditStore : INphiesMessageAuditStore
{
    public List<NphiesMessageLog> Logs { get; } = new();

    public Task SaveAsync(NphiesMessageLog log, CancellationToken cancellationToken = default)
    {
        Logs.Add(log);
        return Task.CompletedTask;
    }
}

public static class RcmFixtures
{
    /// <summary>Generic acknowledgement reply for messages NPHIES only acknowledges (communication, payment-notice, batch).</summary>
    public static string Acknowledgement(NphiesTransportRequest request)
    {
        var header = new Hl7Model.MessageHeader
        {
            Id = Guid.NewGuid().ToString(),
            Meta = new Hl7Model.Meta { Profile = new[] { NphiesProfiles.MessageHeader } },
            Event = new Hl7Model.Coding(NphiesCodeSystems.MessageEvents, "acknowledgement"),
            Sender = new Hl7Model.ResourceReference { Type = "Organization", Identifier = new Hl7Model.Identifier("http://nphies.sa/license/nphies", "NPHIES") },
            Source = new Hl7Model.MessageHeader.MessageSourceComponent { Endpoint = "http://nphies.sa" },
            Response = new Hl7Model.MessageHeader.ResponseComponent { Identifier = request.MessageId ?? Guid.NewGuid().ToString(), Code = Hl7Model.MessageHeader.ResponseType.Ok },
        };
        header.Destination.Add(new Hl7Model.MessageHeader.MessageDestinationComponent
        {
            Endpoint = "http://provider.com",
            Receiver = new Hl7Model.ResourceReference { Type = "Organization", Identifier = new Hl7Model.Identifier("http://nphies.sa/license/provider-license", RcmTestHost.ProviderLicense) },
        });
        var bundle = new Hl7Model.Bundle
        {
            Id = Guid.NewGuid().ToString(),
            Meta = new Hl7Model.Meta { Profile = new[] { NphiesProfiles.Bundle } },
            Type = Hl7Model.Bundle.BundleType.Message,
            Timestamp = DateTimeOffset.UtcNow,
        };
        bundle.Entry.Add(new Hl7Model.Bundle.EntryComponent { FullUrl = "urn:uuid:" + header.Id, Resource = header });
        return new FhirJsonSerializer().SerializeToString(bundle);
    }
}

/// <summary>
/// Integration host for the RCM workflow layer: real EF Core (local SQL Server), real builder/validator/parser/
/// messaging client and RCM services, with a fake NPHIES transport.
/// </summary>
public sealed class RcmTestHost : IAsyncDisposable
{
    private static readonly string DatabaseName = $"NPhiesRcmTests_{Guid.NewGuid():N}";

    /// <summary>
    /// SQL Server used by the integration tests, without a database name. Override with the
    /// <c>NPHIES_TEST_SQLSERVER</c> environment variable (CI and Docker use SQL authentication), e.g.
    /// <c>Server=localhost,1433;User Id=sa;Password=...;TrustServerCertificate=true</c>.
    /// </summary>
    private static readonly string ServerConnectionString =
        Environment.GetEnvironmentVariable("NPHIES_TEST_SQLSERVER") is { Length: > 0 } configured
            ? configured
            : "Server=localhost;Integrated Security=true;TrustServerCertificate=true";

    private static readonly string ConnectionString = WithDatabase(DatabaseName);

    private static string WithDatabase(string database) =>
        new SqlConnectionStringBuilder(ServerConnectionString) { InitialCatalog = database }.ConnectionString;
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static bool _created;
    private static string? _unavailable;

    public const string ProviderLicense = "PR-FHIR";
    public const string PayerLicense = "INS-FHIR";

    public FakeNphiesTransport Transport { get; } = new();
    public MemoryAuditStore Audit { get; } = new();
    public ServiceProvider Services { get; }

    private RcmTestHost(Dictionary<string, string?>? overrides)
    {
        var settings = new Dictionary<string, string?>
        {
            ["Nphies:Sender:ProviderLicense"] = ProviderLicense,
            ["Nphies:Sender:ProviderBaseUrl"] = "http://provider.com",
            ["Nphies:PollMaxCount"] = "10",
            ["Nphies:EnableMessageAudit"] = "false",
            ["Rcm:RequireReviewApprovalForClaims"] = "false",
            ["Rcm:AutoAcknowledgePayments"] = "true",
            ["Rcm:MaxPollsPerCycle"] = "3",
        };
        if (overrides != null)
            foreach (var kv in overrides) settings[kv.Key] = kv.Value;
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddDbContext<ApplicationDbContext>(o => o.UseSqlServer(ConnectionString));
        services.AddSingleton<INphiesMessageAuditStore>(Audit);
        services.AddNphiesMessagingCore(configuration);
        services.AddSingleton<INphiesTransport>(Transport);
        services.AddRcmWorkflows(configuration);
        Services = services.BuildServiceProvider();
    }

    /// <summary>Creates a host against a freshly emptied database; marks the test inconclusive without SQL Server.</summary>
    public static async Task<RcmTestHost> CreateAsync(Dictionary<string, string?>? overrides = null)
    {
        var host = new RcmTestHost(overrides);
        await Gate.WaitAsync();
        try
        {
            if (_unavailable != null)
            {
                if (string.Equals(Environment.GetEnvironmentVariable("NPHIES_TEST_REQUIRE_SQLSERVER"), "true", StringComparison.OrdinalIgnoreCase))
                    Assert.Fail(_unavailable);
                Assert.Inconclusive(_unavailable);
            }
            await using var scope = host.Services.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            if (!_created)
            {
                try
                {
                    await db.Database.MigrateAsync();
                    _created = true;
                }
                catch (SqlException ex)
                {
                    _unavailable = $"Local SQL Server is not available for RCM integration tests: {ex.Message}";
                    // CI sets NPHIES_TEST_REQUIRE_SQLSERVER so a broken database fails the build instead of skipping silently.
                    if (string.Equals(Environment.GetEnvironmentVariable("NPHIES_TEST_REQUIRE_SQLSERVER"), "true", StringComparison.OrdinalIgnoreCase))
                        Assert.Fail(_unavailable);
                    Assert.Inconclusive(_unavailable);
                }
            }
            await ResetAsync(db);
        }
        finally
        {
            Gate.Release();
        }
        return host;
    }

    private static async Task ResetAsync(ApplicationDbContext db)
    {
        const string sql = @"
EXEC sp_MSforeachtable 'IF OBJECT_NAME(OBJECT_ID(''?'')) <> ''__EFMigrationsHistory'' ALTER TABLE ? NOCHECK CONSTRAINT ALL';
EXEC sp_MSforeachtable 'SET QUOTED_IDENTIFIER ON; IF OBJECT_NAME(OBJECT_ID(''?'')) <> ''__EFMigrationsHistory'' DELETE FROM ?';
EXEC sp_MSforeachtable 'IF OBJECT_NAME(OBJECT_ID(''?'')) <> ''__EFMigrationsHistory'' ALTER TABLE ? WITH CHECK CHECK CONSTRAINT ALL';";
        await db.Database.ExecuteSqlRawAsync(sql);
    }

    public static async Task DropDatabaseAsync()
    {
        if (!_created) return;
        SqlConnection.ClearAllPools();
        await using var connection = new SqlConnection(WithDatabase("master"));
        await connection.OpenAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = $"IF DB_ID('{DatabaseName}') IS NOT NULL BEGIN ALTER DATABASE [{DatabaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{DatabaseName}]; END";
        await cmd.ExecuteNonQueryAsync();
        _created = false;
    }

    /// <summary>Resolves <typeparamref name="TService"/> in a fresh DI scope (one DbContext per "request", like the API).</summary>
    public async Task<T> RunAsync<TService, T>(Func<TService, Task<T>> action) where TService : notnull
    {
        await using var scope = Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<TService>());
    }

    public async Task<T> QueryAsync<T>(Func<ApplicationDbContext, Task<T>> query)
    {
        await using var scope = Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        return await query(db);
    }

    public Task<Claim> GetClaimAsync(string id) => QueryAsync(db => db.Claims.AsNoTracking().SingleAsync(c => c.Id == id));

    /// <summary>Replies to every request with the given docs example.</summary>
    public void ReplyWith(string docsFile)
    {
        var json = DocsExamples.ReadJson(docsFile);
        Transport.Respond = _ => json;
    }

    public void ReplyWith(Func<NphiesTransportRequest, string> responder) => Transport.Respond = responder;

    // ===================== Seeding =====================

    public sealed class Seed
    {
        public Organization Provider { get; set; } = null!;
        public Organization Insurer { get; set; } = null!;
        public Patient Patient { get; set; } = null!;
        public Coverage Coverage { get; set; } = null!;
        public Practitioner Practitioner { get; set; } = null!;
    }

    public async Task<Seed> SeedPartiesAsync()
    {
        var seed = new Seed
        {
            Provider = new Organization
            {
                OrganizationName = "Happy Valley Dental",
                LicenseNumber = ProviderLicense,
                LicenseSystem = "http://nphies.sa/license/provider-license",
                NphiesProviderId = ProviderLicense,
                OrganizationType = "prov",
                ProviderTypeCode = "5",
                SpecializationType = "dental",
            },
            Insurer = new Organization
            {
                OrganizationName = "Saudi National Insurance",
                LicenseNumber = PayerLicense,
                LicenseSystem = "http://nphies.sa/license/payer-license",
                NphiesPayerId = PayerLicense,
                OrganizationType = "ins",
                SpecializationType = "insurer",
            },
        };
        seed.Patient = new Patient
        {
            MRN = "MRN-0001",
            NationalId = "2234567890",
            NationalIdType = "PRC",
            FirstName = "Ahmad",
            LastName = "Khaled",
            FullName = "Ahmad Khaled",
            FirstNameArabic = "أحمد",
            LastNameArabic = "خالد",
            Gender = "M",
            DateOfBirth = new DateTime(1985, 3, 12),
            Phone = "+966512345678",
            MaritalStatus = "M",
        };
        seed.Coverage = new Coverage
        {
            PolicyNumber = "POL-2234",
            MemberID = "0000000001",
            CoverageType = "EHCPOL",
            SubscriberRelationship = "self",
            PatientId = seed.Patient.Id,
            InsurerId = seed.Insurer.Id,
            CoverageStartDate = DateTime.UtcNow.Date.AddYears(-1),
            CoverageEndDate = DateTime.UtcNow.Date.AddYears(1),
            CoverageClassType = "group",
            CoverageClassValue = "CB135",
            PlanCode = "Insurance Plan A",
        };
        seed.Practitioner = new Practitioner
        {
            FirstName = "Sara",
            LastName = "Salem",
            LicenseNumber = "7-DR-0001",
            PractitionerLicenseNumber = "7-DR-0001",
            LicenseSystem = "http://nphies.sa/license/practitioner-license",
            Specialization = "22.00",
            OrganizationId = seed.Provider.Id,
        };

        await QueryAsync(async db =>
        {
            db.AddRange(seed.Provider, seed.Insurer, seed.Patient);
            db.Coverages.Add(seed.Coverage);
            db.Practitioners.Add(seed.Practitioner);
            await db.SaveChangesAsync();
            return 0;
        });
        return seed;
    }

    /// <summary>
    /// Seeds an oral (dental) claim or prior authorization mirroring the docs examples. <paramref name="identifier"/> is the
    /// provider claim identifier so unmodified fixture responses (whose ClaimResponse.request references it) correlate.
    /// </summary>
    public async Task<Claim> SeedClaimAsync(Seed seed, string identifier, bool preAuth = false, params (string Code, decimal Net)[] items)
    {
        if (items.Length == 0) items = new[] { ("70000-00-00", 230m) };
        var claim = new Claim
        {
            ClaimNumber = identifier.Replace("req_", "CLM-"),
            ClaimIdentifierSystem = preAuth ? "http://provider.com/authorization" : "http://provider.com/claim",
            ClaimIdentifierValue = identifier,
            ClaimType = "oral",
            ClaimSubType = "op",
            Use = preAuth ? "preauthorization" : "claim",
            CreatedDate = DateTime.UtcNow,
            ServicedPeriodStart = DateTime.UtcNow.Date,
            PatientId = seed.Patient.Id,
            CoverageId = seed.Coverage.Id,
            ProviderId = seed.Provider.Id,
            InsurerId = seed.Insurer.Id,
            PractitionerId = seed.Practitioner.Id,
            PayeeType = "provider",
            Total = items.Sum(i => i.Net),
            TechnicalReviewStatus = "Approved",
            MedicalReviewStatus = "Approved",
        };
        claim.CareTeam.Add(new ClaimCareTeam { ClaimId = claim.Id, Sequence = 1, PractitionerId = seed.Practitioner.Id, Role = "primary", Qualification = "22.00" });
        claim.Diagnoses.Add(new ClaimDiagnosis { ClaimId = claim.Id, Sequence = 1, DiagnosisCode = "K02.1", DiagnosisDisplay = "Caries of dentine", DiagnosisType = "principal" });
        var seq = 1;
        foreach (var (code, net) in items)
        {
            claim.Items.Add(new ClaimItem
            {
                ClaimId = claim.Id,
                Sequence = seq++,
                CareTeamSequence = 1,
                DiagnosisSequence = "1",
                ProductOrServiceCode = code,
                ProductOrServiceSystem = "http://nphies.sa/terminology/CodeSystem/oral-health-op",
                Quantity = 1,
                UnitPrice = net,
                Net = net,
                PatientShare = 0,
                ServicedDate = DateTime.UtcNow.Date,
            });
        }

        await QueryAsync(async db =>
        {
            db.Claims.Add(claim);
            await db.SaveChangesAsync();
            return 0;
        });
        return claim;
    }

    public ValueTask DisposeAsync() => Services.DisposeAsync();
}

[TestClass]
public static class RcmTestAssembly
{
    [AssemblyCleanup]
    public static async Task Cleanup() => await RcmTestHost.DropDatabaseAsync();
}
