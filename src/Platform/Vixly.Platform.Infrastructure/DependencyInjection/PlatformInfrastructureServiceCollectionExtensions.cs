using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Infrastructure.Hosting;
using Vixly.Platform.Infrastructure.Mail;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Platform.Infrastructure.Persistence.Queries;
using Vixly.Platform.Infrastructure.Persistence.Repositories;
using Vixly.Platform.Infrastructure.Security;
using Vixly.Platform.Infrastructure.Tenants;

namespace Vixly.Platform.Infrastructure.DependencyInjection;

// =========================================================
// FILE: PlatformInfrastructureServiceCollectionExtensions.cs
//
// PURPOSE:
// Registers the Platform plane's infrastructure for a host's
// composition root.
//
// RESPONSIBILITIES:
// - Read ConnectionStrings:Platform and fail fast when it is
//   missing
// - Register PlatformDbContext on SQL Server with the history
//   table in the platform schema and transient-fault retry
// - Register repositories, the unit of work, the permission
//   reader and the security adapters
// - Store the Data Protection key ring in the platform schema
//   under the host's own application name (S-5)
//
// IMPORTANT NOTES:
// - Called only from Program.cs of Platform.Api, Platform.Web
//   and Platform.Worker (Global Standard 3)
// - The connection string comes from the environment
//   (S-11); its value is never logged
// - The SQL login in it should have rights on the platform
//   schema only (D-1)
// - Queries that load several collections split into one
//   query per collection, never a cartesian product (P-1)
// - dataProtectionApplicationName must differ from V3's and
//   between deployables, for example "Vixly.V4.Platform.Web"
// =========================================================
public static class PlatformInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration,
        string dataProtectionApplicationName = "Vixly.V4.Platform")
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentException.ThrowIfNullOrWhiteSpace(dataProtectionApplicationName);

        string? connectionString = configuration.GetConnectionString(PlatformDatabase.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"ConnectionStrings:{PlatformDatabase.ConnectionStringName} is not configured.");
        }

        services.AddDbContext<PlatformDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql
                .MigrationsHistoryTable(PlatformDatabase.MigrationsHistoryTable, PlatformDatabase.Schema)
                .UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                .EnableRetryOnFailure()));

        services.AddDataProtection()
            .SetApplicationName(dataProtectionApplicationName)
            .PersistKeysToDbContext<PlatformDbContext>();

        services.AddScoped<IPlatformUnitOfWork, PlatformUnitOfWork>();
        services.AddScoped<IPlatformUserRepository, PlatformUserRepository>();
        services.AddScoped<IPlatformRoleRepository, PlatformRoleRepository>();
        services.AddScoped<IPlatformDepartmentRepository, PlatformDepartmentRepository>();
        services.AddScoped<IPlatformSessionRepository, PlatformSessionRepository>();
        services.AddScoped<IPlatformAuditRepository, PlatformAuditRepository>();
        services.AddScoped<IPlatformSignInChallengeRepository, PlatformSignInChallengeRepository>();
        services.AddScoped<IPlatformPasskeyCeremonyRepository, PlatformPasskeyCeremonyRepository>();
        services.AddScoped<IPlatformAccountTokenRepository, PlatformAccountTokenRepository>();
        services.AddScoped<IPlatformPasswordResetRequestRepository, PlatformPasswordResetRequestRepository>();
        services.AddScoped<IPlatformPermissionReader, PlatformPermissionReader>();
        services.AddScoped<IPlatformUserQueries, PlatformUserQueries>();
        services.AddScoped<IPlatformRoleQueries, PlatformRoleQueries>();
        services.AddScoped<IPlatformDepartmentQueries, PlatformDepartmentQueries>();
        services.AddScoped<IPlatformAuditQueries, PlatformAuditQueries>();
        services.AddScoped<IPlatformTenantRepository, PlatformTenantRepository>();
        services.AddScoped<IPlatformPortalSlugRepository, PlatformPortalSlugRepository>();
        services.AddScoped<IPlatformTenantRegistrationRepository, PlatformTenantRegistrationRepository>();
        services.AddScoped<IPlatformOutboxRepository, PlatformOutboxRepository>();
        services.AddScoped<IPlatformPublishedEventRepository, PlatformPublishedEventRepository>();
        services.AddScoped<IPlatformSalesLeadRepository, PlatformSalesLeadRepository>();
        services.AddScoped<IPlatformTenantChangeRepository, PlatformTenantChangeRepository>();
        services.AddScoped<IPlatformLeadMailer, SmtpPlatformLeadMailer>();
        services.AddScoped<IPlatformTenantQueries, PlatformTenantQueries>();
        services.AddScoped<IPlatformTenantPolicy, ConfiguredPlatformTenantPolicy>();
        services.AddScoped<IPlatformPlanRepository, PlatformPlanRepository>();
        services.AddScoped<IPlatformSubscriptionRepository, PlatformSubscriptionRepository>();
        services.AddScoped<IPlatformLimitOverrideRepository, PlatformLimitOverrideRepository>();
        services.AddScoped<IPlatformSalesSettingsRepository, PlatformSalesSettingsRepository>();
        services.AddScoped<IPlatformPromotionRepository, PlatformPromotionRepository>();
        services.AddScoped<IPlatformOfferRepository, PlatformOfferRepository>();
        services.AddScoped<PlatformInvoiceRepository>();
        services.AddScoped<IPlatformInvoiceRepository>(provider => provider.GetRequiredService<PlatformInvoiceRepository>());
        services.AddScoped<IPlatformInvoiceHistory>(provider => provider.GetRequiredService<PlatformInvoiceRepository>());
        services.AddScoped<IPlatformBillingRepository, PlatformBillingRepository>();
        services.AddScoped<IPlatformBillingQueries, PlatformBillingQueries>();
        services.AddScoped<IPlatformPublicLinks, PlatformPublicLinks>();
        services.AddScoped<IPlatformSubscriptionQueries, PlatformSubscriptionQueries>();
        services.AddScoped<IPlatformLicenceCounter, Vixly.Platform.Infrastructure.Licensing.PlatformDeviceLicenceCounter>();
        services.AddScoped<IPlatformLicensingRepository, PlatformLicensingRepository>();
        services.AddScoped<IPlatformLicensingQueries, PlatformLicensingQueries>();
        services.AddScoped<IPlatformLicensingMailer, SmtpPlatformLicensingMailer>();
        services.AddScoped<IPlatformZelleRepository, PlatformZelleRepository>();
        services.AddScoped<IPlatformZelleQueries, PlatformZelleQueries>();
        services.AddScoped<IPlatformZelleMailer, SmtpPlatformZelleMailer>();
        services.AddScoped<IPlatformLegalRepository, PlatformLegalRepository>();
        services.AddScoped<IPlatformLegalQueries, PlatformLegalQueries>();
        services.AddScoped<IPlatformLegalMailer, SmtpPlatformLegalMailer>();
        services.AddSingleton<IPlatformDeviceKeys, Vixly.Platform.Infrastructure.Licensing.PlatformDeviceKeys>();
        services.AddSingleton<IPlatformRandom, PlatformRandom>();

        services.AddSingleton<IPlatformPasswordHasher, BCryptPlatformPasswordHasher>();
        services.AddSingleton<IPlatformSecretGenerator, PlatformSecretGenerator>();
        services.AddSingleton<IPlatformAuthenticatorCodes, TotpPlatformAuthenticatorCodes>();
        services.AddSingleton<IPlatformPasskeys, Fido2PlatformPasskeys>();
        services.AddSingleton(TimeProvider.System);

        services.AddLocalization();
        services.AddOptions<PlatformMailOptions>().Bind(configuration.GetSection(PlatformMailOptions.SectionName));
        services.AddOptions<Vixly.Platform.Infrastructure.Documents.PlatformDocumentOptions>().Bind(configuration.GetSection(Vixly.Platform.Infrastructure.Documents.PlatformDocumentOptions.SectionName));
        services.AddOptions<PlatformHostingOptions>().Bind(configuration.GetSection(PlatformHostingOptions.SectionName));
        services.AddOptions<PlatformTenantOptions>().Bind(configuration.GetSection(PlatformTenantOptions.SectionName));
        services.AddOptions<PlatformBotCheckOptions>().Bind(configuration.GetSection(PlatformBotCheckOptions.SectionName));
        services.AddScoped<PlatformMailSender>();
        services.AddScoped<IPlatformIdentityMailer, SmtpPlatformIdentityMailer>();
        services.AddScoped<IPlatformTenantMailer, SmtpPlatformTenantMailer>();
        services.AddScoped<IPlatformSubscriptionMailer, SmtpPlatformSubscriptionMailer>();
        services.AddScoped<IPlatformSalesMailer, SmtpPlatformSalesMailer>();
        services.AddScoped<IPlatformBillingMailer, SmtpPlatformBillingMailer>();
        services.AddScoped<IPlatformBillingDocuments, Vixly.Platform.Infrastructure.Documents.QuestPdfPlatformBillingDocuments>();
        AddPayments(services, configuration);
        services.AddHttpClient<IPlatformBotCheck, TurnstilePlatformBotCheck>(client => client.Timeout = TimeSpan.FromSeconds(10));

        services.AddHealthChecks()
            .AddDbContextCheck<PlatformDbContext>("platform-database", tags: new[] { "ready" })
            .AddCheck<PlatformSchemaHealthCheck>("platform-schema", HealthStatus.Unhealthy, new[] { "ready" });

        return services;
    }

    // Slice 5: card payments. Unset provider means Simulated in Development and Stripe elsewhere.
    private static void AddPayments(IServiceCollection services, IConfiguration configuration)
    {
        string environment = configuration["environment"] ?? configuration["ASPNETCORE_ENVIRONMENT"] ?? configuration["DOTNET_ENVIRONMENT"] ?? "Production";
        bool development = string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);

        services.AddOptions<Vixly.Platform.Infrastructure.Payments.PlatformPaymentsOptions>().Bind(configuration.GetSection(Vixly.Platform.Infrastructure.Payments.PlatformPaymentsOptions.SectionName));
        services.AddScoped<IPlatformPaymentsRepository, PlatformPaymentsRepository>();
        services.AddScoped<IPlatformPaymentsQueries, PlatformPaymentsQueries>();
        services.AddScoped<IPlatformPaymentsMailer, SmtpPlatformPaymentsMailer>();
        services.AddScoped<IPlatformCollectionsRepository, PlatformCollectionsRepository>();
        services.AddScoped<IPlatformCollectionsQueries, PlatformCollectionsQueries>();
        services.AddScoped<IPlatformCollectionsMailer, SmtpPlatformCollectionsMailer>();

        // The simulated provider posts its events to the local API; it trusts the local development certificate only.
        services.AddHttpClient(Vixly.Platform.Infrastructure.Payments.SimulatedCardGateway.HttpClientName, client => client.Timeout = TimeSpan.FromSeconds(10))
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (request, _, _, errors) =>
                    errors == System.Net.Security.SslPolicyErrors.None || (development && request.RequestUri is { IsLoopback: true })
            });

        services.AddSingleton<IPlatformCardGateway>(provider =>
        {
            IOptions<Vixly.Platform.Infrastructure.Payments.PlatformPaymentsOptions> options = provider.GetRequiredService<IOptions<Vixly.Platform.Infrastructure.Payments.PlatformPaymentsOptions>>();
            string chosen = options.Value.Provider ?? (development ? "Simulated" : "Stripe");

            return string.Equals(chosen, "Simulated", StringComparison.OrdinalIgnoreCase)
                ? new Vixly.Platform.Infrastructure.Payments.SimulatedCardGateway(options, environment, provider.GetRequiredService<IHttpClientFactory>(), provider.GetRequiredService<ILogger<Vixly.Platform.Infrastructure.Payments.SimulatedCardGateway>>())
                : new Vixly.Platform.Infrastructure.Payments.StripeCardGateway(options, environment, provider.GetRequiredService<ILogger<Vixly.Platform.Infrastructure.Payments.StripeCardGateway>>());
        });
    }
}
