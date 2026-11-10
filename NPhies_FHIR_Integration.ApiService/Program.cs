using NPhies_FHIR_Integration.Common.Constants;
using NPhies_FHIR_Integration.Infrastructure.Repositories;
using NPhies_FHIR_Integration.Domain.Interfaces;
using NPhies_FHIR_Integration.Application.Services;
using NPhies_FHIR_Integration.Application.Services.MasterDataServices;
using NPhies_FHIR_Integration.Application.Mapping;
using NPhies_FHIR_Integration.ApiService.Security.Extensions;
using NPhies_FHIR_Integration.ApiService.Security.Services;
using NPhies_FHIR_Integration.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NPhies_FHIR_Integration.Application.Services.Masters;
using NPhies_FHIR_Integration.Domain.CodeableConcept.Services;

using NPhies_FHIR_Integration.Application.Extensions;
using NPhies_FHIR_Integration.ApiService.Middleware;
using NPhies_FHIR_Integration.Application.Services.Gateway;
using NPhies_FHIR_Integration.Application.Services.Batch;
using NPhies_FHIR_Integration.Application.Services.Caching;
using NPhies_FHIR_Integration.Application.Services.Events;
using NPhies_FHIR_Integration.Application.Services.RCM;
using NPhies_FHIR_Integration.Application.Rcm;
using NPhies_FHIR_Integration.Infrastructure.Data.Configurations;
using NPhies_FHIR_Integration.Application.Services.Diagnostics;

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

// Add Distributed Cache (Memory Cache for development, Redis for production)
if (builder.Environment.IsProduction())
{
    // Uncomment when Redis is configured
    // builder.Services.AddStackExchangeRedisCache(options =>
    // {
    //     options.Configuration = builder.Configuration.GetConnectionString("Redis");
    //     options.InstanceName = "NPhies_";
    // });
    builder.Services.AddDistributedMemoryCache(); // Fallback for now
}
else
{
    builder.Services.AddDistributedMemoryCache(); // For development
}

// Add AutoMapper
builder.Services.AddAutoMapper(
    typeof(ApplicationMappingProfile),
    typeof(EligibilityMappingProfile),
    typeof(MasterDataMappingProfile));

// 🔒 ADD COMPREHENSIVE SECURITY SYSTEM
builder.Services.AddComprehensiveSecurity(builder.Configuration);

// 🚀 ADD NPHIES INTEGRATION SERVICES (Week 2 - Day 4)
builder.Services.AddNphiesIntegration(builder.Configuration);

// 🔍 ADD NPHIES DIAGNOSTIC SERVICE
builder.Services.AddScoped<INphiesDiagnosticService, NphiesDiagnosticService>();

// 🔐 ACCESS CONTROL — data-driven roles & permissions
builder.Services.AddScoped<NPhies_FHIR_Integration.ApiService.Security.Services.IPermissionResolver,
    NPhies_FHIR_Integration.ApiService.Security.Services.PermissionResolver>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationPolicyProvider,
    NPhies_FHIR_Integration.ApiService.Security.Authorization.PermissionPolicyProvider>();
builder.Services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler,
    NPhies_FHIR_Integration.ApiService.Security.Authorization.PermissionAuthorizationHandler>();


// Add controllers
builder.Services.AddControllers();

// ========== INFRASTRUCTURE SERVICES ==========
// ✅ API Gateway Service - Rate limiting, authentication, logging
builder.Services.AddScoped<IApiGatewayService, ApiGatewayService>();

// ✅ Batch Processing Service - Bulk processing of claims, appeals, reports


// ✅ Caching Service - Distributed caching for performance
builder.Services.AddScoped<ICachingService, CachingService>();

// ✅ Event Publisher - Domain events throughout the system
builder.Services.AddScoped<IEventPublisher, EventPublisher>();

// ✅ Webhook Service - Webhook subscriptions and deliveries
builder.Services.AddScoped<IWebhookService, WebhookService>();

// ✅ Notification Service - User notifications
builder.Services.AddScoped<INotificationService, NotificationService>();

// Register generic repository
builder.Services.AddScoped(typeof(IRepository<>), typeof(Repository<>));

// Register specialized repositories
builder.Services.AddScoped<ICoverageEligibilityRequestRepository, CoverageEligibilityRequestRepository>();
builder.Services.AddScoped<ICoverageEligibilityResponseRepository, CoverageEligibilityResponseRepository>();
builder.Services.AddScoped<IPatientRepository, PatientRepository>();
builder.Services.AddScoped<ICoverageRepository, CoverageRepository>();
builder.Services.AddScoped<IOrganizationRepository, OrganizationRepository>();
builder.Services.AddScoped<IBenefitBalanceRepository, BenefitBalanceRepository>();
builder.Services.AddScoped<IEligibilityErrorRepository, EligibilityErrorRepository>();
// Register Claim repositories
builder.Services.AddScoped<IClaimRepository, ClaimRepository>();
builder.Services.AddScoped<IClaimItemRepository, ClaimItemRepository>();
builder.Services.AddScoped<IClaimDiagnosisRepository, ClaimDiagnosisRepository>();
builder.Services.AddScoped<IClaimResponseRepository, ClaimResponseRepository>();

