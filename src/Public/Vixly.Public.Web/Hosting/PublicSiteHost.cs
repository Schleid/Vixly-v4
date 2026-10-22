using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Vixly.Public.Web.Components;
using Vixly.Public.Web.Endpoints;
using Vixly.Public.Web.Platform;
using Vixly.Shared.Hosting;
using Vixly.Shared.Localization;

namespace Vixly.Public.Web.Hosting;

// =========================================================
// FILE: PublicSiteHost.cs
//
// PURPOSE:
// Builds the public site: static server rendering, languages, security headers, limits and the Platform.Api client (Stage 2).
//
// RESPONSIBILITIES:
// - Register services and build the request pipeline
//
// IMPORTANT NOTES:
// - No interactive render mode: pages are plain HTML and forms are plain posts (D-U1)
// - The CSP allows only the bot check (Turnstile) and the card checkout redirect
// - Data Protection keys go to the PublicSite:KeysPath folder: Public has no database (ADR-0015)
// =========================================================
public static class PublicSiteHost
{
    public static WebApplication Build(string[] args)
    {
        return Build(args, null);
    }

    public static WebApplication Build(string[] args, Action<WebApplicationBuilder>? configure)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.Configure<PublicSiteOptions>(builder.Configuration.GetSection(PublicSiteOptions.SectionName));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddLocalization();
        builder.Services.AddRazorComponents();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddMemoryCache();
        builder.Services.AddOutputCache();
        builder.Services.AddAntiforgery(options =>
        {
            // A __Host- cookie is dropped by browsers unless it is Secure.
            options.Cookie.Name = "__Host-vx4-public-af";
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });
        builder.Services.AddVixlyForwardedHeaders(builder.Configuration);
        builder.Services.AddPublicRateLimits();
        builder.Services.AddSingleton<PublicCallerSigner>();
        builder.Services.AddTransient<PublicCallerHandler>();
        builder.Services.AddHttpClient<PlatformApi>((provider, client) =>
            {
                client.BaseAddress = new Uri(provider.GetRequiredService<IOptions<PublicSiteOptions>>().Value.PlatformApiBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(20);
            })
            .AddHttpMessageHandler<PublicCallerHandler>();
        builder.Services.AddScoped<PlatformCatalog>();
        builder.Services.AddHealthChecks().AddCheck<PlatformApiHealthCheck>("platform-api", tags: new[] { VixlyHealth.ReadyTag });

        IDataProtectionBuilder protection = builder.Services.AddDataProtection().SetApplicationName("Vixly.V4.Public.Web");
        string? keys = builder.Configuration[$"{PublicSiteOptions.SectionName}:KeysPath"];

        if (!string.IsNullOrWhiteSpace(keys))
        {
            protection.PersistKeysToFileSystem(new DirectoryInfo(keys));
        }

        configure?.Invoke(builder);

        WebApplication app = builder.Build();
        PublicSiteOptions site = app.Services.GetRequiredService<IOptions<PublicSiteOptions>>().Value;

        if (!app.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(keys))
        {
            // ADR-0015: without a key folder a restart ends open forms and sign-ups in progress.
            app.Logger.LogWarning("PublicSite:KeysPath is not set: Data Protection keys live in memory only.");
        }

        app.UseForwardedHeaders();
        app.UseVixlySecurityHeaders(ContentSecurityPolicy(site));

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error", createScopeForErrors: true);
        }

        app.UseVixlyClientDisconnects();

        app.UseStatusCodePagesWithReExecute("/not-found");
        app.UseHttpsRedirection();
        app.UseRequestLocalization(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(VixlyCultures.Default);
            options.SupportedCultures = VixlyCultures.Supported.ToList();
            options.SupportedUICultures = VixlyCultures.Supported.ToList();
            options.RequestCultureProviders = new List<IRequestCultureProvider>
            {
                PublicLanguage.PathProvider,
                new CookieRequestCultureProvider { CookieName = PublicLanguage.CookieName },
                new AcceptLanguageHeaderRequestCultureProvider()
            };
        });
        app.UsePublicLanguage();
        app.UseRateLimiter();
        app.UseOutputCache();
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapVixlyHealth();
        app.MapPublicEndpoints();
        app.MapRazorComponents<App>();

        return app;
    }

    public static string ContentSecurityPolicy(PublicSiteOptions site)
    {
        ArgumentNullException.ThrowIfNull(site);

        const string Turnstile = "https://challenges.cloudflare.com";
        string checkout = string.Join(' ', site.CheckoutOrigins.Where(origin => Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri) && uri.Scheme == Uri.UriSchemeHttps));

        return "default-src 'self'; " +
            $"script-src 'self' {Turnstile}; " +
            "style-src 'self'; " +
            "img-src 'self' data:; " +
            "font-src 'self'; " +
            "connect-src 'self'; " +
            $"frame-src {Turnstile}; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            $"form-action 'self' {checkout}".TrimEnd() + "; " +
            "object-src 'none'";
    }
}
