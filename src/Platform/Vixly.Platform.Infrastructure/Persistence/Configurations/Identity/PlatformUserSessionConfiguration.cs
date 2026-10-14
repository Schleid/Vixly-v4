using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Identity;

// =========================================================
// FILE: PlatformUserSessionConfiguration.cs
//
// PURPOSE:
// Maps staff sessions to platform.UserSessions.
//
// RESPONSIBILITIES:
// - Unique token hash, looked up on every request
// - Index open sessions per user, for the session list and
//   for ending them all at once
//
// IMPORTANT NOTES:
// - The token hash is fixed-length ASCII hex
// =========================================================
internal sealed class PlatformUserSessionConfiguration : IEntityTypeConfiguration<PlatformUserSession>
{
    public void Configure(EntityTypeBuilder<PlatformUserSession> builder)
    {
        builder.ToTable("UserSessions");
        builder.HasKey(session => session.Id);

        builder.Property(session => session.TokenHash)
            .HasMaxLength(PlatformIdentityLimits.TokenHashLength)
            .IsFixedLength()
            .IsUnicode(false)
            .IsRequired();
        builder.Property(session => session.IpAddress).HasMaxLength(PlatformIdentityLimits.IpAddressMaxLength).IsUnicode(false);
        builder.Property(session => session.UserAgent).HasMaxLength(PlatformIdentityLimits.UserAgentMaxLength);
        builder.Ignore(session => session.IsEnded);

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(session => session.PlatformUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(session => session.TokenHash).IsUnique();
        builder.HasIndex(session => new { session.PlatformUserId, session.EndedUtc });
    }
}
