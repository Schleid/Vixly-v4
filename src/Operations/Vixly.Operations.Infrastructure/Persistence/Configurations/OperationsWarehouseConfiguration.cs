using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsWarehouseConfiguration.cs
//
// PURPOSE:
// Maps warehouses to ops.Warehouses (Slice 1B).
//
// RESPONSIBILITIES:
// - Code unique per tenant
// - Address columns prefixed Address_
//
// IMPORTANT NOTES:
// - Row version guards concurrent edits
// =========================================================
internal sealed class OperationsWarehouseConfiguration : IEntityTypeConfiguration<OperationsWarehouse>
{
    public void Configure(EntityTypeBuilder<OperationsWarehouse> builder)
    {
        builder.ToTable("Warehouses");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Code).HasMaxLength(OperationsLocationLimits.CodeMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.Name).HasMaxLength(OperationsLocationLimits.NameMaxLength).IsRequired();
        builder.Property(item => item.Phone).HasMaxLength(OperationsLocationLimits.PhoneMaxLength).IsUnicode(false);
        builder.Property(item => item.Email).HasMaxLength(OperationsLocationLimits.EmailMaxLength);
        builder.Property(item => item.OpeningHours).HasMaxLength(OperationsLocationLimits.HoursMaxLength);
        builder.Ignore(item => item.IsPublic);
        builder.HasIndex(item => new { item.TenantId, item.Code }).IsUnique();
        builder.OwnsOne(item => item.Address, OperationsAddressMapping.Map);
        builder.Navigation(item => item.Address).IsRequired();
        builder.HasOne<OperationsBranch>().WithMany().HasForeignKey(item => item.BranchId).OnDelete(DeleteBehavior.Restrict);
        builder.Property(item => item.TimeZone).HasMaxLength(OperationsTimeZones.MaxLength).IsUnicode(false);
        builder.HasOne<Vixly.Operations.Domain.Identity.OperationsUser>().WithMany().HasForeignKey(item => item.ManagerUserId).OnDelete(DeleteBehavior.Restrict);

        // Slice 4C: as a destination.
        builder.Ignore(item => item.IsDestination);
        builder.Ignore(item => item.IsOrigin);
        builder.Ignore(item => item.HandlesMoney);
        // The customers' mailbox (owner, 2026-10-21): one per company.
        builder.Ignore(item => item.CanBeMailbox);
        builder.HasIndex(item => new { item.TenantId, item.IsMailbox }).IsUnique().HasFilter("[IsMailbox] = 1").HasDatabaseName("IX_Warehouses_TenantId_Mailbox");
        builder.Ignore(item => item.HomeArea);
        builder.PrimitiveCollection<List<string>>("_homeArea").HasColumnName("HomeArea");
        builder.Property(item => item.OperatedBy).HasDefaultValue(OperationsDestinationOperator.Company).HasSentinel((OperationsDestinationOperator)0);
        builder.Property(item => item.PartnerName).HasMaxLength(OperationsLocationLimits.NameMaxLength);
        builder.Property(item => item.PartnerPhone).HasMaxLength(OperationsLocationLimits.PhoneMaxLength).IsUnicode(false);
        builder.Property(item => item.PartnerEmail).HasMaxLength(OperationsLocationLimits.EmailMaxLength);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(item => item.HubWarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}
