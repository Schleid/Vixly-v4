using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Vixly.Customer.Infrastructure.Persistence;

// =========================================================
// FILE: CustomerKeysDbContext.cs
//
// PURPOSE:
// The Customer.Web key ring store, schema customer (S-5, ADR-0010).
//
// RESPONSIBILITIES:
// - Expose the Data Protection keys table
//
// IMPORTANT NOTES:
// - No business data here: Customer never touches the Operations database (cross-plane rule 2)
// =========================================================
public sealed class CustomerKeysDbContext : DbContext, IDataProtectionKeyContext
{
    public CustomerKeysDbContext(DbContextOptions<CustomerKeysDbContext> options)
        : base(options)
    {
    }

    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(CustomerDatabase.Schema);
        modelBuilder.Entity<DataProtectionKey>().ToTable("DataProtectionKeys");
    }
}
