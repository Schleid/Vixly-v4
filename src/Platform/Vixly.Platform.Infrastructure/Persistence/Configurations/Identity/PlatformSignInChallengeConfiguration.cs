using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Identity;

// =========================================================
// FILE: PlatformSignInChallengeConfiguration.cs
//
// PURPOSE:
// Maps pending sign-ins to platform.SignInChallenges.
//
// RESPONSIBILITIES:
// - Unique token hash, looked up at the second-factor step
// - Index by expiry, for the clean-up job
//
// IMPORTANT NOTES:
// - Rows are short-lived; the Worker removes expired ones
// =========================================================
internal sealed class PlatformSignInChallengeConfiguration : IEntityTypeConfiguration<PlatformSignInChallenge>
{
    public void Configure(EntityTypeBuilder<PlatformSignInChallenge> builder)
    {
        builder.ToTable("SignInChallenges");
        builder.HasKey(challenge => challenge.Id);

        builder.Property(challenge => challenge.TokenHash)
            .HasMaxLength(PlatformIdentityLimits.TokenHashLength)
            .IsFixedLength()
            .IsUnicode(false)
            .IsRequired();

        builder.HasOne<PlatformUser>()
            .WithMany()
            .HasForeignKey(challenge => challenge.PlatformUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(challenge => challenge.TokenHash).IsUnique();
        builder.HasIndex(challenge => challenge.ExpiresUtc);
    }
}
