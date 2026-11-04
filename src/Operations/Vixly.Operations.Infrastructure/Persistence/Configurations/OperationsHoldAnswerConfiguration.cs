using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsHoldAnswerConfiguration.cs
//
// PURPOSE:
// Maps OperationsHoldAnswer to the database (Slice 3E).
//
// RESPONSIBILITIES:
// - Columns, indexes and links
//
// IMPORTANT NOTES:
// - One open answer per parcel and hold (filtered unique index); a row version so two reviews clash
// =========================================================
internal sealed class OperationsHoldAnswerConfiguration : IEntityTypeConfiguration<OperationsHoldAnswer>
{
    public void Configure(EntityTypeBuilder<OperationsHoldAnswer> builder)
    {
        builder.ToTable("HoldAnswers");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Contents).HasMaxLength(OperationsReceivingLimits.ContentsMaxLength);
        builder.Property(item => item.DeclaredValue).HasPrecision(12, 2);
        builder.Property(item => item.CurrencyCode).HasMaxLength(3).IsUnicode(false);
        builder.Property(item => item.Choice).HasMaxLength(20).IsUnicode(false);
        builder.Property(item => item.Note).HasMaxLength(OperationsHoldLimits.NoteMaxLength);
        builder.Property(item => item.DocumentKey).HasMaxLength(OperationsHoldLimits.StorageKeyMaxLength).IsUnicode(false);
        builder.Property(item => item.DocumentType).HasMaxLength(40).IsUnicode(false);
        builder.Property(item => item.DocumentName).HasMaxLength(OperationsHoldLimits.FileNameMaxLength);
        builder.Property(item => item.DocumentSha256).HasMaxLength(64).IsUnicode(false);
        builder.Property(item => item.ReviewNote).HasMaxLength(OperationsHoldLimits.NoteMaxLength);
        builder.HasIndex(item => new { item.TenantId, item.ParcelId, item.Reason }).IsUnique().HasFilter("[Status] = 0").HasDatabaseName("IX_HoldAnswers_TenantId_ParcelId_Reason_Open");
        builder.HasIndex(item => new { item.TenantId, item.ParcelId, item.CreatedUtc });
        builder.HasOne<OperationsParcel>().WithMany().HasForeignKey(item => item.ParcelId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsCustomer>().WithMany().HasForeignKey(item => item.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
