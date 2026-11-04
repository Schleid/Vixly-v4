using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupply.cs
//
// PURPOSE:
// A shipping supply the company sells (Slice 5E): boxes, tape, padding, bags.
//
// RESPONSIBILITIES:
// - Keep the item code, names, unit, category, taxed flag and active state
// - Keep its barcodes
// - Keep its prices from the day each starts, and give the unit price
//
// IMPORTANT NOTES:
// - The item code is unique in the company; a scan of it finds the supply too (an item with no barcode prints its code)
// - Prices are versions (Slice 4B rules): a started price is never changed, a new one is scheduled
// - No price per customer (owner, 2026-10-20)
// =========================================================
public sealed class OperationsSupply : OperationsEntity
{
    private readonly List<OperationsSupplyBarcode> _barcodes = new();

    private readonly List<OperationsSupplyPrice> _prices = new();

    private OperationsSupply()
    {
        Code = string.Empty;
        NameEn = string.Empty;
    }

    private OperationsSupply(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Code = string.Empty;
        NameEn = string.Empty;
    }

    public string Code { get; private set; }

    public string NameEn { get; private set; }

    public string? NameFr { get; private set; }

    public string? NameEs { get; private set; }

    // How it is counted ("roll", "pack of 10"); null: one by one (shown as "each" in the reader's language).
    public string? Unit { get; private set; }

    public OperationsSupplyCategory Category { get; private set; }

    public bool IsTaxed { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyList<OperationsSupplyBarcode> Barcodes => _barcodes;

    public IReadOnlyList<OperationsSupplyPrice> Prices => _prices;

    public static OperationsSupply Create(Guid tenantId, OperationsSupplyDetails details, IEnumerable<string?> barcodes, OperationsSupplyPrice price, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(price);
        OperationsSupply supply = new(tenantId, utcNow) { IsActive = true };
        supply.Set(details);
        supply.ChangeBarcodes(barcodes, utcNow);
        OperationsVersions.Schedule(supply._prices, price, utcNow);
        return supply;
    }

    public void Change(OperationsSupplyDetails details, bool active, DateTimeOffset utcNow)
    {
        Set(details);
        IsActive = active;
        MarkAsModified(utcNow);
    }

    // The barcodes as listed now: new ones added, missing ones removed; the same code twice counts once.
    public void ChangeBarcodes(IEnumerable<string?> barcodes, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(barcodes);
        List<string> codes = barcodes.Select(code => OperationsSupplyLimits.Code(code, OperationsSupplyLimits.BarcodeMaxLength, OperationsSupplyErrors.BarcodeInvalid))
            .OfType<string>().Distinct(StringComparer.Ordinal).ToList();

        if (codes.Count > OperationsSupplyLimits.MaxBarcodes)
        {
            throw new DomainException(OperationsSupplyErrors.BarcodeInvalid);
        }

        _barcodes.RemoveAll(barcode => !codes.Contains(barcode.Code, StringComparer.Ordinal));
        _barcodes.AddRange(codes.Where(code => _barcodes.All(barcode => barcode.Code != code)).Select(code => OperationsSupplyBarcode.Create(TenantId, Id, code, utcNow)));
        MarkAsModified(utcNow);
    }

    public void SchedulePrice(OperationsSupplyPrice price, DateTimeOffset utcNow)
    {
        OperationsVersions.Schedule(_prices, price, utcNow);
        MarkAsModified(utcNow);
    }

    public void RemoveScheduledPrice(Guid priceId, DateTimeOffset utcNow)
    {
        OperationsVersions.Remove(_prices, priceId, utcNow);
        MarkAsModified(utcNow);
    }

    public OperationsSupplyPrice? PriceAt(DateTimeOffset at) => OperationsVersions.At(_prices, at);

    // The unit price at a warehouse for a quantity on that day; null before its first price starts.
    public decimal? UnitPrice(Guid? warehouseId, int quantity, DateTimeOffset at) => PriceAt(at)?.UnitPrice(warehouseId, quantity);

    public bool Answers(string? scanned) =>
        OperationsSupplyLimits.TryCode(scanned, OperationsSupplyLimits.BarcodeMaxLength) is { } code
        && (code == Code || _barcodes.Any(barcode => barcode.Code == code));

    // In the customer's or the clerk's language; French and Spanish fall back to English.
    public string Name(string? language) => language switch
    {
        "fr" => NameFr ?? NameEn,
        "es" => NameEs ?? NameEn,
        _ => NameEn
    };

    private void Set(OperationsSupplyDetails details)
    {
        ArgumentNullException.ThrowIfNull(details);

        if (!Enum.IsDefined(details.Category))
        {
            throw new DomainException(OperationsSupplyErrors.CategoryInvalid);
        }

        Code = OperationsSupplyLimits.Code(details.Code, OperationsSupplyLimits.CodeMaxLength, OperationsSupplyErrors.CodeInvalid) ?? throw new DomainException(OperationsSupplyErrors.CodeRequired);
        NameEn = OperationsChargeLimits.Text(details.NameEn, OperationsSupplyLimits.NameMaxLength, OperationsSupplyErrors.TextInvalid) ?? throw new DomainException(OperationsSupplyErrors.NameRequired);
        NameFr = OperationsChargeLimits.Text(details.NameFr, OperationsSupplyLimits.NameMaxLength, OperationsSupplyErrors.TextInvalid);
        NameEs = OperationsChargeLimits.Text(details.NameEs, OperationsSupplyLimits.NameMaxLength, OperationsSupplyErrors.TextInvalid);
        Unit = OperationsChargeLimits.Text(details.Unit, OperationsSupplyLimits.UnitMaxLength, OperationsSupplyErrors.TextInvalid);
        Category = details.Category;
        IsTaxed = details.Taxed;
    }
}
