using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Settings;

// =========================================================
// FILE: OperationsTenantSettings.cs
//
// PURPOSE:
// The tenant's working settings (Slice 1E; design section 2).
//
// RESPONSIBILITIES:
// - Billing currency, display units, payment gate, delivery modes, staff security and number prefixes
// - Refuse a currency change once an invoice exists (ADR-0018)
//
// IMPORTANT NOTES:
// - One row per tenant; defaults until the first save
// - Weights and dimensions are always stored in kilograms and centimetres (D-O8)
// =========================================================
public sealed class OperationsTenantSettings : OperationsEntity
{
    public const string DefaultCurrency = "USD";

    private OperationsTenantSettings()
    {
        CurrencyCode = DefaultCurrency;
        ShipmentPrefix = "SH";
        ReceivingPrefix = "RC";
        InvoicePrefix = "INV";
        PaymentPrefix = "PAY";
        CustomerPrefix = "C";
        ParcelPrefix = "PK";
        SupplyOrderPrefix = "SO";
        CreditNotePrefix = "CN";
        InspectionPercent = DefaultInspectionPercent;
        UnclaimedDays = Vixly.Operations.Domain.Holds.OperationsHoldLimits.DefaultUnclaimedDays;
    }

    private OperationsTenantSettings(Guid tenantId, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        CurrencyCode = DefaultCurrency;
        WeightUnit = OperationsWeightUnit.Kilogram;
        DimensionUnit = OperationsDimensionUnit.Centimetre;
        DeliveryModes = OperationsDeliveryModes.Pickup;
        ShipmentPrefix = "SH";
        ReceivingPrefix = "RC";
        InvoicePrefix = "INV";
        PaymentPrefix = "PAY";
        CustomerPrefix = "C";
        ParcelPrefix = "PK";
        SupplyOrderPrefix = "SO";
        CreditNotePrefix = "CN";
        InspectionPercent = DefaultInspectionPercent;
        UnclaimedDays = Vixly.Operations.Domain.Holds.OperationsHoldLimits.DefaultUnclaimedDays;
    }

    // The random share of compliance inspection, in percent (Slice 3C, D-R4).
    public const int DefaultInspectionPercent = 5;

    public string CurrencyCode { get; private set; }

    public OperationsWeightUnit WeightUnit { get; private set; }

    public OperationsDimensionUnit DimensionUnit { get; private set; }

    // Paid first unless the tenant allows Collect or credit (ADR-0018).
    public bool AllowCollect { get; private set; }

    public bool AllowCredit { get; private set; }

    public OperationsDeliveryModes DeliveryModes { get; private set; }

    // Slice 4B: the company's IANA time zone; new prices start at midnight there.
    public string? TimeZone { get; private set; }

    // Every staff member needs a second factor, not only the Owner and Administrators (D-O2).
    public bool RequireSecondFactorForAllStaff { get; private set; }

    // Slice 5D (D-6): staff may change the insurance of an open shipment, with the customer's acceptance; the company's policy (owner, 2026-10-20).
    public bool StaffMayChangeInsurance { get; private set; } = true;

    public void ChangeInsuranceAtCounter(bool staffMayChange, DateTimeOffset utcNow)
    {
        StaffMayChangeInsurance = staffMayChange;
        MarkAsModified(utcNow);
    }

    // Slice 5E (S-9): whether the company counts its supplies stock, and what the counter does when an item has none left.
    public bool CountSupplyStock { get; private set; } = true;

    public Vixly.Operations.Domain.Supplies.OperationsSupplyStockOut SupplyStockOut { get; private set; } = Vixly.Operations.Domain.Supplies.OperationsSupplyStockOut.AllowWithWarning;

    // Slice 5E (S-17): whether clerks may discount supplies, the largest discount without approval per line and per order, and whether a reason is required.
    public bool SupplyDiscountsAllowed { get; private set; } = true;

    public decimal SupplyDiscountLinePercent { get; private set; } = 10m;

    public decimal SupplyDiscountOrderPercent { get; private set; } = 10m;

    public bool SupplyDiscountReasonRequired { get; private set; } = true;

