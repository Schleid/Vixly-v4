using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Legal;

// =========================================================
// FILE: PlatformLegalDocumentFileConfiguration.cs
//
// PURPOSE:
// Maps the PDF per language to platform.LegalDocumentFiles.
//
// RESPONSIBILITIES:
// - One file per document and language
//
// IMPORTANT NOTES:
// - Bytes are loaded only by the file queries
// =========================================================
internal sealed class PlatformLegalDocumentFileConfiguration : IEntityTypeConfiguration<PlatformLegalDocumentFile>
{
    public void Configure(EntityTypeBuilder<PlatformLegalDocumentFile> builder)
    {
        builder.ToTable("LegalDocumentFiles");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Language).HasMaxLength(8).IsUnicode(false).IsRequired();
        builder.Property(item => item.Content).HasMaxLength(PlatformLegalLimits.MaxFileMegabytes * 1024 * 1024).IsRequired();
        builder.Property(item => item.Sha256).HasMaxLength(PlatformLegalLimits.HashLength).IsUnicode(false).IsFixedLength().IsRequired();
        builder.Property(item => item.Summary).HasMaxLength(PlatformLegalLimits.SummaryMaxLength);
        builder.HasIndex(item => new { item.PlatformLegalDocumentId, item.Language }).IsUnique();
        builder.HasOne<PlatformLegalDocument>().WithMany().HasForeignKey(item => item.PlatformLegalDocumentId).OnDelete(DeleteBehavior.Cascade);
    }
}
