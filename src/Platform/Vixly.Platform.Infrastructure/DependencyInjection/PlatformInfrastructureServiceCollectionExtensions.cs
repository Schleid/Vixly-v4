using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Infrastructure.Hosting;
using Vixly.Platform.Infrastructure.Mail;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Platform.Infrastructure.Persistence.Queries;
using Vixly.Platform.Infrastructure.Persistence.Repositories;
using Vixly.Platform.Infrastructure.Security;

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

        services.AddSingleton<IPlatformPasswordHasher, BCryptPlatformPasswordHasher>();
        services.AddSingleton<IPlatformSecretGenerator, PlatformSecretGenerator>();
        services.AddSingleton<IPlatformAuthenticatorCodes, TotpPlatformAuthenticatorCodes>();
        services.AddSingleton<IPlatformPasskeys, Fido2PlatformPasskeys>();
        services.AddSingleton(TimeProvider.System);

        services.AddLocalization();
        services.AddOptions<PlatformMailOptions>().Bind(configuration.GetSection(PlatformMailOptions.SectionName));
        services.AddOptions<PlatformHostingOptions>().Bind(configuration.GetSection(PlatformHostingOptions.SectionName));
        services.AddScoped<IPlatformIdentityMailer, SmtpPlatformIdentityMailer>();

        services.AddHealthChecks()
            .AddDbContextCheck<PlatformDbContext>("platform-database", tags: new[] { "ready" })
            .AddCheck<PlatformSchemaHealthCheck>("platform-schema", HealthStatus.Unhealthy, new[] { "ready" });

        return services;
    }
}
