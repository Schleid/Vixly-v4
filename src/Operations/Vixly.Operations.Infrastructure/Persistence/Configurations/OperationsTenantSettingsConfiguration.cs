using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Infrastructure.Persistence.Configurations;

// =========================================================
// FILE: OperationsTenantSettingsConfiguration.cs
//
// PURPOSE:
// Maps the tenant settings to ops.TenantSettings (Slice 1E).
//
// RESPONSIBILITIES:
// - One row per tenant; prefixes and currency as short ASCII columns
//
// IMPORTANT NOTES:
// - Enums stored as int
// =========================================================
internal sealed class OperationsTenantSettingsConfiguration : IEntityTypeConfiguration<OperationsTenantSettings>
{
    public void Configure(EntityTypeBuilder<OperationsTenantSettings> builder)
    {
        builder.ToTable("TenantSettings");
        builder.HasKey(item => item.Id);
        builder.Property<byte[]>("RowVersion").IsRowVersion();
        builder.Property(item => item.CurrencyCode).HasMaxLength(OperationsSettingsLimits.CurrencyCodeLength).IsUnicode(false).IsFixedLength().IsRequired();
        builder.Property(item => item.ShipmentPrefix).HasMaxLength(OperationsSettingsLimits.PrefixMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.ReceivingPrefix).HasMaxLength(OperationsSettingsLimits.PrefixMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.InvoicePrefix).HasMaxLength(OperationsSettingsLimits.PrefixMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.PaymentPrefix).HasMaxLength(OperationsSettingsLimits.PrefixMaxLength).IsUnicode(false).IsRequired();
        builder.Property(item => item.CustomerPrefix).HasMaxLength(OperationsSettingsLimits.PrefixMaxLength).IsUnicode(false).IsRequired().HasDefaultValue("C");
        builder.Property(item => item.ParcelPrefix).HasMaxLength(OperationsSettingsLimits.PrefixMaxLength).IsUnicode(false).IsRequired().HasDefaultValue("PK");
        builder.Property(item => item.SupplyOrderPrefix).HasMaxLength(OperationsSettingsLimits.PrefixMaxLength).IsUnicode(false).IsRequired().HasDefaultValue("SO");
        builder.Property(item => item.CreditNotePrefix).HasMaxLength(OperationsSettingsLimits.PrefixMaxLength).IsUnicode(false).IsRequired().HasDefaultValue("CN");
        builder.Property(item => item.BundlePrefix).HasMaxLength(OperationsSettingsLimits.PrefixMaxLength).IsUnicode(false).IsRequired().HasDefaultValue("BD");
        builder.Property(item => item.BoxPrefix).HasMaxLength(OperationsSettingsLimits.PrefixMaxLength).IsUnicode(false).IsRequired().HasDefaultValue("CB");
        builder.Property(item => item.DisabledEventCauses).HasMaxLength(400).IsUnicode(false);
        builder.Property(item => item.DestinationStorageFeePerDay).HasPrecision(12, 2);
        builder.Property(item => item.DispatchPrefix).HasMaxLength(OperationsSettingsLimits.PrefixMaxLength).IsUnicode(false).IsRequired().HasDefaultValue("DSP");
        builder.Property(item => item.BundleTypes).HasMaxLength((Vixly.Operations.Domain.Bundles.OperationsBundleLimits.TypeMaxLength + 1) * Vixly.Operations.Domain.Bundles.OperationsBundleLimits.MaxTypes);
        builder.Property(item => item.TaxName).HasMaxLength(Vixly.Operations.Domain.Billing.OperationsBillingLimits.TaxNameMaxLength);
        builder.Property(item => item.TaxNumber).HasMaxLength(Vixly.Operations.Domain.Billing.OperationsBillingLimits.TaxNumberMaxLength);
        builder.Property(item => item.FreightTaxPercent).HasPrecision(6, 3);
        builder.Property(item => item.InsuranceTaxPercent).HasPrecision(6, 3);
        builder.Property(item => item.SuppliesTaxPercent).HasPrecision(6, 3);
        builder.Property(item => item.ChargesTaxPercent).HasPrecision(6, 3);
        builder.Property(item => item.InvoiceNoteEn).HasMaxLength(Vixly.Operations.Domain.Billing.OperationsBillingLimits.NoteMaxLength);
        builder.Property(item => item.InvoiceNoteFr).HasMaxLength(Vixly.Operations.Domain.Billing.OperationsBillingLimits.NoteMaxLength);
        builder.Property(item => item.InvoiceNoteEs).HasMaxLength(Vixly.Operations.Domain.Billing.OperationsBillingLimits.NoteMaxLength);
        builder.Ignore(item => item.TaxRates);
        builder.Property(item => item.OtherMethod1).HasMaxLength(30);
        builder.Property(item => item.OtherMethod2).HasMaxLength(30);
        builder.Property(item => item.OtherMethod3).HasMaxLength(30);
        builder.Property(item => item.ZelleQrKey).HasMaxLength(300);
        builder.Property(item => item.ZelleTag).HasMaxLength(120);
        builder.Property(item => item.ZelleContact).HasMaxLength(120);
        builder.Property(item => item.ZelleName).HasMaxLength(120);
        builder.Property(item => item.ZelleTermsEn).HasMaxLength(2000);
        builder.Property(item => item.ZelleTermsFr).HasMaxLength(2000);
        builder.Property(item => item.ZelleTermsEs).HasMaxLength(2000);
        builder.Property(item => item.SupplyDiscountLinePercent).HasPrecision(5, 2);
        builder.Property(item => item.SupplyDiscountOrderPercent).HasPrecision(5, 2);
        builder.Property(item => item.InspectionPercent).HasDefaultValue(OperationsTenantSettings.DefaultInspectionPercent).HasSentinel(-1);
        builder.Property(item => item.UnclaimedDays).HasDefaultValue(Vixly.Operations.Domain.Holds.OperationsHoldLimits.DefaultUnclaimedDays).HasSentinel(-1);

        // Slice 4D step 3: free days and the storage fee per day (none by default).
        builder.Property(item => item.StorageFreeDays).HasDefaultValue(Vixly.Operations.Domain.Holds.OperationsHoldLimits.DefaultUnclaimedDays).HasSentinel(-1);
        builder.Property(item => item.StorageFeePerDay).HasPrecision(12, 2);
        builder.Property(item => item.TimeZone).HasMaxLength(Vixly.Operations.Domain.Locations.OperationsTimeZones.MaxLength).IsUnicode(false);
        builder.HasIndex(item => item.TenantId).IsUnique();
    }
}
