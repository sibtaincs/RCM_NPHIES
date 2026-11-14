using AutoMapper;
using AutoMapper.Internal;
using Microsoft.Extensions.DependencyInjection;
using NPhies_FHIR_Integration.Application.Mapping;

namespace NPhies_FHIR_Integration.Tests.Nphies.Security;

/// <summary>
/// Guards the AutoMapper registration in ApiService <c>Program</c>: the legacy controllers take <see cref="IMapper"/>
/// from DI, and nothing else resolves it in tests. <c>AddAutoMapper(cfg, markerTypes)</c> scans the marker types'
/// assemblies, so profiles that are not named in Program (for example <see cref="PatientMappingProfile"/>) must load too.
/// </summary>
[TestClass]
public class AutoMapperRegistrationTests
{
    private static ApiServiceTestHost _host = null!;

    [ClassInitialize]
    public static void Start(TestContext _) => _host = ApiServiceTestHost.Start("Development");

    [ClassCleanup]
    public static void Stop() => _host?.Dispose();

    [TestMethod]
    public void Mapper_resolves_from_the_api_container_with_all_application_profiles()
    {
        using var scope = _host.Services.CreateScope();
        var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

        var profiles = mapper.ConfigurationProvider.Internal().Profiles.Select(p => p.Name).ToHashSet();

        foreach (var profile in new[]
                 {
                     typeof(ApplicationMappingProfile), typeof(EligibilityMappingProfile), typeof(MasterDataMappingProfile),
                     typeof(PatientMappingProfile),
                 })
        {
            Assert.IsTrue(profiles.Contains(profile.FullName!), $"{profile.Name} is not registered with AutoMapper.");
        }
    }
}