    public void ChangeSupplyDiscounts(bool allowed, decimal linePercent, decimal orderPercent, bool reasonRequired, DateTimeOffset utcNow)
    {
        if (linePercent is < 0m or >= 100m || orderPercent is < 0m or >= 100m || decimal.Round(linePercent, 2) != linePercent || decimal.Round(orderPercent, 2) != orderPercent)
        {
            throw new DomainException(Vixly.Operations.Domain.Supplies.OperationsSupplySaleErrors.DiscountInvalid);
        }

        (SupplyDiscountsAllowed, SupplyDiscountLinePercent, SupplyDiscountOrderPercent, SupplyDiscountReasonRequired) = (allowed, linePercent, orderPercent, reasonRequired);
        MarkAsModified(utcNow);
    }

    // Slice 5F (F-5, I-4 and I-10): the tax on invoices and the note at their foot.
    public string? TaxName { get; private set; }

    public string? TaxNumber { get; private set; }

    public decimal FreightTaxPercent { get; private set; }

    public decimal InsuranceTaxPercent { get; private set; }

    public decimal SuppliesTaxPercent { get; private set; }

    public decimal ChargesTaxPercent { get; private set; }

    // One tax line per kind on the invoice; otherwise a single line (default).
    public bool TaxPerKind { get; private set; }

    public string? InvoiceNoteEn { get; private set; }

    public string? InvoiceNoteFr { get; private set; }

    public string? InvoiceNoteEs { get; private set; }

    public Vixly.Operations.Domain.Billing.OperationsTaxRates TaxRates => new(FreightTaxPercent, InsuranceTaxPercent, SuppliesTaxPercent, ChargesTaxPercent);

    // The note in a language, English when that one is empty.
    public string? InvoiceNote(string? language) => language switch
    {
        "fr" => InvoiceNoteFr ?? InvoiceNoteEn,
        "es" => InvoiceNoteEs ?? InvoiceNoteEn,
        _ => InvoiceNoteEn
    };

    public void ChangeInvoicing(string? taxName, string? taxNumber, Vixly.Operations.Domain.Billing.OperationsTaxRates rates, bool taxPerKind, string? noteEn, string? noteFr, string? noteEs,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(rates);
        Vixly.Operations.Domain.Billing.OperationsTaxRates checkedRates = rates.Checked();
        Shared.Kernel.Errors.Error invalid = Vixly.Operations.Domain.Billing.OperationsBillingErrors.TextInvalid;
        TaxName = Vixly.Operations.Domain.Billing.OperationsBillingLimits.Text(taxName, Vixly.Operations.Domain.Billing.OperationsBillingLimits.TaxNameMaxLength, invalid);
        TaxNumber = Vixly.Operations.Domain.Billing.OperationsBillingLimits.Text(taxNumber, Vixly.Operations.Domain.Billing.OperationsBillingLimits.TaxNumberMaxLength, invalid);
        (FreightTaxPercent, InsuranceTaxPercent, SuppliesTaxPercent, ChargesTaxPercent, TaxPerKind) =
            (checkedRates.Freight, checkedRates.Insurance, checkedRates.Supplies, checkedRates.Charges, taxPerKind);
        InvoiceNoteEn = Vixly.Operations.Domain.Billing.OperationsBillingLimits.Text(noteEn, Vixly.Operations.Domain.Billing.OperationsBillingLimits.NoteMaxLength, invalid);
        InvoiceNoteFr = Vixly.Operations.Domain.Billing.OperationsBillingLimits.Text(noteFr, Vixly.Operations.Domain.Billing.OperationsBillingLimits.NoteMaxLength, invalid);
        InvoiceNoteEs = Vixly.Operations.Domain.Billing.OperationsBillingLimits.Text(noteEs, Vixly.Operations.Domain.Billing.OperationsBillingLimits.NoteMaxLength, invalid);
        MarkAsModified(utcNow);
    }

    public void ChangeSupplyStock(bool count, Vixly.Operations.Domain.Supplies.OperationsSupplyStockOut stockOut, DateTimeOffset utcNow)
    {
        SupplyStockOut = Enum.IsDefined(stockOut) ? stockOut : throw new DomainException(Vixly.Operations.Domain.Supplies.OperationsSupplyStockErrors.ChoiceInvalid);
        CountSupplyStock = count;
        MarkAsModified(utcNow);
    }

    public string ShipmentPrefix { get; private set; }

    public string ReceivingPrefix { get; private set; }

