using Hpn.Api.Infrastructure;
using Hpn.Modules.Admin;
using Hpn.Modules.Appreciation;
using Hpn.Modules.Feed;
using Hpn.Modules.Identity;
using Hpn.Modules.Moderation;
using Hpn.Modules.Photo;
using Hpn.Modules.Profile;
using Hpn.Modules.SocialFingerprint;
using Hpn.SharedKernel.Events;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.RateLimiting;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, loggerConfiguration) => loggerConfiguration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console());

    var services = builder.Services;
    var configuration = builder.Configuration;

    // ---- Shared cross-cutting middleware (host concern only — no business logic) ----
    services.AddProblemDetails();
    services.AddOpenApi();
    services.AddDomainEventDispatcher();

    services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddFixedWindowLimiter(RateLimitPolicies.MagicLink, o =>
        {
            o.PermitLimit = 5;
            o.Window = TimeSpan.FromMinutes(15);
        });
        options.AddFixedWindowLimiter(RateLimitPolicies.Appreciation, o =>
        {
            o.PermitLimit = 60;
            o.Window = TimeSpan.FromMinutes(1);
        });
        options.AddFixedWindowLimiter(RateLimitPolicies.Reports, o =>
        {
            o.PermitLimit = 10;
            o.Window = TimeSpan.FromHours(1);
        });
        options.AddFixedWindowLimiter(RateLimitPolicies.Uploads, o =>
        {
            o.PermitLimit = 20;
            o.Window = TimeSpan.FromMinutes(10);
        });
    });

    services.AddHealthChecks()
        .AddCheck<PostgresHealthCheck>("postgres", tags: ["ready"]);

    var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];
    services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("hpn-api"))
        .WithTracing(tracing =>
        {
            tracing.AddAspNetCoreInstrumentation();
            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                tracing.AddOtlpExporter();
            }
        })
        .WithMetrics(metrics =>
        {
            metrics.AddAspNetCoreInstrumentation().AddRuntimeInstrumentation();
            if (!string.IsNullOrWhiteSpace(otlpEndpoint))
            {
                metrics.AddOtlpExporter();
            }
        });

    // ---- Modules: the host only wires them; it never reaches inside ----
    services.AddIdentityModule(configuration);
    services.AddProfileModule(configuration);
    services.AddPhotoModule(configuration);
    services.AddAppreciationModule(configuration);
    services.AddFeedModule(configuration);
    services.AddSocialFingerprintModule(configuration);
    services.AddModerationModule(configuration);
    services.AddAdminModule(configuration);

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseRateLimiter();

    app.MapOpenApi();

    // Liveness: process is up. Readiness: dependencies (Postgres) are reachable.
    app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready"),
    });

    var api = app.MapGroup("/api/v1");
    api.MapIdentityEndpoints();
    api.MapProfileEndpoints();
    api.MapPhotoEndpoints();
    api.MapAppreciationEndpoints();
    api.MapFeedEndpoints();
    api.MapSocialFingerprintEndpoints();
    api.MapModerationEndpoints();
    api.MapAdminEndpoints();

    if (app.Environment.IsDevelopment())
    {
        // Migrations auto-apply in dev only; prod runs them as a gated deploy step.
        await app.InitializeModulesAsync();
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "HPN API host terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}
