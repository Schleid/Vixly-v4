using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Tenants;

// =========================================================
// FILE: PlatformTenantNoteConfiguration.cs
//
// PURPOSE:
// Maps internal tenant notes to platform.TenantNotes.
//
// RESPONSIBILITIES:
// - Body, author and time; newest first per tenant
//
// IMPORTANT NOTES:
// - Append-only (D-T16)
// =========================================================
internal sealed class PlatformTenantNoteConfiguration : IEntityTypeConfiguration<PlatformTenantNote>
{
    public void Configure(EntityTypeBuilder<PlatformTenantNote> builder)
    {
        builder.ToTable("TenantNotes");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Body).HasMaxLength(PlatformTenantLimits.NoteMaxLength).IsRequired();
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.AuthorUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.PlatformTenantId, item.CreatedUtc });
    }
}
