using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsAccountTokenConfiguration.cs
//
// PURPOSE:
// Maps invitation and password-reset links to ops.AccountTokens.
//
// RESPONSIBILITIES:
// - Unique token hash, looked up on each use
//
// IMPORTANT NOTES:
// - The token hash is fixed-length ASCII hex
// =========================================================
internal sealed class OperationsAccountTokenConfiguration : IEntityTypeConfiguration<OperationsAccountToken>
{
    public void Configure(EntityTypeBuilder<OperationsAccountToken> builder)
    {
        builder.ToTable("AccountTokens");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.TokenHash).HasMaxLength(OperationsIdentityLimits.TokenHashLength).IsFixedLength().IsUnicode(false).IsRequired();
        builder.HasOne<OperationsUser>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.TokenHash).IsUnique();
        builder.HasIndex(item => new { item.UserId, item.Purpose });
    }
}
