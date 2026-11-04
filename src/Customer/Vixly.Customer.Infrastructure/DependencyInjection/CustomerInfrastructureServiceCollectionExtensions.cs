using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Vixly.Customer.Infrastructure.Persistence;

namespace Vixly.Customer.Infrastructure.DependencyInjection;

// =========================================================
// FILE: CustomerInfrastructureServiceCollectionExtensions.cs
//
// PURPOSE:
// Registers the Customer.Web key ring store (composition root only).
//
// RESPONSIBILITIES:
// - Persist the key ring in the customer schema under its own application name (S-5)
// - Add the readiness check
//
// IMPORTANT NOTES:
// - A missing connection string stops the start
// =========================================================
public static class CustomerInfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddCustomerInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        string? connectionString = configuration.GetConnectionString(CustomerDatabase.ConnectionStringName);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException($"ConnectionStrings:{CustomerDatabase.ConnectionStringName} is not configured.");
        }

        services.AddDbContext<CustomerKeysDbContext>(options => options.UseSqlServer(
            connectionString,
            sql => sql.MigrationsHistoryTable(CustomerDatabase.MigrationsHistoryTable, CustomerDatabase.Schema).EnableRetryOnFailure()));
        services.AddDataProtection()
            .SetApplicationName("Vixly.V4.Customer.Web")
            .PersistKeysToDbContext<CustomerKeysDbContext>();
        services.AddHealthChecks().AddDbContextCheck<CustomerKeysDbContext>("customer-keys", tags: new[] { "ready" });

        return services;
    }
}
