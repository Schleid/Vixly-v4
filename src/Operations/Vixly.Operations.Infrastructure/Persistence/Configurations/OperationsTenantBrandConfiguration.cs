using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsTenantBrandConfiguration.cs
//
// PURPOSE:
// Maps the tenant brand to ops.TenantBrands, with its draft and published settings and its texts (Slice 1B).
//
// RESPONSIBILITIES:
// - One row per tenant; draft columns prefixed Draft_, published columns prefixed Published_ (the published name stays in DisplayName)
// - Texts in ops.SiteTexts, unique per stage, language and key
//
// IMPORTANT NOTES:
// - Row version guards concurrent edits
// - Column defaults keep rows written before Slice 1B valid
// =========================================================
internal sealed class OperationsTenantBrandConfiguration : IEntityTypeConfiguration<OperationsTenantBrand>
{
    public void Configure(EntityTypeBuilder<OperationsTenantBrand> builder)
    {
        builder.ToTable("TenantBrands");
        builder.HasKey(brand => brand.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.HasIndex(brand => brand.TenantId).IsUnique().HasDatabaseName("IX_TenantBrands_TenantId_One");
        builder.Ignore(brand => brand.DisplayName);
        builder.Ignore(brand => brand.HasUnpublishedChanges);
        builder.OwnsOne(brand => brand.Draft, settings => Map(settings, "Draft_", "Draft_DisplayName"));
        builder.OwnsOne(brand => brand.Published, settings => Map(settings, "Published_", "DisplayName"));
        builder.Navigation(brand => brand.Draft).IsRequired();
        builder.Navigation(brand => brand.Published).IsRequired();

        builder.OwnsMany(brand => brand.Texts, text =>
        {
            text.ToTable("SiteTexts");
            text.WithOwner().HasForeignKey(item => item.BrandId);
            text.HasKey(item => item.Id);
            text.HasIndex(item => item.TenantId);
            text.HasIndex(item => new { item.BrandId, item.Stage, item.Language, item.Key }).IsUnique();
            text.Property(item => item.Language).HasMaxLength(8).IsUnicode(false).IsRequired();
            text.Property(item => item.Key).HasMaxLength(40).IsUnicode(false).IsRequired();
            text.Property(item => item.Value).HasMaxLength(600).IsRequired();
        });
        builder.Navigation(brand => brand.Texts).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    private static void Map(OwnedNavigationBuilder<OperationsTenantBrand, OperationsBrandSettings> settings, string prefix, string nameColumn)
    {
        settings.Property(item => item.DisplayName).HasColumnName(nameColumn).HasMaxLength(OperationsBrandSettings.DisplayNameMaxLength).IsRequired();
        settings.Property(item => item.Preset).HasColumnName(prefix + "Preset");
        settings.Property(item => item.PrimaryColour).HasColumnName(prefix + "PrimaryColour").HasMaxLength(7).IsFixedLength().IsUnicode(false).HasDefaultValue("#1D4ED8").IsRequired();
        settings.Property(item => item.AccentColour).HasColumnName(prefix + "AccentColour").HasMaxLength(7).IsFixedLength().IsUnicode(false).HasDefaultValue("#0E7490").IsRequired();
        settings.Property(item => item.LogoImageId).HasColumnName(prefix + "LogoImageId");
        settings.Property(item => item.LogoDarkImageId).HasColumnName(prefix + "LogoDarkImageId");
        settings.Property(item => item.FaviconImageId).HasColumnName(prefix + "FaviconImageId");
        settings.Property(item => item.HeroImageId).HasColumnName(prefix + "HeroImageId");
        settings.Property(item => item.FacebookUrl).HasColumnName(prefix + "FacebookUrl").HasMaxLength(OperationsBrandSettings.LinkMaxLength);
        settings.Property(item => item.InstagramUrl).HasColumnName(prefix + "InstagramUrl").HasMaxLength(OperationsBrandSettings.LinkMaxLength);
        settings.Property(item => item.XUrl).HasColumnName(prefix + "XUrl").HasMaxLength(OperationsBrandSettings.LinkMaxLength);
        settings.Property(item => item.LinkedInUrl).HasColumnName(prefix + "LinkedInUrl").HasMaxLength(OperationsBrandSettings.LinkMaxLength);
        settings.Property(item => item.YouTubeUrl).HasColumnName(prefix + "YouTubeUrl").HasMaxLength(OperationsBrandSettings.LinkMaxLength);
        settings.Property(item => item.TikTokUrl).HasColumnName(prefix + "TikTokUrl").HasMaxLength(OperationsBrandSettings.LinkMaxLength);
        // The database default fills rows added before the column; a sentinel no value can take makes EF always send the chosen services, None included.
        settings.Property(item => item.Services).HasColumnName(prefix + "Services").HasDefaultValue(OperationsSiteServices.All).HasSentinel((OperationsSiteServices)(-1));
        settings.Property(item => item.ShowSteps).HasColumnName(prefix + "ShowSteps").HasDefaultValue(true);
        settings.Property(item => item.ShowFaq).HasColumnName(prefix + "ShowFaq");
    }
}
