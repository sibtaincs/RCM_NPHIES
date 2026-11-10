using System.Net;
using Hl7.Fhir.Model;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NPhies_FHIR_Integration.Application.Exceptions;
using NPhies_FHIR_Integration.Application.Nphies.Messaging;
using NPhies_FHIR_Integration.Application.Nphies.Parsing;
using NPhies_FHIR_Integration.Application.Nphies.Validation;
using NPhies_FHIR_Integration.Domain.Entities;
using NPhies_FHIR_Integration.Infrastructure.Configuration;
using NPhies_FHIR_Integration.Infrastructure.Nphies;

namespace NPhies_FHIR_Integration.Tests.Nphies;

[TestClass]
public class NphiesMessagingClientTests
{
    private sealed class FakeTransport : INphiesTransport
    {
        public Func<NphiesTransportRequest, NphiesTransportResponse>? Respond { get; set; }
        public List<NphiesTransportRequest> Requests { get; } = new();

        public Task<NphiesTransportResponse> SendAsync(NphiesTransportRequest request, CancellationToken cancellationToken = default)
        {
            Requests.Add(request);
            return System.Threading.Tasks.Task.FromResult(Respond!(request));
        }
    }

    private sealed class MemoryAudit : INphiesMessageAuditStore
    {
        public List<NphiesMessageLog> Logs { get; } = new();

        public System.Threading.Tasks.Task SaveAsync(NphiesMessageLog log, CancellationToken cancellationToken = default)
        {
            Logs.Add(log);
            return System.Threading.Tasks.Task.CompletedTask;
        }
    }

    private readonly FakeTransport _transport = new();
    private readonly MemoryAudit _audit = new();

    private NphiesMessagingClient CreateClient() => new(
        _transport,
        new NphiesBundleValidator(),
        new NphiesResponseParser(),
        _audit,
        Options.Create(new NphiesConfiguration()),
        NullLogger<NphiesMessagingClient>.Instance);

    [TestMethod]
    public async System.Threading.Tasks.Task Sends_claim_and_correlates_response_with_audit()
    {
        var request = DocsExamples.LoadBundle("7a - Claim Dental Request.json");
        var responseJson = DocsExamples.ReadJson("7b - Claim Response.json");
        _transport.Respond = _ => new NphiesTransportResponse(HttpStatusCode.OK, responseJson, "https://hsb.nphies.sa/$process-message", TimeSpan.FromMilliseconds(42));

        var result = await CreateClient().SendAsync(request, new NphiesSendOptions { CorrelationId = "claim-7a" });

        var sent = XAssert.Single(_transport.Requests);
        Assert.AreEqual("claim-request", sent.EventCode);
        Assert.IsFalse(sent.IsIdempotent);
        Assert.AreEqual(request.Entry[0].Resource.Id, sent.MessageId);
        Assert.AreEqual("claim-response", result.Response.EventCode);
        Assert.AreEqual(HttpStatusCode.OK, result.HttpStatus);

        var log = XAssert.Single(_audit.Logs);
        Assert.AreEqual("claim-7a", log.CorrelationId);
        Assert.AreEqual(sent.Json, log.RequestJson);
        Assert.AreEqual(responseJson, log.ResponseJson);
        Assert.AreEqual("claim-response", log.ResponseEventCode);
    }

    [TestMethod]
    public async System.Threading.Tasks.Task Status_check_is_marked_idempotent()
    {
        var request = DocsExamples.LoadBundle("9b - StatusCheck - Request.json");
        var responseJson = DocsExamples.ReadJson("9c - StatusCheck - Response (Do Not Process).json");
        _transport.Respond = _ => new NphiesTransportResponse(HttpStatusCode.OK, responseJson, "x", TimeSpan.Zero);

        await CreateClient().SendAsync(request);

        Assert.IsTrue(XAssert.Single(_transport.Requests).IsIdempotent);
    }

    [TestMethod]
    public async System.Threading.Tasks.Task Invalid_message_is_not_sent()
    {
        var request = DocsExamples.LoadBundle("7a - Claim Dental Request.json");
        request.Entry.RemoveAt(0);
        request.Entry.Insert(0, new Bundle.EntryComponent { FullUrl = "urn:uuid:x", Resource = new Hl7.Fhir.Model.MessageHeader { Id = "x", Event = new Coding("http://nphies.sa/terminology/CodeSystem/ksa-message-events", "claim-request") } });

        await XAssert.ThrowsAsync<NphiesValidationException>(() => CreateClient().SendAsync(request));
        XAssert.Empty(_transport.Requests);
    }

    [TestMethod]
    public async System.Threading.Tasks.Task Timeout_is_mapped_and_audited()
    {
        var request = DocsExamples.LoadBundle("7a - Claim Dental Request.json");
        _transport.Respond = _ => throw new NphiesTransportException("timed out", isTimeout: true, requestMayHaveBeenDelivered: true);

        await XAssert.ThrowsAsync<NphiesTimeoutException>(() => CreateClient().SendAsync(request));
        Assert.AreEqual("timed out", XAssert.Single(_audit.Logs).Error);
    }

    [TestMethod]
    public async System.Threading.Tasks.Task Empty_response_body_yields_transport_error()
    {
        var request = DocsExamples.LoadBundle("7a - Claim Dental Request.json");
        _transport.Respond = _ => new NphiesTransportResponse(HttpStatusCode.BadGateway, string.Empty, "x", TimeSpan.Zero);

        var result = await CreateClient().SendAsync(request);

        var error = XAssert.Single(result.Response.Errors);
        Assert.AreEqual("502", error.Code);
        Assert.AreEqual(NphiesErrorSource.Transport, error.Source);
    }
}
