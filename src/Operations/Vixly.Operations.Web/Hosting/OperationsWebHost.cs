using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Infrastructure.DependencyInjection;
using Vixly.Operations.Web.Components;
using Vixly.Operations.Web.Endpoints;
using Vixly.Operations.Web.Security;
using Vixly.Operations.Web.Tenancy;
using Vixly.Shared.Hosting;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Web.Hosting;

// =========================================================
// FILE: OperationsWebHost.cs
//
// PURPOSE:
// Builds the Operations.Web pipeline (Stage 3, Slice 0).
//
// RESPONSIBILITIES:
// - Register Infrastructure, hosted workers and Razor components
// - Order the middleware: forwarded headers, security headers, errors, disconnects, tenant, culture, antiforgery
//
// IMPORTANT NOTES:
// - The tenant is resolved before culture, so the tenant default language applies (L-4)
// - Sign-in arrives in Slice 1; Slice 0 serves status pages only
// - Tests build the same pipeline through Build(args, configure)
// =========================================================
public static class OperationsWebHost
{
    public static WebApplication Build(string[] args)
    {
        return Build(args, null);
    }

    public static WebApplication Build(string[] args, Action<WebApplicationBuilder>? configure)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddOperationsInfrastructure(builder.Configuration, "Vixly.V4.Operations.Web");
        builder.Services.AddVixlyForwardedHeaders(builder.Configuration);
        builder.Services.AddOptions<OperationsHostingOptions>().Bind(builder.Configuration.GetSection(OperationsHostingOptions.SectionName));
        builder.Services.AddHostedService<OperationsOutboxWorker>();
        builder.Services.AddHostedService<OperationsPlatformFeedWorker>();
        builder.Services.AddSingleton<OperationsUnclaimedWorker>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<OperationsUnclaimedWorker>());
        builder.Services.AddSingleton<OperationsStatementWorker>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<OperationsStatementWorker>());

        if (builder.Environment.IsDevelopment())
        {
            builder.Services.AddOptions<OperationsDemoDataOptions>().Bind(builder.Configuration.GetSection(OperationsDemoDataOptions.SectionName));
            builder.Services.AddHostedService<OperationsDemoDataWorker>();
            builder.Services.AddScoped<OperationsDemoStaffSeeder>();
            builder.Services.AddTransient<OperationsDemoScenario>();
            builder.Services.AddOperationsDevelopmentCustomerProxy(builder.Configuration);
        }
        builder.Services.AddOperationsRateLimits();
        builder.Services.AddMemoryCache();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddScoped<IOperationsRequestMetadata, OperationsHttpRequestMetadata>();
        builder.Services.AddScoped<OperationsErrorText>();
        builder.Services.AddScoped<Vixly.Operations.Web.Security.OperationsAllowed>();
        builder.Services.AddScoped<StaffPages>();
        builder.Services.AddAuthentication(OperationsSessionAuthenticationHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, OperationsSessionAuthenticationHandler>(OperationsSessionAuthenticationHandler.SchemeName, null);
        builder.Services.AddAuthorization();
        builder.Services.AddLocalization();
        builder.Services.AddAntiforgery(options =>
        {
            options.Cookie.Name = OperationsCookies.Antiforgery;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        });
        builder.Services.AddRazorComponents();
        builder.Services.AddScoped<Vixly.Operations.Web.Features.SignIn.LoginBrand.OperationsLoginBrandPresenter>();

        configure?.Invoke(builder);

        WebApplication app = builder.Build();

        app.UseForwardedHeaders();
        app.UseOperationsDevelopmentCustomerProxy();
        app.UseVixlySecurityHeaders();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error", createScopeForErrors: true);
        }

        app.UseVixlyClientDisconnects();
        app.UseHttpsRedirection();
        app.UseMiddleware<OperationsTenantMiddleware>();
        app.UseRateLimiter();
        app.UseRequestLocalization(options =>
        {
            options.DefaultRequestCulture = new RequestCulture(VixlyCultures.Default);
            options.SupportedCultures = VixlyCultures.Supported.ToList();
            options.SupportedUICultures = VixlyCultures.Supported.ToList();
            options.RequestCultureProviders.Insert(0, new OperationsTenantCultureProvider());
            options.RequestCultureProviders.Insert(0, new CookieRequestCultureProvider { CookieName = OperationsCookies.Culture });
            options.RequestCultureProviders.Insert(0, new OperationsSitePathCultureProvider());
        });
        // Routing runs after the tenant check, so a status page shown in place of the asked path is the page routed.
        app.UseRouting();
        app.UseAuthentication();
        app.UseMiddleware<OperationsDeviceGate>();
        app.UseMiddleware<OperationsAccessGate>();
        app.UseAuthorization();
        app.UseAntiforgery();

        app.MapStaticAssets();
        app.MapVixlyHealth();
        app.MapOperationsAuthEndpoints();
        app.MapOperationsSiteEndpoints();
        app.MapOperationsEditorEndpoints();
        app.MapOperationsDeviceEndpoints();
        app.MapOperationsPasswordEndpoints();
        app.MapOperationsAccountEndpoints();
        app.MapOperationsStaffEndpoints();
        app.MapOperationsSettingsEndpoints();
        app.MapOperationsCompanyEndpoints();
        app.MapOperationsCustomerEndpoints();
        app.MapOperationsParcelEndpoints();
        app.MapOperationsIncomingEndpoints();
        app.MapOperationsShippingLabelEndpoints();
        app.MapOperationsStockCountEndpoints();
        app.MapOperationsTicketEndpoints();
        app.MapOperationsCounterEndpoints();
        app.MapOperationsCounterSupplyEndpoints();
        app.MapOperationsShipmentEndpoints();
        app.MapOperationsInsuranceEndpoints();
        app.MapOperationsReceivingEndpoints();
        app.MapOperationsInspectionEndpoints();
        app.MapOperationsInspectionWorkEndpoints();
        app.MapOperationsChargesEndpoints();
        app.MapOperationsQuotesEndpoints();
        app.MapOperationsSupplyEndpoints();
        app.MapOperationsInvoiceEndpoints();
        app.MapOperationsCashierEndpoints();
        app.MapOperationsBundleEndpoints();
        app.MapOperationsDispatchEndpoints();
        app.MapOperationsEventEndpoints();
        app.MapOperationsArrivalEndpoints();
        app.MapOperationsPickupEndpoints();
        app.MapOperationsCreditEndpoints();
        app.MapOperationsSupplyStockEndpoints();
        app.MapOperationsPrinterEndpoints();
        app.MapOperationsPrintAgentEndpoints();
        // Any method: a failed form post comes back here as a post (2026-10-18).
        app.MapVixlyErrorPage(
            http =>
            {
                Microsoft.Extensions.Localization.IStringLocalizer<Vixly.Operations.Resources.OperationsText> text = http.RequestServices.GetRequiredService<Microsoft.Extensions.Localization.IStringLocalizer<Vixly.Operations.Resources.OperationsText>>();
                return new VixlyErrorPageText(System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, text["ui.error.title"], text["ui.error.body"], text["ui.error.back"], http.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>()?.Path.StartsWith("/staff", StringComparison.Ordinal) == true ? "/staff" : "/");
            },
            "/css/ops-status.css");
        app.MapRazorComponents<App>();

        return app;
    }
}
