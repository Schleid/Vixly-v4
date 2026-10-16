using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Persistence.Conversions;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Billing;

// =========================================================
// FILE: PlatformCompanyProfileConfiguration.cs
//
// PURPOSE:
// Maps Vixly's company profile (D-B12).
//
// RESPONSIBILITIES:
// - Map the entity to its table, columns and indexes
//
// IMPORTANT NOTES:
// - Single row keyed "default"; row version
// - The logo is kept in the row (512 KB at most)
// =========================================================
internal sealed class PlatformCompanyProfileConfiguration : IEntityTypeConfiguration<PlatformCompanyProfile>
{
    public void Configure(EntityTypeBuilder<PlatformCompanyProfile> builder)
    {
        builder.ToTable("CompanyProfile");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Key).HasMaxLength(16).IsUnicode(false).IsRequired();
        builder.HasIndex(item => item.Key).IsUnique();
        builder.Property(item => item.TaxId).HasMaxLength(PlatformBillingLimits.TaxIdMaxLength);
        builder.Property(item => item.Website).HasMaxLength(PlatformBillingLimits.WebsiteMaxLength);
        builder.Property(item => item.SupportEmail).HasMaxLength(254);
        builder.Property(item => item.SupportPhone).HasMaxLength(32).IsUnicode(false);
        builder.Property(item => item.LogoContentType).HasMaxLength(16).IsUnicode(false);
        builder.HasOne<Vixly.Platform.Domain.Identity.PlatformUser>().WithMany().HasForeignKey(item => item.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.ComplexProperty(item => item.Company, company =>
        {
            company.Property(value => value.LegalName).HasColumnName("LegalName").HasMaxLength(PlatformTenantLimits.CompanyNameMaxLength).IsRequired();
            company.Property(value => value.TradingName).HasColumnName("TradingName").HasMaxLength(PlatformTenantLimits.CompanyNameMaxLength);
            company.Property(value => value.Email).HasColumnName("Email").HasConversion(PlatformValueConverters.Email).HasMaxLength(EmailAddress.MaxLength).IsRequired();
            company.Property(value => value.Phone).HasColumnName("Phone").HasConversion(PlatformValueConverters.RequiredPhone).HasMaxLength(PhoneNumber.MaximumDigits + 1).IsUnicode(false).IsRequired();
            company.ComplexProperty(value => value.Address, address =>
            {
                address.Property(item => item.Line1).HasColumnName("AddressLine1").HasMaxLength(PlatformTenantLimits.AddressLineMaxLength).IsRequired();
                address.Property(item => item.Line2).HasColumnName("AddressLine2").HasMaxLength(PlatformTenantLimits.AddressLineMaxLength);
                address.Property(item => item.City).HasColumnName("City").HasMaxLength(PlatformTenantLimits.CityMaxLength).IsRequired();
                address.Property(item => item.StateProvince).HasColumnName("StateProvince").HasMaxLength(PlatformTenantLimits.StateProvinceMaxLength);
                address.Property(item => item.PostalCode).HasColumnName("PostalCode").HasMaxLength(PlatformTenantLimits.PostalCodeMaxLength);
                address.Property(item => item.CountryCode).HasColumnName("CountryCode").HasMaxLength(PlatformTenantLimits.CountryCodeLength).IsFixedLength().IsUnicode(false).IsRequired();
            });
        });
    }
}