    public string InvoicePrefix { get; private set; }

    public string PaymentPrefix { get; private set; }

    // Customer codes: the prefix and at least five digits, for example C00042 (Slice 2, D-C1).
    public string CustomerPrefix { get; private set; }

    // The parcel ID barcode (Slice 3B, D-R3).
    public string ParcelPrefix { get; private set; }

    // Slice 5E: orders of supplies on their own.
    public string SupplyOrderPrefix { get; private set; }

    // Slice 5F (I-6): an approved credit note (CN by default).
    public string CreditNotePrefix { get; private set; }

    // About this many parcels in a hundred are opened at random, on top of the company's rules (0 to 100).
    public int InspectionPercent { get; private set; }

    // After this many days a package still waiting for its customer, or unidentified, goes to the decision list (Slice 3E, Q-R6).
    public int UnclaimedDays { get; private set; }

    // Slice 4D step 3 (Q-D3): days a package waits free of charge, then the storage fee per day (none by default).
    public int StorageFreeDays { get; private set; } = Vixly.Operations.Domain.Holds.OperationsHoldLimits.DefaultUnclaimedDays;

    public decimal? StorageFeePerDay { get; private set; }

    // The defaults a tenant starts with; not saved until the first change.
    public static OperationsTenantSettings CreateDefault(Guid tenantId, DateTimeOffset utcNow) => new(tenantId, utcNow);

    public string Prefix(OperationsNumberKind kind) => kind switch
    {
        OperationsNumberKind.Shipment => ShipmentPrefix,
        OperationsNumberKind.Receiving => ReceivingPrefix,
        OperationsNumberKind.Invoice => InvoicePrefix,
        OperationsNumberKind.Payment => PaymentPrefix,
        OperationsNumberKind.Customer => CustomerPrefix,
        OperationsNumberKind.Parcel => ParcelPrefix,
        OperationsNumberKind.SupplyOrder => SupplyOrderPrefix,
        OperationsNumberKind.CreditNote => CreditNotePrefix,
        OperationsNumberKind.Quote => "QT",
        OperationsNumberKind.Certificate => "IC",
        _ => throw new ArgumentOutOfRangeException(nameof(kind))
    };

    public void ChangeCurrency(string? currencyCode, bool anyInvoiceExists, DateTimeOffset utcNow)
    {
        string code = OperationsCurrencies.Normalize(currencyCode);

        if (code == CurrencyCode)
        {
            return;
        }

        if (anyInvoiceExists)
        {
            throw new DomainException(OperationsSettingsErrors.CurrencyLocked);
        }

        CurrencyCode = code;
        MarkAsModified(utcNow);
    }

    public void ChangeWork(OperationsWeightUnit weightUnit, OperationsDimensionUnit dimensionUnit, bool allowCollect, bool allowCredit, OperationsDeliveryModes deliveryModes, DateTimeOffset utcNow)
    {
        if (!Enum.IsDefined(weightUnit) || !Enum.IsDefined(dimensionUnit))
        {
            throw new DomainException(OperationsSettingsErrors.UnitInvalid);
        }

        if ((deliveryModes & (OperationsDeliveryModes.Pickup | OperationsDeliveryModes.HomeDelivery)) == OperationsDeliveryModes.None
            || (deliveryModes & ~(OperationsDeliveryModes.Pickup | OperationsDeliveryModes.HomeDelivery)) != OperationsDeliveryModes.None)
        {
            throw new DomainException(OperationsSettingsErrors.DeliveryModeRequired);
        }

        WeightUnit = weightUnit;
        DimensionUnit = dimensionUnit;
        AllowCollect = allowCollect;
        AllowCredit = allowCredit;
        DeliveryModes = deliveryModes;
        MarkAsModified(utcNow);
    }

    public void ChangeTimeZone(string? timeZone, DateTimeOffset utcNow)
    {
        TimeZone = Vixly.Operations.Domain.Locations.OperationsTimeZones.Optional(timeZone);
        MarkAsModified(utcNow);
    }

    public void ChangePrefixes(string? shipment, string? receiving, string? invoice, string? payment, DateTimeOffset utcNow) =>
        ChangePrefixes(shipment, receiving, invoice, payment, CustomerPrefix, ParcelPrefix, SupplyOrderPrefix, utcNow);

