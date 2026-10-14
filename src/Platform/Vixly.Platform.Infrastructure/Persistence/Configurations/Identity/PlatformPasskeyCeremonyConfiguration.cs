using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Identity;

// =========================================================
// FILE: PlatformPasskeyCeremonyConfiguration.cs
//
// PURPOSE:
// Maps passkey ceremonies to platform.PasskeyCeremonies.
//
// RESPONSIBILITIES:
// - Unique token hash; index on expiry for the cleanup job
// - Optional link to the user (sign-in ceremonies have none)
//
// IMPORTANT NOTES:
// - Options are stored as sent to the browser (no secrets in
//   them: only a random challenge and public ids)
// =========================================================
internal sealed class PlatformPasskeyCeremonyConfiguration : IEntityTypeConfiguration<PlatformPasskeyCeremony>
{
    public void Configure(EntityTypeBuilder<PlatformPasskeyCeremony> builder)
    {
        builder.ToTable("PasskeyCeremonies");
        builder.HasKey(ceremony => ceremony.Id);

        builder.Property(ceremony => ceremony.TokenHash)
            .HasMaxLength(PlatformIdentityLimits.TokenHashLength)
            .IsFixedLength()
            .IsUnicode(false)
            .IsRequired();
        builder.Property(ceremony => ceremony.OptionsJson)
            .HasMaxLength(PlatformPasskeyCeremony.OptionsMaxLength)
            .IsUnicode(false)
            .IsRequired();

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(ceremony => ceremony.PlatformUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ceremony => ceremony.TokenHash).IsUnique();
        builder.HasIndex(ceremony => ceremony.ExpiresUtc);
    }
}
