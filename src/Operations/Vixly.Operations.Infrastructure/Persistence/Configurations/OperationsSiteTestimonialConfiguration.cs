using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsSiteTestimonialConfiguration.cs
//
// PURPOSE:
// Maps customer testimonials to ops.SiteTestimonials (owner, 2026-10-30).
//
// RESPONSIBILITIES:
// - Lengths, the consent columns and the order index
//
// IMPORTANT NOTES:
// - Tenant filter and audit columns come from the model conventions
// =========================================================
internal sealed class OperationsSiteTestimonialConfiguration : IEntityTypeConfiguration<OperationsSiteTestimonial>
{
    public void Configure(EntityTypeBuilder<OperationsSiteTestimonial> builder)
    {
        builder.ToTable("SiteTestimonials");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Name).HasMaxLength(OperationsSiteTestimonial.NameMaxLength).IsRequired();
        builder.Property(item => item.Place).HasMaxLength(OperationsSiteTestimonial.PlaceMaxLength);
        builder.Property(item => item.Quote).HasMaxLength(OperationsSiteTestimonial.QuoteMaxLength).IsRequired();
        builder.Property(item => item.Source).HasMaxLength(OperationsSiteTestimonial.SourceMaxLength);
        builder.HasIndex(item => new { item.TenantId, item.SortOrder });

        // Customer reviews (owner, 2026-11-03): one per customer; new ones are counted for the badge.
        builder.HasIndex(item => new { item.TenantId, item.CustomerId }).IsUnique().HasFilter("[CustomerId] IS NOT NULL").HasDatabaseName("IX_SiteTestimonials_TenantId_CustomerId_One");
        builder.HasIndex(item => new { item.TenantId, item.ReviewedUtc });
        builder.Property(item => item.NameStyle).HasConversion<byte?>();
        builder.Property(item => item.HiddenReason).HasConversion<byte?>();
        builder.Ignore(item => item.FromCustomer);
        builder.Ignore(item => item.IsNew);
        builder.Ignore(item => item.MayBePublished);
    }
}
