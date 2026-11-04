using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsCustomerSessionConfiguration.cs
//
// PURPOSE:
// Maps customer sessions to ops.CustomerSessions (Slice 2).
//
// RESPONSIBILITIES:
// - Find a session by its token hash; list a customer's open ones
//
// IMPORTANT NOTES:
// - Only the hash is stored
// =========================================================
internal sealed class OperationsCustomerSessionConfiguration : IEntityTypeConfiguration<OperationsCustomerSession>
{
    public void Configure(EntityTypeBuilder<OperationsCustomerSession> builder)
    {
        builder.ToTable("CustomerSessions");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.TokenHash).HasMaxLength(OperationsIdentityLimits.TokenHashLength).IsUnicode(false).IsFixedLength().IsRequired();
        builder.Property(item => item.IpAddress).HasMaxLength(OperationsIdentityLimits.IpAddressMaxLength).IsUnicode(false);
        builder.Property(item => item.UserAgent).HasMaxLength(OperationsIdentityLimits.UserAgentMaxLength);
        builder.Ignore(item => item.IsEnded);
        builder.HasIndex(item => item.TokenHash).IsUnique();
        builder.HasIndex(item => new { item.TenantId, item.CustomerId, item.EndedUtc });
        builder.HasOne<OperationsCustomer>().WithMany().HasForeignKey(item => item.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
