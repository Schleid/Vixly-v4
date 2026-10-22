using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Vixly.Customer.Infrastructure.Persistence;

// =========================================================
// FILE: CustomerKeysDbContextDesignTimeFactory.cs
//
// PURPOSE:
// Builds the key ring DbContext for dotnet ef (migrations only).
//
// RESPONSIBILITIES:
// - Give the tools a context with the customer history table
//
// IMPORTANT NOTES:
// - Never used at run time
// =========================================================
public sealed class CustomerKeysDbContextDesignTimeFactory : IDesignTimeDbContextFactory<CustomerKeysDbContext>
{
    public CustomerKeysDbContext CreateDbContext(string[] args)
    {
        string connectionString =
            Environment.GetEnvironmentVariable("VIXLY_CUSTOMER_DESIGN_CONNECTION")
            ?? @"Server=GDELL\SQLEXPRESS;Database=VixlyV4;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;Application Name=Vixly.V4.Customer.Migrations";

        return new CustomerKeysDbContext(new DbContextOptionsBuilder<CustomerKeysDbContext>()
            .UseSqlServer(connectionString, sql => sql.MigrationsHistoryTable(CustomerDatabase.MigrationsHistoryTable, CustomerDatabase.Schema))
            .Options);
    }
}
