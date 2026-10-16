using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Legal;

// =========================================================
// FILE: PlatformLegalDocumentConfiguration.cs
//
// PURPOSE:
// Maps legal document versions to platform.LegalDocuments, with their included versions.
//
// RESPONSIBILITIES:
// - One published version per kind; version label unique per kind; row version
//
// IMPORTANT NOTES:
// - Included versions live in platform.LegalDocumentParts
// =========================================================
internal sealed class PlatformLegalDocumentConfiguration : IEntityTypeConfiguration<PlatformLegalDocument>
{
    public void Configure(EntityTypeBuilder<PlatformLegalDocument> builder)
    {
        builder.ToTable("LegalDocuments");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Version).HasMaxLength(PlatformLegalLimits.VersionMaxLength).IsRequired();
        builder.HasIndex(item => new { item.Kind, item.Version }).IsUnique();
        builder.HasIndex(item => item.Kind, "IX_LegalDocuments_OnePublished").IsUnique().HasFilter("[Status] = 2");
        builder.HasIndex(item => new { item.Kind, item.PublishedUtc });
        builder.Ignore(item => item.IsDraft);
        builder.Ignore(item => item.NeedsAcceptance);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.PublishedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.OwnsMany(item => item.Parts, part =>
        {
            part.ToTable("LegalDocumentParts");
            part.WithOwner().HasForeignKey("PlatformLegalDocumentId");
            part.Property<int>("Id").ValueGeneratedOnAdd();
            part.HasKey("Id");
            part.Property(value => value.Version).HasMaxLength(PlatformLegalLimits.VersionMaxLength).IsRequired();
            part.HasOne<PlatformLegalDocument>().WithMany().HasForeignKey(value => value.IncludedDocumentId).OnDelete(DeleteBehavior.Restrict);
        });
        builder.Navigation(item => item.Parts).HasField("_parts").UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
