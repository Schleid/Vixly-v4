using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Infrastructure.Persistence.Conversions;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Collections;

// =========================================================
// FILE: PlatformCollectionCaseEventConfiguration.cs
//
// PURPOSE:
// Maps the history of collections cases (D-C15).
//
// RESPONSIBILITIES:
// - Map history to CollectionCaseEvents
// - Make a card retry unique per invoice and day (filtered unique RetryKey)
//
// IMPORTANT NOTES:
// - Append-only (DENY UPDATE, DELETE in the global script)
// =========================================================
internal sealed class PlatformCollectionCaseEventConfiguration : IEntityTypeConfiguration<PlatformCollectionCaseEvent>
{
    public void Configure(EntityTypeBuilder<PlatformCollectionCaseEvent> builder)
    {
        builder.ToTable("CollectionCaseEvents");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.EventCode).HasMaxLength(100).IsUnicode(false).IsRequired();
        builder.Property(item => item.Note).HasMaxLength(PlatformCollectionsLimits.NoteMaxLength);
        builder.Property(item => item.RetryKey).HasMaxLength(PlatformCollectionsLimits.RetryKeyMaxLength).IsUnicode(false);
        builder.HasIndex(item => item.RetryKey).IsUnique().HasFilter("[RetryKey] IS NOT NULL");
        builder.Property(item => item.Arguments).HasConversion(PlatformValueConverters.Arguments, PlatformValueConverters.ArgumentsComparer).IsRequired();
        builder.HasOne<PlatformCollectionCase>().WithMany().HasForeignKey(item => item.PlatformCollectionCaseId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vixly.Platform.Domain.Tenants.PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Vixly.Platform.Domain.Identity.PlatformUser>().WithMany().HasForeignKey(item => item.ActorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.PlatformCollectionCaseId, item.CreatedUtc });
    }
}
