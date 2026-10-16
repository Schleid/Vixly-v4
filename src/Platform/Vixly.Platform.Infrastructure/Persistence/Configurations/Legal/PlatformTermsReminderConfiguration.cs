using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Legal;

// =========================================================
// FILE: PlatformTermsReminderConfiguration.cs
//
// PURPOSE:
// Maps Terms reminders to platform.TermsReminders.
//
// RESPONSIBILITIES:
// - Each automatic step once per tenant and version (filtered unique index)
//
// IMPORTANT NOTES:
// - Manual reminders may repeat after the cooldown
// =========================================================
internal sealed class PlatformTermsReminderConfiguration : IEntityTypeConfiguration<PlatformTermsReminder>
{
    public void Configure(EntityTypeBuilder<PlatformTermsReminder> builder)
    {
        builder.ToTable("TermsReminders");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Language).HasMaxLength(8).IsUnicode(false).IsRequired();
        builder.Property(item => item.Reason).HasMaxLength(PlatformLegalLimits.ReasonMaxLength);
        builder.HasIndex(item => new { item.PlatformTenantId, item.PlatformLegalDocumentId, item.Step }, "IX_TermsReminders_OncePerStep").IsUnique().HasFilter("[Step] <> 20");
        builder.HasIndex(item => new { item.PlatformTenantId, item.CreatedUtc });
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformLegalDocument>().WithMany().HasForeignKey(item => item.PlatformLegalDocumentId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.SentByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
