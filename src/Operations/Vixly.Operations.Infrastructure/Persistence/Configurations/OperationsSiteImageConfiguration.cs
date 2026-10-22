using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsSiteImageConfiguration.cs
//
// PURPOSE:
// Maps site images to ops.SiteImages (Slice 1B; images in the database, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Store the bytes with their type, size and hash
//
// IMPORTANT NOTES:
// - Rows are never changed
// =========================================================
internal sealed class OperationsSiteImageConfiguration : IEntityTypeConfiguration<OperationsSiteImage>
{
    public void Configure(EntityTypeBuilder<OperationsSiteImage> builder)
    {
        builder.ToTable("SiteImages");
        builder.HasKey(image => image.Id);
        builder.Property(image => image.ContentType).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(image => image.Content).HasMaxLength(OperationsSiteImage.MaxBytes).IsRequired();
        builder.Property(image => image.Sha256).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
    }
}
