using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Infrastructure.Persistence.Conversions;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Identity;

// =========================================================
// FILE: PlatformPasswordResetRequestConfiguration.cs
//
// PURPOSE:
// Maps the reset-request throttle log to
// platform.PasswordResetRequests.
//
// RESPONSIBILITIES:
// - Index by email and time, and by client IP and time, for
//   the two throttle checks
//
// IMPORTANT NOTES:
// - No foreign key: requests for unknown emails are stored
//   too, on purpose
// =========================================================
internal sealed class PlatformPasswordResetRequestConfiguration : IEntityTypeConfiguration<PlatformPasswordResetRequest>
{
    public void Configure(EntityTypeBuilder<PlatformPasswordResetRequest> builder)
    {
        builder.ToTable("PasswordResetRequests");
        builder.HasKey(request => request.Id);

        builder.Property(request => request.Email)
            .HasConversion(PlatformValueConverters.Email)
            .HasMaxLength(EmailAddress.MaxLength)
            .IsRequired();
        builder.Property(request => request.IpAddress).HasMaxLength(PlatformIdentityLimits.IpAddressMaxLength).IsUnicode(false);

        builder.HasIndex(request => new { request.Email, request.CreatedUtc });
        builder.HasIndex(request => new { request.IpAddress, request.CreatedUtc });
    }
}
