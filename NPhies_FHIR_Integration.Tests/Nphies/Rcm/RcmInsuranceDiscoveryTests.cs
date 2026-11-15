using System.Net;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Nphies.Discovery;
using NPhies_FHIR_Integration.Application.Nphies.Sandbox;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Application.Rcm.Dtos;
using NPhies_FHIR_Integration.Application.Rcm.Services;
using NPhies_FHIR_Integration.Infrastructure.Configuration;
using Task = System.Threading.Tasks.Task;

namespace NPhies_FHIR_Integration.Tests.Nphies.Rcm;

/// <summary>Insurance discovery through the NPHIES Check Insurance API (Beneficiary Discovery Insurance guide V1.0).</summary>
[TestClass]
public class RcmInsuranceDiscoveryTests
{
    private const string HidpPolicy = """
        {"PolicyNumber":"12345678","InsuranceCompanyEN":"Saudi National Insurance","InsuranceCompanyAR":"التأمين الوطني","InsuranceCompanyID":"102",
         "ExpiryDate":"2027-03-31","ClassName":"VIP","IdentityNumber":"1234567891","Name":"Ahmad Khaled","Gender":"1","NationalityID":"682",
         "DeductibleRate":"20","MaxLimit":"500","BeneficiaryType":"2","BeneficiaryNumber":"000123","NetworkID":"NW1","IssueDate":"01-04-2026",
         "SponsorNumber":"7001234567","PolicyHolder":"ACME Trading"}
        """;

    // ----- reading the response -----

    [TestMethod]
    public void A_policy_is_read_from_an_array_a_wrapper_or_a_single_object()
    {
        foreach (var body in new[] { $"[{HidpPolicy}]", $$"""{"Status":"OK","Insurance":[{{HidpPolicy}}]}""", HidpPolicy })
        {
            var result = InsuranceDiscoveryParser.Parse(body, 200);
            Assert.IsNull(result.ErrorCode, body);
            var policy = result.Policies.Single();
            Assert.AreEqual("12345678", policy.PolicyNumber);
            Assert.AreEqual("102", policy.InsuranceCompanyId);
            Assert.AreEqual(new DateTime(2027, 3, 31), policy.ExpiryDate, "ExpiryDate is YYYY-MM-DD.");
            Assert.AreEqual(new DateTime(2026, 4, 1), policy.IssueDate, "The HIDP IssueDate is DD-MM-YYYY.");
            Assert.AreEqual("male", policy.Gender);
            Assert.AreEqual(20m, policy.DeductibleRate);
            Assert.AreEqual(500m, policy.MaxLimit);
            Assert.AreEqual("2", policy.BeneficiaryType);
            Assert.AreEqual("ACME Trading", policy.Raw["policyholder"], "Every field is kept as returned.");
        }
    }

    [TestMethod]
    public void Field_names_are_matched_without_regard_to_case_and_numbers_may_be_unquoted()
    {
        var result = InsuranceDiscoveryParser.Parse("""[{"policyNumber":"P-1","insuranceCompanyId":101,"membershipCode":"HU-9","insuredName":"Visitor","gender":2}]""", 200);
        var policy = result.Policies.Single();
        Assert.AreEqual("101", policy.InsuranceCompanyId);
        Assert.AreEqual("HU-9", policy.BeneficiaryNumber, "The Hajj and Umrah MembershipCode is the member number.");
        Assert.AreEqual("Visitor", policy.Name);
        Assert.AreEqual("female", policy.Gender);
    }

    [TestMethod]
    public void Errors_are_read_from_an_object_or_a_bare_code()
    {
        var notFound = InsuranceDiscoveryParser.Parse("""{"ErrorCode":"105","ErrorDescription":"No record found"}""", 200);
        Assert.IsTrue(notFound.NotFound);
        Assert.IsFalse(notFound.IsError, "No record is an answer, not a failure.");

        var header = InsuranceDiscoveryParser.Parse("""{"code":108}""", 401);
        Assert.AreEqual("108", header.ErrorCode);
        Assert.AreEqual("Invalid Authorization Header", header.ErrorMessage);
        Assert.IsTrue(header.IsError);

        Assert.AreEqual("105", InsuranceDiscoveryParser.Parse("105", 200).ErrorCode);
        Assert.AreEqual("unreadable", InsuranceDiscoveryParser.Parse("<html>Bad gateway</html>", 502).ErrorCode);
        Assert.AreEqual("http", InsuranceDiscoveryParser.Parse("", 500).ErrorCode);
    }

    [TestMethod]
    public void An_obviously_bad_key_is_refused_before_anything_is_sent()
    {
        Assert.AreEqual("101", NphiesInsuranceDiscoveryClient.Validate(" ", InsuranceSystemType.Hidp)!.ErrorCode);
        Assert.AreEqual("102", NphiesInsuranceDiscoveryClient.Validate("12345", InsuranceSystemType.Hidp)!.ErrorCode);
        Assert.AreEqual("103", NphiesInsuranceDiscoveryClient.Validate("12345ABCDE", InsuranceSystemType.Hidp)!.ErrorCode);
        Assert.AreEqual("104", NphiesInsuranceDiscoveryClient.Validate("1234567891", (InsuranceSystemType)9)!.ErrorCode);
        Assert.IsNull(NphiesInsuranceDiscoveryClient.Validate("1234567891", InsuranceSystemType.Hidp));
        Assert.IsNull(NphiesInsuranceDiscoveryClient.Validate("A1234567", InsuranceSystemType.Huidp), "A passport number is not 10 digits.");
    }

