using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Persistence.Conversions;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Tenants;

// =========================================================
// FILE: PlatformTenantConfiguration.cs
//
// PURPOSE:
// Maps tenants to platform.Tenants with their contacts, tags and
// suspensions.
//
// RESPONSIBILITIES:
// - Unique tenant code and current slug
// - Company details as columns of the tenant row
// - Owned contacts, tags and suspensions in their own tables
// - Row version for optimistic concurrency
//
// IMPORTANT NOTES:
// - Company details are a complex type: replacing them updates
//   the row in place
// - The account manager and creator point at platform.Users
//   (no cascade)
// =========================================================
internal sealed class PlatformTenantConfiguration : IEntityTypeConfiguration<PlatformTenant>
{
    public void Configure(EntityTypeBuilder<PlatformTenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(tenant => tenant.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();

        builder.Property(tenant => tenant.TenantCode)
            .HasMaxLength(PlatformTenantLimits.TenantCodeMaxLength)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(tenant => tenant.CurrentSlug)
            .HasMaxLength(PlatformTenantLimits.SlugMaxLength)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(tenant => tenant.DefaultLanguage)
            .HasConversion(PlatformValueConverters.Language)
            .HasMaxLength(8)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(tenant => tenant.TimeZone)
            .HasMaxLength(PlatformTenantLimits.TimeZoneMaxLength)
            .IsUnicode(false)
            .IsRequired();

        builder.ComplexProperty(item => item.Company, company =>
        {
            company.Property(value => value.LegalName)
                .HasColumnName("LegalName")
                .HasMaxLength(PlatformTenantLimits.CompanyNameMaxLength)
                .IsRequired();
            company.Property(value => value.TradingName)
                .HasColumnName("TradingName")
                .HasMaxLength(PlatformTenantLimits.CompanyNameMaxLength);
            company.Property(value => value.Email)
                .HasColumnName("CompanyEmail")
                .HasConversion(PlatformValueConverters.Email)
                .HasMaxLength(EmailAddress.MaxLength)
                .IsRequired();
            company.Property(value => value.Phone)
                .HasColumnName("CompanyPhone")
                .HasConversion(PlatformValueConverters.RequiredPhone)
                .HasMaxLength(PhoneNumber.MaximumDigits + 1)
                .IsUnicode(false)
                .IsRequired();
            company.ComplexProperty(value => value.Address, address =>
            {
                address.Property(item => item.Line1).HasColumnName("AddressLine1").HasMaxLength(PlatformTenantLimits.AddressLineMaxLength).IsRequired();
                address.Property(item => item.Line2).HasColumnName("AddressLine2").HasMaxLength(PlatformTenantLimits.AddressLineMaxLength);
                address.Property(item => item.City).HasColumnName("City").HasMaxLength(PlatformTenantLimits.CityMaxLength).IsRequired();
                address.Property(item => item.StateProvince).HasColumnName("StateProvince").HasMaxLength(PlatformTenantLimits.StateProvinceMaxLength);
                address.Property(item => item.PostalCode).HasColumnName("PostalCode").HasMaxLength(PlatformTenantLimits.PostalCodeMaxLength);
                address.Property(item => item.CountryCode)
                    .HasColumnName("CountryCode")
                    .HasMaxLength(PlatformTenantLimits.CountryCodeLength)
                    .IsFixedLength()
                    .IsUnicode(false)
                    .IsRequired();
            });
        });

        builder.Property(tenant => tenant.TaxId).HasMaxLength(Vixly.Platform.Domain.Billing.PlatformBillingLimits.TaxIdMaxLength);
        builder.Property(tenant => tenant.TaxExemptionReason).HasMaxLength(Vixly.Platform.Domain.Billing.PlatformBillingLimits.ReasonMaxLength);
        builder.Property(tenant => tenant.TaxExemptionReference).HasMaxLength(Vixly.Platform.Domain.Billing.PlatformBillingLimits.ReferenceMaxLength);

        builder.HasIndex(tenant => tenant.TenantCode).IsUnique();
        builder.HasIndex(tenant => tenant.CurrentSlug).IsUnique();
        builder.HasIndex(tenant => new { tenant.Status, tenant.CreatedUtc });
        builder.HasIndex(tenant => tenant.AccountManagerUserId);

        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(tenant => tenant.AccountManagerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(tenant => tenant.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(tenant => tenant.OpenSuspension);
        builder.Ignore(tenant => tenant.HasBeenActive);
        builder.Ignore(tenant => tenant.CanCorrectSlug);

        builder.OwnsMany(tenant => tenant.Contacts, contact =>
        {
            contact.ToTable("TenantContacts");
            contact.WithOwner().HasForeignKey(item => item.PlatformTenantId);
            contact.HasKey(item => item.Id);
            contact.Property(item => item.Name).HasMaxLength(PlatformTenantLimits.ContactNameMaxLength).IsRequired();
            contact.Property(item => item.Email)
                .HasConversion(PlatformValueConverters.Email)
                .HasMaxLength(EmailAddress.MaxLength)
                .IsRequired();
            contact.Property(item => item.Phone)
                .HasConversion(PlatformValueConverters.Phone)
                .HasMaxLength(PhoneNumber.MaximumDigits + 1)
                .IsUnicode(false);
            contact.HasIndex(item => new { item.PlatformTenantId, item.Role }).IsUnique();
            contact.HasIndex(item => item.Email);
        });
        builder.Navigation(tenant => tenant.Contacts).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(tenant => tenant.Tags, tag =>
        {
            tag.ToTable("TenantTags");
            tag.WithOwner().HasForeignKey(item => item.PlatformTenantId);
            tag.HasKey(item => item.Id);
            tag.Property(item => item.Value).HasMaxLength(PlatformTenantLimits.TagMaxLength).IsUnicode(false).IsRequired();
            tag.HasIndex(item => new { item.PlatformTenantId, item.Value }).IsUnique();
            tag.HasIndex(item => item.Value);
        });
        builder.Navigation(tenant => tenant.Tags).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(tenant => tenant.Suspensions, suspension =>
        {
            suspension.ToTable("TenantSuspensions");
            suspension.WithOwner().HasForeignKey(item => item.PlatformTenantId);
            suspension.HasKey(item => item.Id);
            suspension.Property(item => item.Reason).HasMaxLength(PlatformTenantLimits.ReasonMaxLength).IsRequired();
            suspension.Property(item => item.ResolutionReason).HasMaxLength(PlatformTenantLimits.ReasonMaxLength);
            suspension.Ignore(item => item.IsOpen);
            suspension.HasIndex(item => new { item.PlatformTenantId, item.ResolvedUtc });
        });
        builder.Navigation(tenant => tenant.Suspensions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
