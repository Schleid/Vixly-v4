using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Persistence.Conversions;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Tenants;

// =========================================================
// FILE: PlatformTenantRegistrationConfiguration.cs
//
// PURPOSE:
// Maps self-service registrations to platform.TenantRegistrations.
//
// RESPONSIBILITIES:
// - Company details as columns, owner and consent fields
// - Unique access-token hash
// - Row version for optimistic concurrency
//
// IMPORTANT NOTES:
// - Only hashes are stored (fixed 64 hex characters)
// =========================================================
internal sealed class PlatformTenantRegistrationConfiguration : IEntityTypeConfiguration<PlatformTenantRegistration>
{
    public void Configure(EntityTypeBuilder<PlatformTenantRegistration> builder)
    {
        builder.ToTable("TenantRegistrations");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();

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

        builder.Property(item => item.OwnerName).HasMaxLength(PlatformTenantLimits.ContactNameMaxLength).IsRequired();
        builder.Property(item => item.OwnerEmail)
            .HasConversion(PlatformValueConverters.Email)
            .HasMaxLength(EmailAddress.MaxLength)
            .IsRequired();
        builder.Property(item => item.OwnerPhone)
            .HasConversion(PlatformValueConverters.RequiredPhone)
            .HasMaxLength(PhoneNumber.MaximumDigits + 1)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(item => item.Language)
            .HasConversion(PlatformValueConverters.Language)
            .HasMaxLength(8)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(item => item.TimeZone).HasMaxLength(PlatformTenantLimits.TimeZoneMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.Slug).HasMaxLength(PlatformTenantLimits.SlugMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.AccessTokenHash)
            .HasMaxLength(PlatformIdentityLimits.TokenHashLength)
            .IsFixedLength()
            .IsUnicode(false)
            .IsRequired();
        builder.Property(item => item.TermsVersion).HasMaxLength(PlatformTenantLimits.LegalVersionMaxLength).IsRequired();
        builder.Property(item => item.PrivacyVersion).HasMaxLength(PlatformTenantLimits.LegalVersionMaxLength).IsRequired();
        builder.Property(item => item.ConsentIpAddress).HasMaxLength(PlatformTenantLimits.IpAddressMaxLength).IsUnicode(false);
        builder.Property(item => item.CancellationReason).HasMaxLength(PlatformTenantLimits.ReasonMaxLength);

        builder.Ignore(item => item.IsClosed);
        builder.Ignore(item => item.VerificationDeadlineUtc);

        builder.HasIndex(item => item.AccessTokenHash).IsUnique();
        builder.HasIndex(item => new { item.Status, item.CreatedUtc });
        builder.HasIndex(item => item.OwnerEmail);
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.CancelledByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
