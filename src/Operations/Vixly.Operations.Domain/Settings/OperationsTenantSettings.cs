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
        BundlePrefix = "BD";
        BoxPrefix = "CB";
        DispatchPrefix = "DSP";
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
        BundlePrefix = "BD";
        BoxPrefix = "CB";
        DispatchPrefix = "DSP";
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

    // Slice 5F step 2b (K-2): the company's credit terms in days, what a charge above the limit does, and the monthly statement notice.
    public int CreditTermsDays { get; private set; } = 30;

    public Vixly.Operations.Domain.Billing.OperationsCreditOverLimit CreditOverLimit { get; private set; } = Vixly.Operations.Domain.Billing.OperationsCreditOverLimit.Refuse;

    public bool CreditStatements { get; private set; } = true;

    public void ChangeCredit(int termsDays, Vixly.Operations.Domain.Billing.OperationsCreditOverLimit overLimit, bool statements, DateTimeOffset utcNow)
    {
        if (termsDays is < 0 or > Vixly.Operations.Domain.Billing.OperationsCreditAccount.MaxTermsDays || !Enum.IsDefined(overLimit))
        {
            throw new DomainException(Vixly.Operations.Domain.Billing.OperationsCreditErrors.SettingsInvalid);
        }

        (CreditTermsDays, CreditOverLimit, CreditStatements) = (termsDays, overLimit, statements);
        MarkAsModified(utcNow);
    }

    // Slice 5F (C-1): the payment methods the company accepts; cash on by default.
    public bool AcceptCash { get; private set; } = true;

    public bool AcceptZelle { get; private set; }

    public bool AcceptCheque { get; private set; }

    public bool AcceptBankTransfer { get; private set; }

    // The company's own methods by name (MonCash, Natcash...); none: not offered.
    public string? OtherMethod1 { get; private set; }

    public string? OtherMethod2 { get; private set; }

    public string? OtherMethod3 { get; private set; }

    // Zelle (P-8): the QR picture the company uploads, its tag, the email or phone enrolled, the name shown, and its terms.
    public string? ZelleQrKey { get; private set; }

    public string? ZelleTag { get; private set; }

    public string? ZelleContact { get; private set; }

    public string? ZelleName { get; private set; }

    public string? ZelleTermsEn { get; private set; }

    public string? ZelleTermsFr { get; private set; }

    public string? ZelleTermsEs { get; private set; }

    public bool Accepts(Vixly.Operations.Domain.Billing.OperationsPaymentMethod method) => method switch
    {
        Vixly.Operations.Domain.Billing.OperationsPaymentMethod.Cash => AcceptCash,
        Vixly.Operations.Domain.Billing.OperationsPaymentMethod.Zelle => AcceptZelle,
        Vixly.Operations.Domain.Billing.OperationsPaymentMethod.Cheque => AcceptCheque,
        Vixly.Operations.Domain.Billing.OperationsPaymentMethod.BankTransfer => AcceptBankTransfer,
        Vixly.Operations.Domain.Billing.OperationsPaymentMethod.Other1 => OtherMethod1 is not null,
        Vixly.Operations.Domain.Billing.OperationsPaymentMethod.Other2 => OtherMethod2 is not null,
        Vixly.Operations.Domain.Billing.OperationsPaymentMethod.Other3 => OtherMethod3 is not null,
        _ => false
    };

    // The company's own name of one of its other methods.
    public string? OtherName(Vixly.Operations.Domain.Billing.OperationsPaymentMethod method) => method switch
    {
        Vixly.Operations.Domain.Billing.OperationsPaymentMethod.Other1 => OtherMethod1,
        Vixly.Operations.Domain.Billing.OperationsPaymentMethod.Other2 => OtherMethod2,
        Vixly.Operations.Domain.Billing.OperationsPaymentMethod.Other3 => OtherMethod3,
        _ => null
    };

    public string? ZelleTerms(string? language) => language switch
    {
        "fr" => ZelleTermsFr ?? ZelleTermsEn,
        "es" => ZelleTermsEs ?? ZelleTermsEn,
        _ => ZelleTermsEn
    };

    public void ChangePayments(bool cash, bool zelle, bool cheque, bool bankTransfer, string? other1, string? other2, string? other3, DateTimeOffset utcNow)
    {
        Shared.Kernel.Errors.Error named = Vixly.Operations.Domain.Billing.OperationsPaymentErrors.MethodNameInvalid;
        string?[] others = { Vixly.Operations.Domain.Billing.OperationsBillingLimits.Text(other1, 30, named), Vixly.Operations.Domain.Billing.OperationsBillingLimits.Text(other2, 30, named), Vixly.Operations.Domain.Billing.OperationsBillingLimits.Text(other3, 30, named) };

        if (others.Where(name => name is not null).Distinct(StringComparer.OrdinalIgnoreCase).Count() != others.Count(name => name is not null))
        {
            throw new DomainException(named);
        }

        // Zelle is offered only with a way to pay (a tag or an email or phone) and the terms the customer accepts (P-9).
        if (zelle && ((ZelleTag is null && ZelleContact is null) || ZelleTermsEn is null))
        {
            throw new DomainException(Vixly.Operations.Domain.Billing.OperationsPaymentErrors.ZelleIncomplete);
        }

        (AcceptCash, AcceptZelle, AcceptCheque, AcceptBankTransfer) = (cash, zelle, cheque, bankTransfer);
        (OtherMethod1, OtherMethod2, OtherMethod3) = (others[0], others[1], others[2]);
        MarkAsModified(utcNow);
    }

    public void ChangeZelle(string? tag, string? contact, string? name, string? termsEn, string? termsFr, string? termsEs, DateTimeOffset utcNow)
    {
        Shared.Kernel.Errors.Error invalid = Vixly.Operations.Domain.Billing.OperationsPaymentErrors.TextInvalid;
        string? Short(string? value) => Vixly.Operations.Domain.Billing.OperationsBillingLimits.Text(value, 120, invalid);
        string? Long(string? value) => Vixly.Operations.Domain.Billing.OperationsBillingLimits.Text(value, 2000, invalid);
        (ZelleTag, ZelleContact, ZelleName) = (Short(tag), Short(contact), Short(name));
        (ZelleTermsEn, ZelleTermsFr, ZelleTermsEs) = (Long(termsEn), Long(termsFr), Long(termsEs));

        // Whether Zelle can be offered with these details is checked by ChangePayments, called after with the new choice.
        MarkAsModified(utcNow);
    }

    // The stored picture's key; null removes it. Returns the key it replaces, for the file to be removed.
    public string? ChangeZelleQr(string? key, DateTimeOffset utcNow)
    {
        string? before = ZelleQrKey;
        ZelleQrKey = key;
        MarkAsModified(utcNow);
        return before;
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

    // Slice 6A (D-1): bundles and repack boxes.
    public string BundlePrefix { get; private set; }

    public string BoxPrefix { get; private set; }

    // Slice 6B: dispatches.
    public string DispatchPrefix { get; private set; }

    // Slice 6C (E-3, E-5): the standard causes the company switched off (comma list), and whether customers get an email for a delay or an exception.
    public string? DisabledEventCauses { get; private set; }

    public bool EventEmails { get; private set; } = true;

    // Slice 7 (R-1): whether customers get an email when their shipment is ready for pickup.
    public bool PickupEmails { get; private set; } = true;

    // Slice 7 step 2 (R-4, R-5, R-10): the proof asked at the pickup counter (ID and signature by default), destination charges and storage (off by default).
    public Vixly.Operations.Domain.Pickup.OperationsPickupProofs PickupProofs { get; private set; } = Vixly.Operations.Domain.Pickup.OperationsPickupProofs.Default;

    public bool DestinationCharges { get; private set; }

    public bool DestinationStorage { get; private set; }

    public int DestinationStorageFreeDays { get; private set; } = 7;

    public decimal? DestinationStorageFeePerDay { get; private set; }

    public void ChangePickup(Vixly.Operations.Domain.Pickup.OperationsPickupProofs proofs, bool charges, bool storage, int freeDays, decimal? feePerDay, DateTimeOffset utcNow)
    {
        if ((proofs & ~Vixly.Operations.Domain.Pickup.OperationsPickupProofs.All) != 0 || freeDays is < 0 or > 365 || feePerDay is < 0m or > 10_000m
            || (storage && feePerDay is not > 0m) || feePerDay is { } fee && decimal.Round(fee, 2) != fee)
        {
            throw new DomainException(Vixly.Operations.Domain.Pickup.OperationsPickupErrors.SettingsInvalid);
        }

        (PickupProofs, DestinationCharges, DestinationStorage, DestinationStorageFreeDays, DestinationStorageFeePerDay) = (proofs, charges, storage, freeDays, feePerDay);
        MarkAsModified(utcNow);
    }

    public void ChangePickupEmails(bool emails, DateTimeOffset utcNow)
    {
        PickupEmails = emails;
        MarkAsModified(utcNow);
    }

    public IReadOnlyList<string> DisabledEventCauseList() =>
        string.IsNullOrEmpty(DisabledEventCauses) ? Array.Empty<string>() : DisabledEventCauses.Split(',', StringSplitOptions.RemoveEmptyEntries);

    public void ChangeEvents(bool emails, IEnumerable<string>? disabledCauses, DateTimeOffset utcNow)
    {
        List<string> disabled = (disabledCauses ?? Array.Empty<string>()).Select(code => code.Trim()).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

        if (disabled.Any(code => !Vixly.Operations.Domain.Events.OperationsEventReasons.IsStandard(code)))
        {
            throw new DomainException(Vixly.Operations.Domain.Events.OperationsEventErrors.ReasonRequired);
        }

        (EventEmails, DisabledEventCauses) = (emails, disabled.Count == 0 ? null : string.Join(',', disabled));
        MarkAsModified(utcNow);
    }

    // Slice 6A (D-1): the company's bundle types, one per line (none: bundles have no type); what a target does; how consolidated shipments travel.
    public string? BundleTypes { get; private set; }

    public Vixly.Operations.Domain.Bundles.OperationsBundleTargetRule BundleTargetRule { get; private set; } = Vixly.Operations.Domain.Bundles.OperationsBundleTargetRule.Warn;

    public Vixly.Operations.Domain.Bundles.OperationsConsolidationWay ConsolidationWay { get; private set; } = Vixly.Operations.Domain.Bundles.OperationsConsolidationWay.Repack;

    public IReadOnlyList<string> BundleTypeList() =>
        BundleTypes is null ? Array.Empty<string>() : BundleTypes.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

    public void ChangeBundles(IEnumerable<string?> types, Vixly.Operations.Domain.Bundles.OperationsBundleTargetRule rule, Vixly.Operations.Domain.Bundles.OperationsConsolidationWay way,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(types);
        List<string> names = types.Select(Vixly.Operations.Domain.Bundles.OperationsBundle.TypeText).OfType<string>().ToList();

        if (!Enum.IsDefined(rule) || !Enum.IsDefined(way) || names.Count > Vixly.Operations.Domain.Bundles.OperationsBundleLimits.MaxTypes
            || names.Distinct(StringComparer.OrdinalIgnoreCase).Count() != names.Count)
        {
            throw new DomainException(Vixly.Operations.Domain.Bundles.OperationsBundleErrors.SettingsInvalid);
        }

        (BundleTypes, BundleTargetRule, ConsolidationWay) = (names.Count == 0 ? null : string.Join('\n', names), rule, way);
        MarkAsModified(utcNow);
    }

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
        OperationsNumberKind.Bundle => BundlePrefix,
        OperationsNumberKind.Box => BoxPrefix,
        OperationsNumberKind.Dispatch => DispatchPrefix,
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

    public void ChangePrefixes(string? shipment, string? receiving, string? invoice, string? payment, string? customer, string? parcel, string? supplyOrder, string? creditNote,
        DateTimeOffset utcNow) =>
        ChangePrefixes(shipment, receiving, invoice, payment, customer, parcel, supplyOrder, creditNote, BundlePrefix, BoxPrefix, utcNow);

    public void ChangePrefixes(string? shipment, string? receiving, string? invoice, string? payment, string? customer, string? parcel, string? supplyOrder, string? creditNote,
        string? bundle, string? box, DateTimeOffset utcNow) =>
        ChangePrefixes(shipment, receiving, invoice, payment, customer, parcel, supplyOrder, creditNote, bundle, box, DispatchPrefix, utcNow);

    // All eleven together, so two prefixes can swap in one save.
    public void ChangePrefixes(string? shipment, string? receiving, string? invoice, string? payment, string? customer, string? parcel, string? supplyOrder, string? creditNote,
        string? bundle, string? box, string? dispatch, DateTimeOffset utcNow)
    {
        string[] prefixes =
        {
            CheckPrefix(shipment), CheckPrefix(receiving), CheckPrefix(invoice), CheckPrefix(payment), CheckPrefix(customer), CheckPrefix(parcel), CheckPrefix(supplyOrder), CheckPrefix(creditNote),
            CheckPrefix(bundle), CheckPrefix(box), CheckPrefix(dispatch)
        };

        if (prefixes.Distinct(StringComparer.Ordinal).Count() != prefixes.Length)
        {
            throw new DomainException(OperationsSettingsErrors.PrefixesMustDiffer);
        }

        (ShipmentPrefix, ReceivingPrefix, InvoicePrefix, PaymentPrefix, CustomerPrefix, ParcelPrefix, SupplyOrderPrefix, CreditNotePrefix, BundlePrefix, BoxPrefix, DispatchPrefix) =
            (prefixes[0], prefixes[1], prefixes[2], prefixes[3], prefixes[4], prefixes[5], prefixes[6], prefixes[7], prefixes[8], prefixes[9], prefixes[10]);
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