    // ----- the HTTP call -----

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = new();
        public string Body { get; set; } = "[]";

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(Body) });
        }
    }

    private static NphiesInsuranceDiscoveryClient Client(RecordingHandler handler, string environment = "Sandbox", string? authorization = null, bool sandbox = false) =>
        new(new HttpClient(handler),
            Options.Create(new NphiesConfiguration { Environment = environment }),
            Options.Create(new NphiesDiscoveryOptions { AuthorizationHeader = authorization }),
            Options.Create(new NphiesSandboxOptions { Enabled = sandbox }),
            NullLogger<NphiesInsuranceDiscoveryClient>.Instance);

    [TestMethod]
    public async Task The_call_is_a_get_with_the_patient_key_and_system_type()
    {
        var handler = new RecordingHandler { Body = $"[{HidpPolicy}]" };

        var result = await Client(handler, authorization: "Bearer token-1").CheckAsync(" 1234567891 ", InsuranceSystemType.Hidp);

        var request = handler.Requests.Single();
        Assert.AreEqual(HttpMethod.Get, request.Method);
        Assert.AreEqual("http://hsb.oba.nphies.sa/check-insurance?PatientKey=1234567891&SystemType=1", request.RequestUri!.ToString());
        Assert.AreEqual("Bearer token-1", request.Headers.GetValues("Authorization").Single());
        Assert.AreEqual(1, result.Policies.Count);

        await Client(handler, environment: "Production").CheckAsync("P 123/45", InsuranceSystemType.Huidp);
        Assert.AreEqual("https://hsb.nphies.sa/check-insurance?PatientKey=P%20123%2F45&SystemType=5", handler.Requests[1].RequestUri!.AbsoluteUri);
        Assert.IsFalse(handler.Requests[1].Headers.Contains("Authorization"), "No header is sent unless one is configured.");
    }

    [TestMethod]
    public async Task Nothing_is_sent_for_a_bad_key_or_in_sandbox_mode()
    {
        var handler = new RecordingHandler();

        Assert.AreEqual("102", (await Client(handler).CheckAsync("123", InsuranceSystemType.Hidp)).ErrorCode);
        var simulated = await Client(handler, sandbox: true).CheckAsync("1234567891", InsuranceSystemType.Hidp);

        Assert.IsTrue(simulated.Simulated);
        Assert.AreEqual(1, simulated.Policies.Count);
        Assert.IsTrue((await Client(handler, sandbox: true).CheckAsync("1234567890", InsuranceSystemType.Hidp)).NotFound, "Sandbox keys ending in 0 have no policy.");
        Assert.AreEqual(0, handler.Requests.Count);
    }

    // ----- the front-office workflow (sandbox answers) -----

    private static Task<RcmTestHost> SandboxHostAsync() => RcmTestHost.CreateAsync(new() { ["Nphies:Sandbox:Enabled"] = "true" });

    [TestMethod]
    public async Task Discovery_uses_the_patients_identity_number_and_reports_no_record()
    {
        await using var host = await SandboxHostAsync();
        var seed = await host.SeedPartiesAsync(); // national id 2234567890: no record in the sandbox

        var result = await host.RunAsync<IRcmInsuranceDiscoveryService, InsuranceDiscoveryDto>(s =>
            s.DiscoverAsync(new InsuranceDiscoveryRequestDto { PatientId = seed.Patient.Id }));

        Assert.AreEqual("2234567890", result.PatientKey);
        Assert.IsFalse(result.Found);
        Assert.AreEqual("105", result.ErrorCode);
    }

    [TestMethod]
    public async Task A_discovered_policy_is_matched_to_the_payer_and_saved_as_a_coverage_once()
    {
        await using var host = await SandboxHostAsync();
        var seed = await host.SeedPartiesAsync();
        var request = new InsuranceDiscoveryRequestDto { PatientId = seed.Patient.Id, PatientKey = "1234567891" };
        Task<InsuranceDiscoveryDto> DiscoverAsync() => host.RunAsync<IRcmInsuranceDiscoveryService, InsuranceDiscoveryDto>(s => s.DiscoverAsync(request));
        Task<DiscoveredCoverageDto> AddAsync(string policyNumber) => host.RunAsync<IRcmInsuranceDiscoveryService, DiscoveredCoverageDto>(s =>
            s.AddCoverageAsync(new AddDiscoveredCoverageDto { PatientId = seed.Patient.Id, PatientKey = "1234567891", PolicyNumber = policyNumber }));

        var first = (await DiscoverAsync()).Policies.Single();
        Assert.IsNull(first.MatchedInsurerId, "No payer on file has this insurance company id yet.");
        var conflict = await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() => AddAsync(first.PolicyNumber!));
        Assert.AreEqual(RcmErrorKind.Conflict, conflict.Kind);

        await host.QueryAsync(async db =>
        {
            (await db.Organizations.SingleAsync(o => o.Id == seed.Insurer.Id)).NphiesPayerId = first.InsuranceCompanyId;
            return await db.SaveChangesAsync();
        });
        var matched = (await DiscoverAsync()).Policies.Single();
        Assert.AreEqual(seed.Insurer.Id, matched.MatchedInsurerId);
        Assert.IsNull(matched.ExistingCoverageId);

        var added = await AddAsync(matched.PolicyNumber!);
        Assert.IsTrue(added.Created);
        var coverage = await host.QueryAsync(db => db.Coverages.AsNoTracking().SingleAsync(c => c.Id == added.CoverageId));
        Assert.AreEqual(seed.Patient.Id, coverage.PatientId);
        Assert.AreEqual(seed.Insurer.Id, coverage.InsurerId);
        Assert.AreEqual(matched.PolicyNumber, coverage.PolicyNumber);
        Assert.AreEqual(matched.BeneficiaryNumber, coverage.MemberID);
        Assert.AreEqual(matched.ExpiryDate, coverage.CoverageEndDate);
        Assert.AreEqual("active", coverage.Status);

        Assert.IsFalse((await AddAsync(matched.PolicyNumber!)).Created, "The same policy is not added twice.");
        Assert.AreEqual(added.CoverageId, (await DiscoverAsync()).Policies.Single().ExistingCoverageId);
        var missing = await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() => AddAsync("NOT-A-POLICY"));
        Assert.AreEqual(RcmErrorKind.NotFound, missing.Kind);
    }

    [TestMethod]
    public async Task A_dependents_coverage_records_the_relationship_and_the_policy_holder()
    {
        await using var host = await SandboxHostAsync();
        var seed = await host.SeedPartiesAsync(); // the seeded patient plays the mother
        var child = new NPhies_FHIR_Integration.Domain.Entities.Patient
        {
            MRN = "MRN-CH-0001", NationalIdType = "NI", FirstName = "Sara", LastName = "Khaled", FullName = "Sara Khaled", Gender = "F",
            DateOfBirth = DateTime.UtcNow.Date.AddYears(-6),
        };
        await host.QueryAsync(async db => { db.Patients.Add(child); return await db.SaveChangesAsync(); });

        // Find a sandbox key whose policy lists the beneficiary as a dependent.
        string? key = null;
        DiscoveredPolicyDto? policy = null;
        for (var i = 1; i < 60 && policy is null; i++)
        {
            if (i % 10 == 0) continue;
            key = (1100000000 + i).ToString();
            var found = await host.RunAsync<IRcmInsuranceDiscoveryService, InsuranceDiscoveryDto>(s =>
                s.DiscoverAsync(new InsuranceDiscoveryRequestDto { PatientId = child.Id, PatientKey = key }));
            policy = found.Policies.SingleOrDefault(p => p.BeneficiaryType == "dependent");
        }
        Assert.IsNotNull(policy, "The sandbox should have a dependent policy among the keys tried.");

        Task<DiscoveredCoverageDto> AddAsync(string? relationship, string? holderId) => host.RunAsync<IRcmInsuranceDiscoveryService, DiscoveredCoverageDto>(s =>
            s.AddCoverageAsync(new AddDiscoveredCoverageDto
            {
                PatientId = child.Id, PatientKey = key, PolicyNumber = policy!.PolicyNumber!, InsurerId = seed.Insurer.Id,
                Relationship = relationship, SubscriberPatientId = holderId,
            }));

        await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() => AddAsync("self", seed.Patient.Id), "A dependent is never 'self'.");
        await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() => AddAsync("child", child.Id), "The holder cannot be the patient.");

        var added = await AddAsync("Child", seed.Patient.Id);

        var coverage = await host.QueryAsync(db => db.Coverages.AsNoTracking().SingleAsync(c => c.Id == added.CoverageId));
        Assert.AreEqual("child", coverage.SubscriberRelationship);
        Assert.AreEqual(seed.Patient.Id, coverage.SubscriberPatientId, "The mother is the policy holder.");
        Assert.AreEqual(child.Id, coverage.PatientId);
    }

    [TestMethod]
    public async Task A_key_is_required_and_must_fit_the_system_type()
    {
        await using var host = await SandboxHostAsync();
        Task<InsuranceDiscoveryDto> DiscoverAsync(string? key, int systemType) => host.RunAsync<IRcmInsuranceDiscoveryService, InsuranceDiscoveryDto>(s =>
            s.DiscoverAsync(new InsuranceDiscoveryRequestDto { PatientKey = key, SystemType = systemType }));

        await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() => DiscoverAsync(null, 1));
        await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() => DiscoverAsync("A1234567", 1));
        await Assert.ThrowsExceptionAsync<RcmWorkflowException>(() => DiscoverAsync("1234567891", 7));
        Assert.IsTrue((await DiscoverAsync("A1234567", 5)).Found, "A passport number works for Hajj and Umrah visitors.");
    }
}
