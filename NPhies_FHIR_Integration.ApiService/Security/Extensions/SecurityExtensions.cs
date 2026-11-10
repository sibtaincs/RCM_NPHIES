using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using NPhies_FHIR_Integration.ApiService.Security.Services;
using NPhies_FHIR_Integration.ApiService.Security.Middleware;
using NPhies_FHIR_Integration.ApiService.Security;

namespace NPhies_FHIR_Integration.ApiService.Security.Extensions;

/// <summary>
/// Security extension methods for dependency injection
/// </summary>
public static class SecurityExtensions
{
    /// <summary>
    /// Add comprehensive security services
    /// </summary>
    public static IServiceCollection AddComprehensiveSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Register security services
        services.AddScoped<IJwtService, JwtService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IPasswordHashingService, PasswordHashingService>();
        services.AddScoped<IUserRegistrationService, UserRegistrationService>();
        services.AddScoped<IAuditLoggingService, AuditLoggingService>();
        services.AddScoped<IRateLimitingService, RateLimitingService>();
        services.AddHttpContextAccessor();

        // Configure JWT Authentication
        ConfigureJwtAuthentication(services, configuration);

        // Configure Authorization Policies
        ConfigureAuthorizationPolicies(services);

        // Configure CORS
        ConfigureCors(services, configuration);

        return services;
    }

    /// <summary>
    /// Configure JWT authentication
    /// </summary>
    private static void ConfigureJwtAuthentication(IServiceCollection services, IConfiguration configuration)
    {
        var jwtSettings = configuration.GetSection("JwtSettings");
        var key = Encoding.ASCII.GetBytes(SecretConfiguration.GetJwtSecret(configuration));

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.RequireHttpsMetadata = !configuration.GetValue<bool>("App:IsDevelopment");
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidIssuer = jwtSettings["Issuer"] ?? "NPhiesIssuer",
                ValidateAudience = true,
                ValidAudience = jwtSettings["Audience"] ?? "NPhiesAudience",
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromSeconds(10),
                RoleClaimType = System.Security.Claims.ClaimTypes.Role
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = context =>
            {
                // Additional token validation logic can be added here
                return Task.CompletedTask;
            },
                OnAuthenticationFailed = context =>
             {
                 // Log authentication failures
                 return Task.CompletedTask;
             }
            };
        });
    }

    /// <summary>
    /// Configure authorization policies
    /// </summary>
    private static void ConfigureAuthorizationPolicies(IServiceCollection services)
    {
        services.AddAuthorization(options =>
             {
                 // Role-based policies
                 options.AddPolicy("AdminOnly", policy =>
             policy.RequireRole(SecurityConstants.ADMIN_ROLE));

                 options.AddPolicy("RCMProcessor", policy =>
                      policy.Requirements.Add(new RoleRequirement(SecurityConstants.ADMIN_ROLE, SecurityConstants.RCM_PROCESSOR_ROLE)));

                 options.AddPolicy("RCMViewer", policy =>
               policy.Requirements.Add(new RoleRequirement(SecurityConstants.ADMIN_ROLE, SecurityConstants.RCM_PROCESSOR_ROLE, SecurityConstants.RCM_VIEWER_ROLE)));

                 options.AddPolicy("ClaimsProcessor", policy =>
               policy.Requirements.Add(new RoleRequirement(SecurityConstants.ADMIN_ROLE, SecurityConstants.CLAIMS_PROCESSOR_ROLE)));

                 options.AddPolicy("ClaimsReviewer", policy =>
                policy.Requirements.Add(new RoleRequirement(SecurityConstants.ADMIN_ROLE, SecurityConstants.CLAIMS_REVIEWER_ROLE)));

                 // Compliance policies
                 options.AddPolicy("ComplianceOfficer", policy =>
             policy.RequireRole(SecurityConstants.COMPLIANCE_OFFICER_ROLE, SecurityConstants.ADMIN_ROLE));

                 options.AddPolicy("Auditor", policy =>
            policy.RequireRole(SecurityConstants.AUDITOR_ROLE, SecurityConstants.ADMIN_ROLE));
             });
    }

    /// <summary>
    /// Configure CORS
    /// </summary>
    private static void ConfigureCors(IServiceCollection services, IConfiguration configuration)
    {
        services.AddCors(options =>
           {
               if (configuration.GetValue<bool>("App:IsDevelopment"))
               {
                   // Development CORS policy
                   options.AddPolicy("AllowDevelopment", policy =>
                            {
                       policy.AllowAnyOrigin()
                                 .AllowAnyMethod()
                            .AllowAnyHeader();
                   });
               }
               else
               {
                   // Production CORS policy
                   options.AddPolicy("AllowSpecified", policy =>
            {
        var origins = configuration.GetSection("SecuritySettings:Cors:AllowedOrigins")
         .Get<string[]>() ?? new[] { "https://yourdomain.com" };

        policy.WithOrigins(origins)
 .AllowAnyMethod()
.AllowAnyHeader()
.AllowCredentials();
    });
               }
           });
    }

    /// <summary>
    /// Use security middleware
    /// </summary>
    public static IApplicationBuilder UseComprehensiveSecurity(this IApplicationBuilder app, IConfiguration configuration)
    {
        // Add rate limiting middleware
        app.UseRateLimiting();

        // Add audit logging middleware
        app.UseAuditLogging();

        // Add HTTPS redirection
        if (!configuration.GetValue<bool>("App:IsDevelopment"))
            app.UseHttpsRedirection();

        // Use authentication and authorization
        app.UseAuthentication();
        app.UseAuthorization();

        // Use CORS
        var corsPolicyName = configuration.GetValue<bool>("App:IsDevelopment") ? "AllowDevelopment" : "AllowSpecified";
        app.UseCors(corsPolicyName);

        return app;
    }
}

/// <summary>
/// Role requirement for authorization
/// </summary>
public class RoleRequirement : Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
{
    public string[] AllowedRoles { get; }

    public RoleRequirement(params string[] roles)
    {
        AllowedRoles = roles;
    }
}
