using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Localization;
using Vixly.Platform.Application.DependencyInjection;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Infrastructure.DependencyInjection;
using Vixly.Platform.Web.Components;
using Vixly.Platform.Web.Endpoints;
using Vixly.Platform.Web.Security;
using Vixly.Shared.Hosting;
using Vixly.Shared.Localization;

namespace Vixly.Platform.Web.Hosting;

// =========================================================
// FILE: PlatformWebHost.cs
//
// PURPOSE:
// Builds the Platform console host: services and pipeline.
//
// RESPONSIBILITIES:
// - Register Application, Infrastructure, localization
//   (en, fr, es), session authentication, rate limits,
//   antiforgery and Blazor
// - Order the middleware: forwarded headers, security
//   headers, authentication, localization (the signed-in
//   user's language first), rate limiting, antiforgery,
//   endpoints
//
// IMPORTANT NOTES:
// - Composition root: the only Web file that touches
//   Infrastructure (Global Standard 3)
// - Data Protection application name differs from V3's
//   (ADR-0010 M-1)
// - configure runs last, before Build: the HTTP tests use it
//   to swap in a test server; production passes nothing
// =========================================================
public static class PlatformWebHost
{
    public static WebApplication Build(string[] args)
    {
        return Build(args, null);
    }

    public static WebApplication Build(string[] args, Action<WebApplicationBuilder>? configure)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddPlatformInfrastructure(builder.Configuration, "Vixly.V4.Platform.Web");
        builder.Services.AddPlatformApplication();
        builder.Services.AddVixlyForwardedHeaders(builder.Configuration);

        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<PlatformInteractiveRequest>();
        builder.Services.AddScoped<IPlatformRequestMetadata, PlatformHttpRequestMetadata>();

        builder.Services.AddAuthentication(PlatformSessionAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, PlatformSessionAuthenticationHandler>(
                PlatformSessionAuthenticationHandler.SchemeName,
                _ => { });
        builder.Services.AddAuthorization();
        builder.Services.AddCascadingAuthenticationState();
        builder.Services.AddScoped<AuthenticationStateProvider, PlatformRevalidatingAuthenticationStateProvider>();
        builder.Services.AddScoped<PlatformCurrentUser>();
        builder.Services.AddScoped<PlatformErrorText>();
        builder.Services.AddPlatformPresenters();
        builder.Services.AddHostedService<PlatformSystemRoleStartup>();
        builder.Services.AddOptions<PlatformOutboxOptions>().Bind(builder.Configuration.GetSection(PlatformOutboxOptions.SectionName));
        builder.Services.AddHostedService<PlatformOutboxWorker>();
        builder.Services.AddOptions<PlatformSubscriptionClockOptions>().Bind(builder.Configuration.GetSection(PlatformSubscriptionClockOptions.SectionName));
        builder.Services.AddHostedService<PlatformSubscriptionClockWorker>();

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddOptions<PlatformDemoDataOptions>().Bind(builder.Configuration.GetSection(PlatformDemoDataOptions.SectionName));
            builder.Services.AddHostedService<PlatformDemoDataWorker>();
        }

        builder.Services.AddPlatformRateLimits();
        builder.Services.AddAntiforgery(options =>
        {
            options.Cookie.Name = PlatformCookies.Antiforgery;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });

        builder.Services.AddRazorComponents().AddInteractiveServerComponents();

        configure?.Invoke(builder);

        WebApplication app = builder.Build();

        app.UseForwardedHeaders();
        app.UseVixlySecurityHeaders();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error", createScopeForErrors: true);
        }

        app.UseHttpsRedirection();
        app.UseAuthentication();
        app.UseRequestLocalization(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(VixlyCultures.Default);
            options.SupportedCultures = VixlyCultures.Supported.ToList();
            options.SupportedUICultures = VixlyCultures.Supported.ToList();
            options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider { CookieName = PlatformCookies.Culture });
            options.RequestCultureProviders.Insert(0, new PlatformAccountCultureProvider());
        });

        app.UseAuthorization();
        app.UseRateLimiter();
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapVixlyHealth();
        app.MapPlatformAuthEndpoints();
        app.MapPlatformBillingEndpoints();
        app.MapPlatformLegalEndpoints();
        app.MapRazorComponents<App>().AddInteractiveServerRenderMode();

        return app;
    }
}
