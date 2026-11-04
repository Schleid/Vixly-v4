using Microsoft.AspNetCore.Authentication;
using Vixly.Shared.Hosting.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Infrastructure.DependencyInjection;
using Vixly.Operations.Web.Components;
using Vixly.Operations.Web.Endpoints;
using Vixly.Operations.Web.Security;
using Vixly.Operations.Web.Tenancy;
using Vixly.Shared.Hosting;
using Vixly.Shared.Hosting.Maintenance;
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

        // A missing setting names itself on a setup page instead of failing the start (owner, 2026-10-23).
        if (Vixly.Shared.Hosting.Settings.VixlySettingsCheck.Run(builder, OperationsWebSettings.Site, OperationsWebSettings.All, configure) is { } setup)
        {
            return setup;
        }

        builder.Services.AddOperationsInfrastructure(builder.Configuration, "Vixly.V4.Operations.Web");
        builder.Services.AddVixlyKeyRingEncryption(builder.Configuration);
        // Performance plan P-1 (owner, 2026-11-04): SQL commands and outgoing calls per request; slow requests logged.
        Vixly.Shared.Hosting.Diagnostics.VixlyRequestTiming.AddVixlyRequestTiming(builder.Services, builder.Configuration);

        // Performance plan P-5: the background jobs start one after another.
        Vixly.Shared.Hosting.Jobs.VixlyJobStagger.Add(builder.Services);
        builder.Services.AddVixlyForwardedHeaders(builder.Configuration);
        builder.Services.AddOptions<OperationsHostingOptions>().Bind(builder.Configuration.GetSection(OperationsHostingOptions.SectionName));
        builder.Services.AddHostedService<OperationsOutboxWorker>();
        builder.Services.AddHostedService<OperationsWebhookWorker>();
        builder.Services.AddHostedService<OperationsReportScheduleWorker>();
        builder.Services.AddHostedService<OperationsPlatformFeedWorker>();
        builder.Services.AddSingleton<OperationsUnclaimedWorker>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<OperationsUnclaimedWorker>());
        // Owner, 2026-10-29 (ticket desk): managers told about tickets that wait too long.
        builder.Services.AddSingleton<OperationsTicketEscalationWorker>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<OperationsTicketEscalationWorker>());
        builder.Services.AddSingleton<OperationsShipmentCapWorker>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<OperationsShipmentCapWorker>());
        builder.Services.AddSingleton<OperationsHousekeepingWorker>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<OperationsHousekeepingWorker>());
        builder.Services.AddSingleton<OperationsStatementWorker>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<OperationsStatementWorker>());
        // Owner, 2026-10-28: company brands for the Cloudflare Worker's maintenance page, every night.
        builder.Services.AddSingleton<OperationsEdgeBrandWorker>();
        builder.Services.AddHostedService(provider => provider.GetRequiredService<OperationsEdgeBrandWorker>());

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
        builder.Services.AddVixlyMaintenance(builder.Configuration);
        builder.Services.AddScoped<Vixly.Shared.Hosting.Maintenance.IVixlyMaintenanceSource, OperationsMaintenanceSource>();
        builder.Services.AddScoped<IOperationsRequestMetadata, OperationsHttpRequestMetadata>();
        builder.Services.AddScoped<OperationsErrorText>();
        builder.Services.AddScoped<Vixly.Operations.Web.Security.OperationsAllowed>();
        builder.Services.AddScoped<StaffPages>();
        HashSet<string> helpRoutes = new(StringComparer.Ordinal);
        builder.Services.AddSingleton(_ => new Vixly.Operations.Web.Features.Help.OperationsHelpCatalogue(helpRoutes));
        builder.Services.AddScoped<Vixly.Operations.Web.Features.Help.IOperationsHelpReaderSource, Vixly.Operations.Web.Features.Help.OperationsHelpReaderSource>();
        builder.Services.AddScoped<Vixly.Operations.Web.Features.Help.OperationsHelpPresenter>();
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

        configure?.Invoke(builder);

        WebApplication app = builder.Build();

        // Before the forwarded headers: the country is read only from a trusted proxy (owner, 2026-10-21).
        app.UseVixlyClientCountry();
        app.UseVixlyForwardedHeaders();
        Vixly.Shared.Hosting.Diagnostics.VixlyRequestTiming.UseVixlyRequestTiming(app, async http => http.Request.Path.StartsWithSegments("/staff") && OperationsCurrentUser.IsSignedIn(http)
            && (await http.RequestServices.GetRequiredService<Vixly.Operations.Web.Security.OperationsAllowed>().GetAsync()).IsOwner);
        app.UseOperationsDevelopmentCustomerProxy();
        app.UseVixlySecurityHeaders();
        app.UseOperationsCameraPages();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error", createScopeForErrors: true);
        }

        app.UseVixlyClientDisconnects();
        app.UseHttpsRedirection();
        // Before the tenant check: during maintenance nothing behind it may answer (Control backlog 2026-10-22).
        app.UseVixlyMaintenance(OperationsMaintenancePages.Site, OperationsMaintenancePages.Text, "/css/ops-status.css");
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
        app.UseMiddleware<OperationsBlankFields>();

        app.MapStaticAssets();
        app.MapVixlyHealth();
        app.MapOperationsAuthEndpoints();
        app.MapOperationsQuickSignInEndpoints();
        app.MapOperationsMapEndpoints();
        app.MapOperationsShelfCountEndpoints();
        app.MapOperationsSiteEndpoints();
        app.MapOperationsEditorEndpoints();
        app.MapOperationsDeviceEndpoints();
        app.MapOperationsPasswordEndpoints();
        app.MapOperationsAccountEndpoints();
        app.MapOperationsStaffEndpoints();
        app.MapOperationsHelpEndpoints();
        app.MapOperationsSettingsEndpoints();
        app.MapOperationsItemEndpoints();
        app.MapOperationsCompanyEndpoints();
        app.MapOperationsCustomerEndpoints();
        app.MapOperationsSupportEndpoints();
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
        app.MapOperationsCardEndpoints();
        app.MapOperationsScreenEndpoints();
        app.MapOperationsPayablesEndpoints();
        app.MapOperationsTextEndpoints();
        app.MapOperationsPublicApiEndpoints();
        app.MapOperationsApiSettingsEndpoints();
        app.MapOperationsReportEndpoints();
        app.MapOperationsBundleEndpoints();
        app.MapOperationsDispatchEndpoints();
        app.MapOperationsEventEndpoints();
        app.MapOperationsArrivalEndpoints();
        app.MapOperationsPickupEndpoints();
        app.MapOperationsDeliveryEndpoints();
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

        foreach (Microsoft.AspNetCore.Routing.RouteEndpoint endpoint in ((IEndpointRouteBuilder)app).DataSources
            .SelectMany(source => source.Endpoints).OfType<Microsoft.AspNetCore.Routing.RouteEndpoint>())
            if (endpoint.RoutePattern.RawText is { } route) helpRoutes.Add(route);
        _ = app.Services.GetRequiredService<Vixly.Operations.Web.Features.Help.OperationsHelpCatalogue>();

        return app;
    }
}
