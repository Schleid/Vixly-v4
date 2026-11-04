using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsUserSessionConfiguration.cs
//
// PURPOSE:
// Maps staff sessions to ops.UserSessions.
//
// RESPONSIBILITIES:
// - Unique token hash, looked up on each use
//
// IMPORTANT NOTES:
// - The token hash is fixed-length ASCII hex
// =========================================================
internal sealed class OperationsUserSessionConfiguration : IEntityTypeConfiguration<OperationsUserSession>
{
    public void Configure(EntityTypeBuilder<OperationsUserSession> builder)
    {
        builder.ToTable("UserSessions");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.TokenHash).HasMaxLength(OperationsIdentityLimits.TokenHashLength).IsFixedLength().IsUnicode(false).IsRequired();
        builder.HasOne<OperationsUser>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.TokenHash).IsUnique();
        builder.Property(item => item.IpAddress).HasMaxLength(OperationsIdentityLimits.IpAddressMaxLength).IsUnicode(false);
        builder.Property(item => item.UserAgent).HasMaxLength(OperationsIdentityLimits.UserAgentMaxLength);
        builder.Ignore(item => item.IsEnded);
        builder.HasIndex(item => new { item.UserId, item.EndedUtc });
        builder.Property(item => item.DeviceName).HasMaxLength(OperationsIdentityLimits.DeviceNameMaxLength);
        builder.Property(item => item.DeviceNumber).HasMaxLength(OperationsIdentityLimits.DeviceNumberMaxLength).IsUnicode(false);
        builder.Property(item => item.Country).HasMaxLength(2).IsFixedLength().IsUnicode(false);
        // Member activity (owner, 2026-10-21): the company's sign-ins, newest first.
        builder.HasIndex(item => new { item.TenantId, item.CreatedUtc });
    }
}
