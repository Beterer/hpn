using FluentValidation;
using Hpn.Modules.Identity.Contracts;
using Hpn.Modules.Identity.Internal;
using Hpn.Modules.Identity.Internal.Auth;
using Hpn.Modules.Identity.Internal.Email;
using Hpn.Modules.Identity.Internal.Features.GetMe;
using Hpn.Modules.Identity.Internal.Features.Logout;
using Hpn.Modules.Identity.Internal.Features.RequestMagicLink;
using Hpn.Modules.Identity.Internal.Features.VerifyMagicLink;
using Hpn.Modules.Identity.Internal.Persistence;
using Hpn.SharedKernel.Modules;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hpn.Modules.Identity;

/// <summary>
/// Composition root for the Identity module. The host calls these two methods and
/// nothing else reaches inside (backbone §5.3, §5.4). Identity owns authentication
/// (the session cookie scheme); the host only runs the auth middleware.
/// </summary>
public static class IdentityModule
{
    public static IServiceCollection AddIdentityModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Postgres"))
                   .UseSnakeCaseNamingConvention());

        services.AddScoped<IModuleInitializer, IdentityModuleInitializer>();

        services.Configure<IdentityOptions>(configuration.GetSection(IdentityOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        services.TryAddSingleton(TimeProvider.System);

        services.AddScoped<IIdentityApi, IdentityApi>();
        services.AddScoped<SessionAuthenticator>();
        services.AddScoped<RequestMagicLinkHandler>();
        services.AddScoped<VerifyMagicLinkHandler>();
        services.AddScoped<GetMeHandler>();
        services.AddScoped<LogoutHandler>();

        services.AddValidatorsFromAssemblyContaining<RequestMagicLinkValidator>(ServiceLifetime.Scoped, includeInternalTypes: true);

        AddEmailSender(services, configuration);

        // Identity defines the auth scheme; the host runs UseAuthentication/UseAuthorization.
        services.AddAuthentication(SessionAuthenticationDefaults.Scheme)
            .AddScheme<AuthenticationSchemeOptions, SessionAuthenticationHandler>(SessionAuthenticationDefaults.Scheme, configureOptions: null);

        return services;
    }

    public static IEndpointRouteBuilder MapIdentityEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var auth = endpoints.MapGroup("/auth").WithTags("Auth");
        auth.MapRequestMagicLink();
        auth.MapVerifyMagicLink();
        auth.MapLogout();

        endpoints.MapGetMe();

        return endpoints;
    }

    private static void AddEmailSender(IServiceCollection services, IConfiguration configuration)
    {
        var provider = configuration.GetSection(EmailOptions.SectionName)["Provider"];
        if (string.Equals(provider, "resend", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IEmailSender, ResendEmailSender>((sp, client) =>
            {
                var options = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<EmailOptions>>().Value;
                client.BaseAddress = new Uri("https://api.resend.com/");
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", options.Resend.ApiKey);
            });
        }
        else
        {
            services.AddScoped<IEmailSender, SmtpEmailSender>();
        }
    }
}
