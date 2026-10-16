using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Collections;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Collections;

// =========================================================
// FILE: PlatformCollectionsSettingsConfiguration.cs
//
// PURPOSE:
// Maps the collections settings row (D-C12).
//
// RESPONSIBILITIES:
// - Map the settings to CollectionsSettings with a row version
//
// IMPORTANT NOTES:
// - One row, keyed "default"
// =========================================================
internal sealed class PlatformCollectionsSettingsConfiguration : IEntityTypeConfiguration<PlatformCollectionsSettings>
{
    public void Configure(EntityTypeBuilder<PlatformCollectionsSettings> builder)
    {
        builder.ToTable("CollectionsSettings");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Key).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.HasIndex(item => item.Key).IsUnique();
        builder.Property(item => item.RetryDays).HasMaxLength(40).IsUnicode(false).IsRequired();
        builder.Ignore(item => item.RetryDayList);
        builder.Ignore(item => item.AccessRules);
        builder.HasOne<Vixly.Platform.Domain.Identity.PlatformUser>().WithMany().HasForeignKey(item => item.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
