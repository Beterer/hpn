using Hpn.Api.Infrastructure;
using Hpn.Modules.Admin;
using Hpn.Modules.Appreciation;
using Hpn.Modules.Feed;
using Hpn.Modules.Identity;
using Hpn.Modules.Moderation;
using Hpn.Modules.Photo;
using Hpn.Modules.Profile;
using Hpn.Modules.SocialFingerprint;
using System.Threading.RateLimiting;
using Hpn.SharedKernel;
using Hpn.SharedKernel.Auth;
using Hpn.SharedKernel.Events;
using Hpn.SharedKernel.RateLimiting;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
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

    // We sit behind a TLS-terminating reverse proxy (Caddy, §3.5), so trust its
    // X-Forwarded-* to recover the real client IP + scheme. Without this, the
    // per-IP rate limiter and stored request IPs would all see the proxy. The
    // app is only reachable via that single proxy hop, so we trust one hop and
    // don't pin a (dynamic, containerized) proxy address.
    services.Configure<ForwardedHeadersOptions>(options =>
    {
        options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        options.ForwardLimit = 1;
        options.KnownIPNetworks.Clear();
        options.KnownProxies.Clear();
    });

    // Shared auth plumbing: the Identity module registers the session scheme;
    // the host exposes the current principal to every module (backbone §11).
    services.AddHttpContextAccessor();
    services.AddScoped<ICurrentUser, HttpContextCurrentUser>();
    services.AddAuthorization();

    services.AddRateLimiter(options =>
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

        // Magic-link is partitioned per client IP (tight); per-email volume is
        // additionally capped inside the handler (backbone §10.6).
        options.AddPolicy(RateLimitPolicies.MagicLink, httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 5,
                    Window = TimeSpan.FromMinutes(15),
                }));
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

    // Recover the real client IP/scheme before anything reads them (logging,
    // rate limiting, cookies).
    app.UseForwardedHeaders();
    app.UseSerilogRequestLogging();
    app.UseExceptionHandler();
    app.UseStatusCodePages();
    app.UseAuthentication();
    app.UseAuthorization();
    // After auth so per-user policies (appreciation/reports, M5/M9) can partition
    // on the authenticated identity; the anonymous magic-link policy is unaffected.
    app.UseRateLimiter();

    app.MapOpenApi();

    // Liveness: process is up. Readiness: dependencies (Postgres) are reachable.
    app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = registration => registration.Tags.Contains("ready"),
    });

    var api = app.MapGroup(ApiRoutes.Prefix);
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
