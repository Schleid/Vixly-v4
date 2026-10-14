using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Identity;

// =========================================================
// FILE: PlatformAccountTokenConfiguration.cs
//
// PURPOSE:
// Maps invitation and password-reset links to
// platform.AccountTokens.
//
// RESPONSIBILITIES:
// - Unique token hash, looked up when a link is opened
// - Index by user and purpose, to revoke older links
//
// IMPORTANT NOTES:
// - One table for both purposes (V3 had two)
// =========================================================
internal sealed class PlatformAccountTokenConfiguration : IEntityTypeConfiguration<PlatformAccountToken>
{
    public void Configure(EntityTypeBuilder<PlatformAccountToken> builder)
    {
        builder.ToTable("AccountTokens");
        builder.HasKey(token => token.Id);

        builder.Property(token => token.TokenHash)
            .HasMaxLength(PlatformIdentityLimits.TokenHashLength)
            .IsFixedLength()
            .IsUnicode(false)
            .IsRequired();

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(token => token.PlatformUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(token => token.TokenHash).IsUnique();
        builder.HasIndex(token => new { token.PlatformUserId, token.Purpose });
    }
}
