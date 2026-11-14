using NPhies_FHIR_Integration.Infrastructure.Repositories;
using NPhies_FHIR_Integration.Domain.Interfaces;
using NPhies_FHIR_Integration.Application.Services;
using NPhies_FHIR_Integration.Application.Mapping;
using NPhies_FHIR_Integration.ApiService.Security.Extensions;
using NPhies_FHIR_Integration.ApiService.Security.Services;
using NPhies_FHIR_Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Application.Extensions;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;

var builder = WebApplication.CreateBuilder(args);
NPhies_FHIR_Integration.ApiService.Security.SecretConfiguration.EnsureSecrets(builder.Configuration, builder.Environment);

// Add service defaults & Aspire client integrations.
builder.AddServiceDefaults();

// Add services to the container.
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();

// Add DbContext - Use SQL Server with default connection string
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// 🌐 CORS for the React dev client (Vite dev server on :5173)
const string ReactDevCors = "ReactDevCors";
builder.Services.AddCors(options =>
{
    options.AddPolicy(ReactDevCors, policy =>
    {
        policy.WithOrigins("http://localhost:5173", "https://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// In-memory distributed cache until Redis is introduced (roadmap: operations phase).
builder.Services.AddDistributedMemoryCache();

builder.Services.AddAutoMapper(typeof(ApplicationMappingProfile));

// 🔒 Authentication, authorization, rate limiting and audit logging
builder.Services.AddComprehensiveSecurity(builder.Configuration);

// 🚀 NPHIES messaging core (builder, validator, mTLS transport, audit) and health checks
builder.Services.AddNphiesIntegration(builder.Configuration);

// 🔐 ACCESS CONTROL — data-driven roles & permissions
builder.Services.AddScoped<NPhies_FHIR_Integration.ApiService.Security.Services.IPermissionResolver,
    NPhies_FHIR_Integration.ApiService.Security.Services.PermissionResolver>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider,
    NPhies_FHIR_Integration.ApiService.Security.Authorization.PermissionPolicyProvider>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
    NPhies_FHIR_Integration.ApiService.Security.Authorization.PermissionAuthorizationHandler>();

builder.Services.AddControllers();

builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Claim review gate (technical and medical review) used by ClaimsController
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<IClaimService, ClaimService>();

// ✅ NPHIES RCM WORKFLOWS (eligibility, prior auth, claims, batch, poll, communications, payments, denials, appeals, reports)
builder.Services.AddRcmWorkflows(builder.Configuration);
builder.Services.AddHostedService<NPhies_FHIR_Integration.ApiService.BackgroundServices.NphiesPollWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// 🌐 ENABLE CORS for the React dev client (must precede authentication/authorization so preflights aren't rejected)
app.UseCors(ReactDevCors);

// 🔒 USE COMPREHENSIVE SECURITY SYSTEM
app.UseComprehensiveSecurity(builder.Configuration);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();

    // Apply migrations and seed database in development
    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // ✅ STEP 1: Apply pending migrations
            logger.LogInformation("🔍 Checking database connection and pending migrations...");
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // MigrateAsync creates the database when it does not exist yet, so there is no
            // CanConnectAsync() gate here: that returns false for a missing database and used
            // to skip migrations entirely. A server that is truly unreachable throws below.
            var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                logger.LogInformation("⏳ Applying {Count} pending migrations: {Migrations}",
                    pendingMigrations.Count(),
                 string.Join(", ", pendingMigrations));
                await context.Database.MigrateAsync();
                logger.LogInformation("✅ Migrations applied successfully!");
            }
            else
            {
                logger.LogInformation("✅ Database is up to date. No pending migrations.");
            }

            // ✅ STEP 2: Seed data
            logger.LogInformation("🌱 Starting database seeding...");
            logger.LogInformation("═══════════════════════════════════════════════════");

            await NPhies_FHIR_Integration.Infrastructure.Data.Seeders.DatabaseSeeder.SeedAsync(context, logger);

            if (builder.Environment.IsDevelopment())
            {
                await NPhies_FHIR_Integration.Infrastructure.Data.Seeders.DevelopmentClaimSeeder.SeedAsync(
                    context, logger);
                await NPhies_FHIR_Integration.Infrastructure.Data.Seeders.SampleDataSeeder.SeedAsync(context, logger);
                if (builder.Configuration.GetValue("Nphies:Sandbox:SeedActivity", true))
                    await NPhies_FHIR_Integration.Application.Nphies.Sandbox.RcmSampleActivitySeeder.SeedAsync(app.Services, logger);
            }

            // 👤 STEP 3: Seed the bootstrap administrator (idempotent)
            var passwordHasher = scope.ServiceProvider
        .GetRequiredService<NPhies_FHIR_Integration.ApiService.Security.Services.IPasswordHashingService>();
   await NPhies_FHIR_Integration.ApiService.Security.AdminUserSeeder.SeedAsync(
    context, passwordHasher, builder.Configuration, logger);

// 🔐 STEP 4: Seed roles, permissions and default grants (idempotent)
     await NPhies_FHIR_Integration.ApiService.Security.AccessControlSeeder.SeedAsync(context, logger);

         logger.LogInformation("═══════════════════════════════════════════════════");
            logger.LogInformation("✅ All database seeding completed successfully!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "❌ An error occurred during migration or seeding");
            logger.LogError("Error Type: {Type}", ex.GetType().Name);
            logger.LogError("Error Message: {Message}", ex.Message);

            if (ex.InnerException != null)
            {
                logger.LogError("Inner Error: {InnerMessage}", ex.InnerException.Message);
            }

            // Don't throw - allow app to start even if seeding fails
            logger.LogWarning("⚠️ Application will continue, but seeding may be incomplete");
            logger.LogWarning("⚠️ Please check the logs above for specific error details");
        }
    }
}

app.MapControllers();
app.MapDefaultEndpoints();

app.Run();

/// <summary>Exposed so integration tests can host the API with WebApplicationFactory.</summary>
public partial class Program { }
