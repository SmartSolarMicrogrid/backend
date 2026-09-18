using System.Text;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using SmartSolarMicrogrid.API.Auth;
using SmartSolarMicrogrid.API.Data;
using SmartSolarMicrogrid.API.Repositories;
using SmartSolarMicrogrid.API.Repositories.Interfaces;
using SmartSolarMicrogrid.API.Services;
using SmartSolarMicrogrid.API.Services.Interfaces;
using SmartSolarMicrogrid.API.Validators;

namespace SmartSolarMicrogrid.API.Configuration;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // ── MongoDB ─────────────────────────────────────────────────
        var mongoSettings = configuration
            .GetSection("MongoDbSettings")
            .Get<MongoDbSettings>()!;

        services.AddSingleton(mongoSettings);
        services.AddSingleton<MongoDbContext>();
        services.AddSingleton<ITransactionRunner, MongoTransactionRunner>();
        services.AddSingleton<IndexInitializer>();
        services.AddHealthChecks().AddCheck<MongoHealthCheck>("mongodb");

        // ── JWT ──────────────────────────────────────────────────────
        var jwtSettings = configuration
            .GetSection("JwtSettings")
            .Get<JwtSettings>()!;

        services.AddSingleton(jwtSettings);
        services.AddSingleton<JwtTokenGenerator>();

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwtSettings.Issuer,
                    ValidAudience            = jwtSettings.Audience,
                    IssuerSigningKey         = new SymmetricSecurityKey(
                                                  Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                };
            });

        services.AddAuthorization();

        // ── Rate Limiting ────────────────────────────────────────────
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.AddPolicy("login", httpContext =>
                System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1)
                    }));
        });

        // ── Repositories ─────────────────────────────────────────────
        services.AddScoped<IUserRepository,        UserRepository>();
        services.AddScoped<IProsumerRepository,    ProsumerRepository>();
        services.AddScoped<INodeRepository,        NodeRepository>();
        services.AddScoped<ISlotRepository,        SlotRepository>();
        services.AddScoped<IReservationRepository, ReservationRepository>();

        // ── Policies & Strategies ────────────────────────────────────
        services.AddSingleton<Services.Policies.ReservationPolicy>();
        services.AddSingleton<Services.Pricing.ITradePricing, Services.Pricing.ExportPricing>();
        services.AddSingleton<Services.Pricing.ITradePricing, Services.Pricing.ImportPricing>();

        // ── Services ─────────────────────────────────────────────────
        services.AddScoped<IAuthService,        AuthService>();
        services.AddScoped<IUserService,        UserService>();
        services.AddScoped<IProsumerService,    ProsumerService>();
        services.AddScoped<INodeService,        NodeService>();
        services.AddScoped<ISlotService,        SlotService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddSingleton<QrService>();
        services.AddScoped<ITransferService,    TransferService>();
        services.AddScoped<IDashboardService,   DashboardService>();
        services.AddSingleton<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, NodeScopeHandler>();

        // ── Background Workers ───────────────────────────────────────
        services.AddHostedService<Workers.SlotGenerationWorker>();
        services.AddHostedService<Workers.ReservationExpiryWorker>();

        // ── FluentValidation ─────────────────────────────────────────
        services.AddFluentValidationAutoValidation();
        services.AddValidatorsFromAssemblyContaining<LoginRequestValidator>();

        // ── CORS ─────────────────────────────────────────────────────
        services.AddCors(options =>
        {
            options.AddPolicy("AllowWebApp", policy =>
                policy.WithOrigins("http://localhost:3000", "http://localhost:5173")
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

        return services;
    }
}
