using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.Extensions.Options;
using Vixly.Customer.Infrastructure.DependencyInjection;
using Vixly.Customer.Web.Components;
using Vixly.Customer.Web.Operations;
using Vixly.Customer.Web.Tenancy;
using Vixly.Shared.Hosting;
using Vixly.Shared.Localization;

namespace Vixly.Customer.Web.Hosting;

// =========================================================
// FILE: CustomerWebHost.cs
//
// PURPOSE:
// Builds the Customer.Web pipeline at /customer (Stage 3, Slice 0).
//
// RESPONSIBILITIES:
// - Serve everything under /customer
// - Register the key ring store, the Operations.Api client and Razor components
// - Order the middleware: forwarded headers, security headers, errors, disconnects, tenant, culture, antiforgery
//
// IMPORTANT NOTES:
// - Own process, cookies and keys (S-4, S-5); no Operations database (cross-plane rule 2)
// - Customer sign-in arrives in Slice 2
// - Tests build the same pipeline through Build(args, configure)
// =========================================================
public static class CustomerWebHost
{
    public static WebApplication Build(string[] args)
    {
        return Build(args, null);
    }

    public static WebApplication Build(string[] args, Action<WebApplicationBuilder>? configure)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddCustomerInfrastructure(builder.Configuration);
        builder.Services.AddVixlyForwardedHeaders(builder.Configuration);
        builder.Services.AddOptions<CustomerHostingOptions>().Bind(builder.Configuration.GetSection(CustomerHostingOptions.SectionName));
        builder.Services.AddOptions<CustomerOperationsOptions>().Bind(builder.Configuration.GetSection(CustomerOperationsOptions.SectionName));
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddMemoryCache();
        builder.Services.AddSingleton<CustomerCallerSigner>();
        builder.Services.AddHttpClient(CustomerOperationsApi.HttpClientName, (provider, client) =>
        {
            CustomerOperationsOptions options = provider.GetRequiredService<IOptions<CustomerOperationsOptions>>().Value;

            if (!string.IsNullOrWhiteSpace(options.BaseUrl))
            {
                client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
            }

            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(options.TimeoutSeconds, 1, 60));
        });
        builder.Services.AddSingleton<CustomerOperationsApi>();
        builder.Services.AddOptions<Vixly.Customer.Web.Portal.CustomerBotCheckOptions>().Bind(builder.Configuration.GetSection(Vixly.Customer.Web.Portal.CustomerBotCheckOptions.SectionName));
        builder.Services.AddHttpClient(Vixly.Customer.Web.Portal.TurnstileCustomerBotCheck.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(10));
        builder.Services.AddSingleton<Vixly.Customer.Web.Portal.ICustomerBotCheck, Vixly.Customer.Web.Portal.TurnstileCustomerBotCheck>();
        builder.Services.AddCustomerStatusPageLimits();
        builder.Services.AddLocalization();
        builder.Services.AddAntiforgery(options =>
        {
            options.Cookie.Name = CustomerCookies.Antiforgery;
            options.Cookie.Path = CustomerCookies.Path;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });
        builder.Services.AddRazorComponents();

        configure?.Invoke(builder);

        WebApplication app = builder.Build();

        app.UseForwardedHeaders();
        app.UseVixlySecurityHeaders(Vixly.Customer.Web.Portal.CustomerPortalSecurity.ContentSecurityPolicy);

        if (!app.Environment.IsDevelopment())
        {
            // Under the portal's path, so the error page passes the /customer check below.
            app.UseExceptionHandler(CustomerCookies.Path + VixlyErrorPage.Path, createScopeForErrors: true);
        }

        app.UseVixlyClientDisconnects();
        app.UseHttpsRedirection();
        app.UsePathBase(CustomerCookies.Path);
        app.Use((http, next) =>
        {
            // Everything this app serves lives under /customer (Edge routes only that path here).
            if (!http.Request.PathBase.HasValue)
            {
                http.Response.StatusCode = StatusCodes.Status404NotFound;
                return Task.CompletedTask;
            }

            return next(http);
        });
        app.UseMiddleware<CustomerTenantMiddleware>();
        app.UseRequestLocalization(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(VixlyCultures.Default);
            options.SupportedCultures = VixlyCultures.Supported.ToList();
            options.SupportedUICultures = VixlyCultures.Supported.ToList();
            options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider { CookieName = CustomerCookies.Culture });
        });
        // Routing runs after the tenant check, so a status page shown in place of the asked path is the page routed.
        app.UseRouting();
        // After routing, so the portal endpoints' own limits apply (Slice 2).
        app.UseRateLimiter();
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapVixlyHealth();
        Vixly.Customer.Web.Portal.CustomerPortalEndpoints.MapCustomerPortalEndpoints(app);
        Vixly.Customer.Web.Portal.CustomerPackageEndpoints.MapCustomerPackageEndpoints(app);
        Vixly.Customer.Web.Portal.CustomerShipmentEndpoints.MapCustomerShipmentEndpoints(app);
        Vixly.Customer.Web.Portal.CustomerRateEndpoints.MapCustomerRateEndpoints(app);
        Vixly.Customer.Web.Portal.CustomerTicketEndpoints.MapCustomerTicketEndpoints(app);
        Vixly.Customer.Web.Portal.CustomerInvoiceEndpoints.MapCustomerInvoiceEndpoints(app);
        // Any method: a failed form post comes back here as a post (2026-10-18).
        app.MapVixlyErrorPage(
            http =>
            {
                Microsoft.Extensions.Localization.IStringLocalizer<Vixly.Customer.Resources.CustomerText> text = http.RequestServices.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Vixly.Customer.Resources.CustomerText>>();
                return new VixlyErrorPageText(System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, text["ui.error.title"], text["ui.error.body"], text["ui.error.back"], CustomerCookies.Path + "/");
            },
            CustomerCookies.Path + "/css/customer-status.css");
        app.MapRazorComponents<App>();

        return app;
    }
}
