var builder = DistributedApplication.CreateBuilder(args);

var apiService = builder.AddProject<Projects.NPhies_FHIR_Integration_ApiService>("apiservice")
    .WithHttpHealthCheck("/api/v1/health");

builder.Build().Run();
