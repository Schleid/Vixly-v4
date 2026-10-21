using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsDispatchConfiguration.cs
//
// PURPOSE:
// Maps dispatches to ops.Dispatches, with their legs as JSON (Slice 6B).
//
// RESPONSIBILITIES:
// - Number, route, service, state and times
// - Legs in one JSON column, changed only through the dispatch
//
// IMPORTANT NOTES:
// - The row version guards the bundle count and the legs
// =========================================================
internal sealed class OperationsDispatchConfiguration : IEntityTypeConfiguration<OperationsDispatch>
{
    public void Configure(EntityTypeBuilder<OperationsDispatch> builder)
    {
        builder.ToTable("Dispatches");
        builder.HasKey(dispatch => dispatch.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(dispatch => dispatch.Number).HasMaxLength(40).IsUnicode(false).IsRequired();
        builder.Property(dispatch => dispatch.DestinationCountryCode).HasMaxLength(2).IsFixedLength().IsUnicode(false).IsRequired();
        builder.Property(dispatch => dispatch.ServiceCode).HasMaxLength(Vixly.Operations.Domain.Charges.OperationsChargeLimits.ServiceCodeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(dispatch => dispatch.Notes).HasMaxLength(OperationsDispatch.NotesMaxLength);
        builder.Property(dispatch => dispatch.CancelReason).HasMaxLength(300);
        builder.Property(dispatch => dispatch.CostComputed).HasPrecision(14, 2);
        builder.Property(dispatch => dispatch.CostTotal).HasPrecision(14, 2);
        builder.Property(dispatch => dispatch.CostNote).HasMaxLength(OperationsDispatch.CostNoteMaxLength);
        builder.Property(dispatch => dispatch.CostCurrencyCode).HasMaxLength(3).IsUnicode(false);
        builder.Property(dispatch => dispatch.ExportReference).HasMaxLength(OperationsDispatch.ExportReferenceMaxLength);
        builder.Property(dispatch => dispatch.HandlingInstructions).HasMaxLength(OperationsDispatch.InstructionsMaxLength);
        builder.OwnsMany(dispatch => dispatch.Legs, leg =>
        {
            leg.ToJson("Legs");
            leg.Property(item => item.From).HasMaxLength(OperationsDispatchLeg.PlaceMaxLength);
            leg.Property(item => item.To).HasMaxLength(OperationsDispatchLeg.PlaceMaxLength);
        });
        builder.Navigation(dispatch => dispatch.Legs).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(dispatch => dispatch.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(dispatch => dispatch.DestinationWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(dispatch => new { dispatch.TenantId, dispatch.Number }).IsUnique();
        builder.HasIndex(dispatch => new { dispatch.TenantId, dispatch.RequestKey }).IsUnique().HasFilter("[RequestKey] IS NOT NULL");
        builder.HasIndex(dispatch => new { dispatch.TenantId, dispatch.WarehouseId, dispatch.Status });

        // Slice 7: the warehouse that receives it.
        builder.HasOne<Vixly.Operations.Domain.Locations.OperationsWarehouse>().WithMany().HasForeignKey(dispatch => dispatch.ReceivingWarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(dispatch => new { dispatch.TenantId, dispatch.ReceivingWarehouseId, dispatch.Status });
    }
}