    public void ChangePrefixes(string? shipment, string? receiving, string? invoice, string? payment, string? customer, DateTimeOffset utcNow) =>
        ChangePrefixes(shipment, receiving, invoice, payment, customer, ParcelPrefix, SupplyOrderPrefix, utcNow);

    public void ChangePrefixes(string? shipment, string? receiving, string? invoice, string? payment, string? customer, string? parcel, DateTimeOffset utcNow) =>
        ChangePrefixes(shipment, receiving, invoice, payment, customer, parcel, SupplyOrderPrefix, utcNow);

    public void ChangePrefixes(string? shipment, string? receiving, string? invoice, string? payment, string? customer, string? parcel, string? supplyOrder, DateTimeOffset utcNow) =>
        ChangePrefixes(shipment, receiving, invoice, payment, customer, parcel, supplyOrder, CreditNotePrefix, utcNow);

    // All eight together, so two prefixes can swap in one save.
    public void ChangePrefixes(string? shipment, string? receiving, string? invoice, string? payment, string? customer, string? parcel, string? supplyOrder, string? creditNote,
        DateTimeOffset utcNow)
    {
        string[] prefixes =
        {
            CheckPrefix(shipment), CheckPrefix(receiving), CheckPrefix(invoice), CheckPrefix(payment), CheckPrefix(customer), CheckPrefix(parcel), CheckPrefix(supplyOrder), CheckPrefix(creditNote)
        };

        if (prefixes.Distinct(StringComparer.Ordinal).Count() != prefixes.Length)
        {
            throw new DomainException(OperationsSettingsErrors.PrefixesMustDiffer);
        }

        (ShipmentPrefix, ReceivingPrefix, InvoicePrefix, PaymentPrefix, CustomerPrefix, ParcelPrefix, SupplyOrderPrefix, CreditNotePrefix) =
            (prefixes[0], prefixes[1], prefixes[2], prefixes[3], prefixes[4], prefixes[5], prefixes[6], prefixes[7]);
        MarkAsModified(utcNow);
    }

    public void ChangeInspectionPercent(int percent, DateTimeOffset utcNow)
    {
        InspectionPercent = percent is >= 0 and <= 100 ? percent : throw new DomainException(Vixly.Operations.Domain.Inspection.OperationsInspectionErrors.ShareInvalid);
        MarkAsModified(utcNow);
    }

    public void ChangeUnclaimedDays(int days, DateTimeOffset utcNow)
    {
        UnclaimedDays = days is >= Vixly.Operations.Domain.Holds.OperationsHoldLimits.MinUnclaimedDays and <= Vixly.Operations.Domain.Holds.OperationsHoldLimits.MaxUnclaimedDays
            ? days
            : throw new DomainException(Vixly.Operations.Domain.Holds.OperationsHoldErrors.DaysInvalid);
        MarkAsModified(utcNow);
    }

    public void ChangeStorageFee(int freeDays, decimal? feePerDay, DateTimeOffset utcNow)
    {
        if (freeDays is < 0 or > Vixly.Operations.Domain.Holds.OperationsHoldLimits.MaxUnclaimedDays || feePerDay is < 0m or > 1000m || (feePerDay is { } fee && decimal.Round(fee, 2) != fee))
        {
            throw new DomainException(Vixly.Operations.Domain.Holds.OperationsHoldErrors.StorageFeeInvalid);
        }

        (StorageFreeDays, StorageFeePerDay) = (freeDays, feePerDay is 0m ? null : feePerDay);
        MarkAsModified(utcNow);
    }

    public void RequireSecondFactor(bool forAllStaff, DateTimeOffset utcNow)
    {
        RequireSecondFactorForAllStaff = forAllStaff;
        MarkAsModified(utcNow);
    }

    // One to six capital letters or digits, starting with a letter.
    private static string CheckPrefix(string? prefix)
    {
        string value = prefix?.Trim().ToUpperInvariant() ?? string.Empty;

        return value.Length is >= 1 and <= OperationsSettingsLimits.PrefixMaxLength && char.IsAsciiLetterUpper(value[0]) && value.All(character => char.IsAsciiLetterUpper(character) || char.IsAsciiDigit(character))
            ? value
            : throw new DomainException(OperationsSettingsErrors.PrefixInvalid);
    }
}