// Phase 2: Register Adjudication and Rejection repositories
builder.Services.AddScoped<IAdjudicationDetailRepository, AdjudicationDetailRepository>();
builder.Services.AddScoped<IRejectionReasonRepository, RejectionReasonRepository>();

// Register services
builder.Services.AddScoped<IEligibilityService, EligibilityService>();
builder.Services.AddScoped<IFhirToEntityMapper, FhirToEntityMapper>();

// Register Claim services
builder.Services.AddScoped<IClaimService, ClaimService>();
builder.Services.AddScoped<IClaimItemService, ClaimItemService>();
builder.Services.AddScoped<IClaimDiagnosisService, ClaimDiagnosisService>();
builder.Services.AddScoped<IClaimResponseService, ClaimResponseService>();

// Phase 2: Register Payment Calculation Engine
builder.Services.AddScoped<IPaymentCalculationEngine, PaymentCalculationEngine>();

// Phase 2: Register Payment Service
builder.Services.AddScoped<IPaymentService, PaymentService>();

// 📚 MASTER DATA SERVICES - Phase 3
builder.Services.AddScoped<IServiceCodeMasterService, ServiceCodeMasterService>();
builder.Services.AddScoped<IMedicationCodeMasterService, MedicationCodeMasterService>();
builder.Services.AddScoped<IMedicalDeviceCodeMasterService, MedicalDeviceCodeMasterService>();
builder.Services.AddScoped<IDiagnosisCodeMasterService, DiagnosisCodeMasterService>();
builder.Services.AddScoped<IModifierCodeMasterService, ModifierCodeMasterService>();
builder.Services.AddScoped<IBenefitCodeMasterService, BenefitCodeMasterService>();
builder.Services.AddScoped<INphiesCodeMappingService, NphiesCodeMappingService>();
builder.Services.AddScoped<IPayerMasterService, PayerMasterService>();
builder.Services.AddScoped<IPayerPolicyMasterService, PayerPolicyMasterService>();
builder.Services.AddScoped<IPolicyBenefitCoverageService, PolicyBenefitCoverageService>();
builder.Services.AddScoped<IClinicMasterService, ClinicMasterService>();
builder.Services.AddScoped<IDoctorMasterService, DoctorMasterService>();
builder.Services.AddScoped<IDoctorQualificationService, DoctorQualificationService>();
builder.Services.AddScoped<IClaimSubmissionRulesService, ClaimSubmissionRulesService>();

// Register Error Code Service for NPHIES error code management
builder.Services.AddScoped<IErrorCodeService, ErrorCodeService>();

// ✅ CODEABLE CONCEPT SERVICE - Phase 4
builder.Services.AddScoped<ICodeableConceptService, CodeableConceptService>();

// ✅ RCM SERVICES
builder.Services.AddScoped<NPhies_FHIR_Integration.Application.Services.RCM.Rules.AdjudicationRuleEngine>();
builder.Services.AddScoped<IRCMService, RCMService>();

// ✅ NPHIES RCM WORKFLOWS (eligibility, prior auth, claims, batch, poll, communications, payments, denials, appeals, reports)
builder.Services.AddRcmWorkflows(builder.Configuration);
builder.Services.AddHostedService<NPhies_FHIR_Integration.ApiService.BackgroundServices.NphiesPollWorker>();

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseExceptionHandler();

// 🔒 USE COMPREHENSIVE SECURITY SYSTEM
app.UseComprehensiveSecurity(builder.Configuration);

// 🌐 ENABLE CORS for the React dev client (must precede endpoint mapping)
app.UseCors(ReactDevCors);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    // Apply migrations and seed database in development
    using (var scope = app.Services.CreateScope())
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        try
        {
            // ✅ STEP 1: Apply pending migrations
            logger.LogInformation("🔍 Checking database connection and pending migrations...");
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            // Test connection first
            var canConnect = await context.Database.CanConnectAsync();
            if (!canConnect)
            {
                logger.LogError("❌ Cannot connect to database. Please check:");
                logger.LogError("   1. SQL Server is running");
                logger.LogError("   2. Connection string is correct");
                logger.LogError("   3. Database permissions are set");
                throw new Exception("Database connection failed");
            }

            logger.LogInformation("✅ Database connection successful");

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
