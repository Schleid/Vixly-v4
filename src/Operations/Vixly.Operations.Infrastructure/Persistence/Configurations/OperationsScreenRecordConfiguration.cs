using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Screens;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsScreenRecordConfiguration.cs
//
// PURPOSE:
// Maps what a customer accepted or signed on a customer screen to the database (Slice 5F step 3).
//
// RESPONSIBILITIES:
// - Columns and indexes
//
// IMPORTANT NOTES:
// - Kept when the screen is removed (the screen's name is copied); never updated
// =========================================================
internal sealed class OperationsScreenRecordConfiguration : IEntityTypeConfiguration<OperationsScreenRecord>
{
    public void Configure(EntityTypeBuilder<OperationsScreenRecord> builder)
    {
        builder.ToTable("ScreenRecords");
        builder.HasKey(record => record.Id);
        builder.Property(record => record.ScreenName).HasMaxLength(OperationsCustomerScreen.NameMaxLength).IsRequired();
        builder.Property(record => record.Language).HasMaxLength(10).IsUnicode(false).IsRequired();
        builder.Property(record => record.TermsHash).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.HasIndex(record => new { record.TenantId, record.InvoiceId }).HasFilter("[InvoiceId] IS NOT NULL");
    }
}
