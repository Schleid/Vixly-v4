using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Screens;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsCustomerScreenConfiguration.cs
//
// PURPOSE:
// Maps a customer screen to the database (Slice 5F step 3).
//
// RESPONSIBILITIES:
// - Columns and indexes
//
// IMPORTANT NOTES:
// - The secret and the pairing code are kept only as SHA-256 hashes, each unique when set
// =========================================================
internal sealed class OperationsCustomerScreenConfiguration : IEntityTypeConfiguration<OperationsCustomerScreen>
{
    public void Configure(EntityTypeBuilder<OperationsCustomerScreen> builder)
    {
        builder.ToTable("CustomerScreens");
        builder.HasKey(screen => screen.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(screen => screen.Name).HasMaxLength(OperationsCustomerScreen.NameMaxLength).IsRequired();
        builder.Property(screen => screen.SecretHash).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(screen => screen.PairingCodeHash).HasMaxLength(64).IsUnicode(false);
        builder.Ignore(screen => screen.Paired);
        builder.HasOne<OperationsWarehouse>().WithMany().HasForeignKey(screen => screen.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(screen => new { screen.TenantId, screen.WarehouseId });
        builder.HasIndex(screen => screen.SecretHash).IsUnique().HasFilter("[SecretHash] <> ''");
        builder.HasIndex(screen => screen.PairingCodeHash).IsUnique().HasFilter("[PairingCodeHash] IS NOT NULL");
    }
}
