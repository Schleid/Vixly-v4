using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Delivery;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsDriverPositionConfiguration.cs
//
// PURPOSE:
// Maps driver positions (Slice 12C) to ops.DriverPositions.
//
// RESPONSIBILITIES:
// - Six decimals for the place; read by route and time; deleted by time
//
// IMPORTANT NOTES:
// - The route's foreign key cascades: a position never outlives its route
// =========================================================
internal sealed class OperationsDriverPositionConfiguration : IEntityTypeConfiguration<OperationsDriverPosition>
{
    public void Configure(EntityTypeBuilder<OperationsDriverPosition> builder)
    {
        builder.ToTable("DriverPositions");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Latitude).HasPrecision(9, 6);
        builder.Property(item => item.Longitude).HasPrecision(9, 6);
        builder.HasIndex(item => new { item.TenantId, item.RouteId, item.TakenUtc });
        builder.HasIndex(item => item.TakenUtc);
        builder.HasOne<OperationsDeliveryRoute>().WithMany().HasForeignKey(item => item.RouteId).OnDelete(DeleteBehavior.Cascade);
    }
}
