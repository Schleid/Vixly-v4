using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsSignInChallengeConfiguration.cs
//
// PURPOSE:
// Maps pending sign-ins to ops.SignInChallenges.
//
// RESPONSIBILITIES:
// - Unique token hash, looked up on each use
//
// IMPORTANT NOTES:
// - The token hash is fixed-length ASCII hex
// =========================================================
internal sealed class OperationsSignInChallengeConfiguration : IEntityTypeConfiguration<OperationsSignInChallenge>
{
    public void Configure(EntityTypeBuilder<OperationsSignInChallenge> builder)
    {
        builder.ToTable("SignInChallenges");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.TokenHash).HasMaxLength(OperationsIdentityLimits.TokenHashLength).IsFixedLength().IsUnicode(false).IsRequired();
        builder.HasOne<OperationsUser>().WithMany().HasForeignKey(item => item.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => item.TokenHash).IsUnique();
        builder.HasIndex(item => item.ExpiresUtc);
        builder.Property(item => item.DeviceName).HasMaxLength(OperationsIdentityLimits.DeviceNameMaxLength);
        builder.Property(item => item.DeviceNumber).HasMaxLength(OperationsIdentityLimits.DeviceNumberMaxLength).IsUnicode(false);
        builder.Property(item => item.Country).HasMaxLength(2).IsFixedLength().IsUnicode(false);
        builder.Ignore(item => item.Place);
    }
}
