using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Tenants;
using Vixly.Platform.Infrastructure.Persistence.Conversions;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Persistence.Configurations.Tenants;

// =========================================================
// FILE: PlatformTenantChangeRequestConfiguration.cs
//
// PURPOSE:
// Maps company change requests to platform.TenantChangeRequests (B12).
//
// RESPONSIBILITIES:
// - Map the details before (Previous*) and asked for (Proposed*) as columns
// - One pending request per tenant (filtered unique index)
// - The proof lives in its own table, one row per request
//
// IMPORTANT NOTES:
// - Concurrency by row version: two staff cannot both decide one request
// =========================================================
internal sealed class PlatformTenantChangeRequestConfiguration : IEntityTypeConfiguration<PlatformTenantChangeRequest>
{
    public void Configure(EntityTypeBuilder<PlatformTenantChangeRequest> builder)
    {
        builder.ToTable("TenantChangeRequests");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.Reason).HasMaxLength(PlatformTenantChangeLimits.ReasonMaxLength).IsRequired();
        builder.Property(item => item.SubmittedByReference).HasMaxLength(PlatformTenantChangeLimits.ActorMaxLength).IsRequired();
        builder.Property(item => item.SubmittedByName).HasMaxLength(PlatformTenantChangeLimits.ActorMaxLength).IsRequired();
        builder.Property(item => item.DecisionNote).HasMaxLength(PlatformTenantChangeLimits.NoteMaxLength);
        builder.Ignore(item => item.IsPending);

        builder.ComplexProperty(item => item.Previous, company => Company(company, "Previous"));
        builder.ComplexProperty(item => item.Proposed, company => Company(company, "Proposed"));

        builder.HasOne(item => item.Proof)
            .WithOne()
            .HasForeignKey<PlatformTenantChangeProof>(proof => proof.PlatformTenantChangeRequestId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(item => item.PlatformTenantId, "IX_TenantChangeRequests_OnePending").IsUnique().HasFilter("[Status] = 1");
        builder.HasIndex(item => new { item.Status, item.CreatedUtc });
        builder.HasIndex(item => new { item.PlatformTenantId, item.CreatedUtc });
        builder.HasOne<PlatformTenant>().WithMany().HasForeignKey(item => item.PlatformTenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PlatformUser>().WithMany().HasForeignKey(item => item.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    private static void Company(ComplexPropertyBuilder<PlatformCompanyDetails> company, string prefix)
    {
        company.Property(value => value.LegalName).HasColumnName(prefix + "LegalName").HasMaxLength(PlatformTenantLimits.CompanyNameMaxLength).IsRequired();
        company.Property(value => value.TradingName).HasColumnName(prefix + "TradingName").HasMaxLength(PlatformTenantLimits.CompanyNameMaxLength);
        company.Property(value => value.Email)
            .HasColumnName(prefix + "Email")
            .HasConversion(PlatformValueConverters.Email)
            .HasMaxLength(EmailAddress.MaxLength)
            .IsRequired();
        company.Property(value => value.Phone)
            .HasColumnName(prefix + "Phone")
            .HasConversion(PlatformValueConverters.RequiredPhone)
            .HasMaxLength(PhoneNumber.MaximumDigits + 1)
            .IsUnicode(false)
            .IsRequired();
        company.ComplexProperty(value => value.Address, address =>
        {
            address.Property(item => item.Line1).HasColumnName(prefix + "AddressLine1").HasMaxLength(PlatformTenantLimits.AddressLineMaxLength).IsRequired();
            address.Property(item => item.Line2).HasColumnName(prefix + "AddressLine2").HasMaxLength(PlatformTenantLimits.AddressLineMaxLength);
            address.Property(item => item.City).HasColumnName(prefix + "City").HasMaxLength(PlatformTenantLimits.CityMaxLength).IsRequired();
            address.Property(item => item.StateProvince).HasColumnName(prefix + "StateProvince").HasMaxLength(PlatformTenantLimits.StateProvinceMaxLength);
            address.Property(item => item.PostalCode).HasColumnName(prefix + "PostalCode").HasMaxLength(PlatformTenantLimits.PostalCodeMaxLength);
            address.Property(item => item.CountryCode)
                .HasColumnName(prefix + "CountryCode")
                .HasMaxLength(PlatformTenantLimits.CountryCodeLength)
                .IsFixedLength()
                .IsUnicode(false)
                .IsRequired();
        });
    }
}
