using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Infrastructure.Persistence.Conversions;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Billing;

// =========================================================
// FILE: PlatformBillingDocumentConfiguration.cs
//
// PURPOSE:
// Maps stored PDFs (D-B10).
//
// RESPONSIBILITIES:
// - Map the entity to its table, columns and indexes
//
// IMPORTANT NOTES:
// - Written once; one document per invoice, credit note or receipt
// =========================================================
internal sealed class PlatformBillingDocumentConfiguration : IEntityTypeConfiguration<PlatformBillingDocument>
{
    public void Configure(EntityTypeBuilder<PlatformBillingDocument> builder)
    {
        builder.ToTable("BillingDocuments");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Number).HasMaxLength(PlatformBillingLimits.NumberMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.Language).HasConversion(PlatformValueConverters.Language).HasMaxLength(8).IsUnicode(false).IsRequired();
        builder.Property(item => item.Sha256).HasMaxLength(64).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(item => item.Content).IsRequired();
        builder.HasIndex(item => new { item.Kind, item.OwnerId }).IsUnique();
        builder.HasIndex(item => item.PlatformInvoiceId);
        builder.HasOne<PlatformInvoice>().WithMany().HasForeignKey(item => item.PlatformInvoiceId).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(item => item.FileName);
    }
}
