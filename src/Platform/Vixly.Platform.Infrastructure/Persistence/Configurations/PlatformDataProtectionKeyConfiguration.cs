using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: PlatformDataProtectionKeyConfiguration.cs
//
// PURPOSE:
// Maps the ASP.NET Core Data Protection key ring to
// platform.DataProtectionKeys (S-5).
//
// RESPONSIBILITIES:
// - Name the table in the platform schema
//
// IMPORTANT NOTES:
// - Framework type, so it carries no plane prefix; the
//   naming test allows this one table
// - Each Platform deployable uses its own application name,
//   so their keys never mix
// =========================================================
internal sealed class PlatformDataProtectionKeyConfiguration : IEntityTypeConfiguration<DataProtectionKey>
{
    public void Configure(EntityTypeBuilder<DataProtectionKey> builder)
    {
        builder.ToTable("DataProtectionKeys");
    }
}
