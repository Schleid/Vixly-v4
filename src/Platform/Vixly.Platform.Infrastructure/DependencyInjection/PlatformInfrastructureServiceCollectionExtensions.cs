using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Platform.Infrastructure.Persistence;

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
//
// IMPORTANT NOTES:
// - Called only from Program.cs of Platform.Api, Platform.Web
//   and Platform.Worker (Global Standard 3)
// - The connection string comes from the environment
//   (S-11); its value is never logged
// - The SQL login in it should have rights on the platform
//   schema only (D-1)
// =========================================================
public static class PlatformInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddPlatformInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

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
                .EnableRetryOnFailure()));

        return services;
    }
}
