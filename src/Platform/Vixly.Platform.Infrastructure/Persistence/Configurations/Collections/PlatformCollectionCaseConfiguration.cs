using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Collections;

// =========================================================
// FILE: PlatformCollectionCaseConfiguration.cs
//
// PURPOSE:
// Maps collections cases (D-C1).
//
// RESPONSIBILITIES:
// - Map cases to CollectionCases
// - Allow at most one open case per tenant (filtered unique index)
//
// IMPORTANT NOTES:
// - Row version: two hosts never move the same case at once (D-C18)
// =========================================================
internal sealed class PlatformCollectionCaseConfiguration : IEntityTypeConfiguration<PlatformCollectionCase>
{
    public void Configure(EntityTypeBuilder<PlatformCollectionCase> builder)
    {
        builder.ToTable("CollectionCases");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Ignore(item => item.IsOpen);
        builder.Ignore(item => item.IsBlocked);
        builder.Ignore(item => item.ExtendedThrough);
        builder.HasIndex(item => item.PlatformTenantId).IsUnique().HasFilter("[Status] = 1").HasDatabaseName("IX_CollectionCases_OneOpenCasePerTenant");
        builder.HasIndex(item => new { item.Status, item.Stage });
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformSubscription>().WithMany().HasForeignKey(item => item.PlatformSubscriptionId).OnDelete(DeleteBehavior.Restrict);
    }
}
