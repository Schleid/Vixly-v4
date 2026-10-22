using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Vixly.Customer.Infrastructure.Persistence;

namespace Vixly.Customer.Infrastructure.Tests;

// =========================================================
// FILE: CustomerKeysModelTests.cs
//
// PURPOSE:
// Proves the Customer database holds only its key ring, in the customer schema (Slice 0, S-5).
//
// RESPONSIBILITIES:
// - One table, in the customer schema
//
// IMPORTANT NOTES:
// - Builds the model only
// =========================================================
public sealed class CustomerKeysModelTests
{
    [Fact]
    public void Model_IsOnlyTheKeyRing_InTheCustomerSchema()
    {
        using CustomerKeysDbContext context = new(new DbContextOptionsBuilder<CustomerKeysDbContext>()
            .UseSqlServer("Server=placeholder;Database=Model;Integrated Security=true")
            .Options);

        IEntityType table = Assert.Single(context.Model.GetEntityTypes());
        Assert.Equal("customer", table.GetSchema());
        Assert.Equal("DataProtectionKeys", table.GetTableName());
    }
}
