using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Customers;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsQuoteConfiguration.cs
//
// PURPOSE:
// Maps OperationsQuote to the database (Slice 4).
//
// RESPONSIBILITIES:
// - Columns, the JSON pieces and lines, indexes and links
//
// IMPORTANT NOTES:
// - One number per company
// =========================================================
internal sealed class OperationsQuoteConfiguration : IEntityTypeConfiguration<OperationsQuote>
{
    public void Configure(EntityTypeBuilder<OperationsQuote> builder)
    {
        builder.ToTable("Quotes");
        builder.HasKey(quote => quote.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(quote => quote.Number).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(quote => quote.ServiceCode).HasMaxLength(OperationsChargeLimits.ServiceCodeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(quote => quote.CountryCode).HasMaxLength(2).IsFixedLength().IsUnicode(false);
        builder.HasOne<Vixly.Operations.Domain.Locations.OperationsWarehouse>().WithMany().HasForeignKey(quote => quote.OriginWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vixly.Operations.Domain.Locations.OperationsWarehouse>().WithMany().HasForeignKey(quote => quote.DestinationWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(quote => quote.CurrencyCode).HasMaxLength(3).IsUnicode(false).IsRequired();
        builder.Property(quote => quote.StandardTotal).HasPrecision(18, 2);
        builder.Property(quote => quote.Total).HasPrecision(18, 2);
        builder.Property(quote => quote.RequestKey).HasMaxLength(64).IsUnicode(false);
        builder.Property(quote => quote.PreferredReason).HasMaxLength(OperationsChargeLimits.ReasonMaxLength);
        builder.Ignore(quote => quote.IsPreferred);
        builder.OwnsMany(quote => quote.Pieces, piece =>
        {
            piece.ToJson("Pieces");
            piece.Property(item => item.WeightKg).HasPrecision(10, 3);
            piece.Property(item => item.LengthCm).HasPrecision(10, 2);
            piece.Property(item => item.WidthCm).HasPrecision(10, 2);
            piece.Property(item => item.HeightCm).HasPrecision(10, 2);
            piece.Property(item => item.DeclaredValue).HasPrecision(12, 2);
        });
        builder.OwnsMany(quote => quote.Lines, line =>
        {
            line.ToJson("Lines");
            line.Property(item => item.Quantity).HasPrecision(14, 2);
            line.Property(item => item.Basis).HasPrecision(14, 2);
            line.Property(item => item.UnitPrice).HasPrecision(14, 4);
            line.Property(item => item.Amount).HasPrecision(18, 2);
        });
        builder.Navigation(quote => quote.Pieces).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(quote => quote.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasIndex(quote => new { quote.TenantId, quote.Number }).IsUnique();
        builder.HasIndex(quote => new { quote.TenantId, quote.RequestKey }).IsUnique().HasFilter("[RequestKey] IS NOT NULL");
        builder.HasIndex(quote => new { quote.TenantId, quote.CustomerId, quote.CreatedUtc });
        builder.HasOne<OperationsCustomer>().WithMany().HasForeignKey(quote => quote.CustomerId).OnDelete(DeleteBehavior.Restrict);
    }
}
