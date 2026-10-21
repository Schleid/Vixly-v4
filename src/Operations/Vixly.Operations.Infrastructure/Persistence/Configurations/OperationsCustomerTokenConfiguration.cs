using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsCustomerTokenConfiguration.cs
//
// PURPOSE:
// Maps customer links to ops.CustomerTokens (Slice 2).
//
// RESPONSIBILITIES:
// - Find a link by its hash
//
// IMPORTANT NOTES:
// - Only the hash is stored
// =========================================================
internal sealed class OperationsCustomerTokenConfiguration : IEntityTypeConfiguration<OperationsCustomerToken>
{
    public void Configure(EntityTypeBuilder<OperationsCustomerToken> builder)
    {
        builder.ToTable("CustomerTokens");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.TokenHash).HasMaxLength(OperationsIdentityLimits.TokenHashLength).IsUnicode(false).IsFixedLength().IsRequired();
        builder.HasIndex(item => item.TokenHash).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.CustomerId, item.Purpose });
        builder.HasOne<OperationsCustomer>().WithMany().HasForeignKey(item => item.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
