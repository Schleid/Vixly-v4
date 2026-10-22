using System.Globalization;
using Microsoft.AspNetCore.Http;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Domain.Supplies;

namespace Vixly.Operations.Web.Components.Supplies;

// =========================================================
// FILE: OperationsSupplyForm.cs
//
// PURPOSE:
// The supply forms as typed (Slice 5E): the details, the barcodes and a price, kept to show again after a refusal.
//
// RESPONSIBILITIES:
// - Read the posted fields
// - Turn the price, the quantity breaks and the warehouse prices into values, or say they could not be read
//
// IMPORTANT NOTES:
// - Prices are typed with a dot (as the prices pages); an empty break row or warehouse price is skipped
// - Barcodes: one per line, or separated by commas or spaces
// =========================================================
public sealed class OperationsSupplyForm
{
    public const int BreakRows = OperationsSupplyLimits.MaxBreaks;

    public string? Code { get; init; }

    public string? NameEn { get; init; }

    public string? NameFr { get; init; }

    public string? NameEs { get; init; }

    public string? Unit { get; init; }

    public string? Category { get; init; }

    public bool Taxed { get; init; } = true;

    public bool Active { get; init; } = true;

    public string? Barcodes { get; init; }

    public string? Price { get; init; }

    public string? Effective { get; init; }

    public IReadOnlyList<(string? Quantity, string? Price)> Breaks { get; init; } = Array.Empty<(string?, string?)>();

    public IReadOnlyDictionary<Guid, string> WarehousePrices { get; init; } = new Dictionary<Guid, string>();

    public static OperationsSupplyForm From(OperationsSupply supply) => new()
    {
        Code = supply.Code,
        NameEn = supply.NameEn,
        NameFr = supply.NameFr,
        NameEs = supply.NameEs,
        Unit = supply.Unit,
        Category = supply.Category.ToString(),
        Taxed = supply.IsTaxed,
        Active = supply.IsActive,
        Barcodes = string.Join("\n", supply.Barcodes.Select(barcode => barcode.Code))
    };

    public static OperationsSupplyForm Read(IFormCollection form)
    {
        ArgumentNullException.ThrowIfNull(form);
        string? Text(string name) => form[name].ToString() is { Length: > 0 } value ? value : null;
        Dictionary<Guid, string> places = new();

        foreach (string key in form.Keys.Where(key => key.StartsWith("wh_", StringComparison.Ordinal)).Take(OperationsSupplyLimits.MaxWarehousePrices + 1))
        {
            if (Guid.TryParse(key[3..], out Guid warehouse) && Text(key) is { } typed)
            {
                places[warehouse] = typed;
            }
        }

        return new OperationsSupplyForm
        {
            Code = Text("code"),
            NameEn = Text("name_en"),
            NameFr = Text("name_fr"),
            NameEs = Text("name_es"),
            Unit = Text("unit"),
            Category = Text("category"),
            Taxed = form["taxed"] == "true",
            Active = form["active"] == "true",
            Barcodes = Text("barcodes"),
            Price = Text("price"),
            Effective = Text("effective"),
            Breaks = Enumerable.Range(1, BreakRows).Select(row => (Text("break_qty_" + row), Text("break_price_" + row))).ToList(),
            WarehousePrices = places
        };
    }

    public OperationsSupplyCategory CategoryValue => Enum.TryParse(Category, out OperationsSupplyCategory category) && Enum.IsDefined(category) ? category : 0;

    public IReadOnlyList<string> BarcodeList =>
        (Barcodes ?? string.Empty).Split(new[] { '\n', '\r', ',', ' ', '\t', ';' }, StringSplitOptions.RemoveEmptyEntries).Take(OperationsSupplyLimits.MaxBarcodes + 1).ToList();

    // Null: not a price; the use case refuses it.
    public decimal? PriceValue => OperationsPriceText.Number(Price);

    public (DateOnly? Day, bool Read) Day =>
        Effective is null ? (null, true)
        : DateOnly.TryParseExact(Effective, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day) ? (day, true) : (null, false);

    // Null: a row could not be read.
    public IReadOnlyList<OperationsSupplyBreak>? BreakValues()
    {
        List<OperationsSupplyBreak> steps = new();

        foreach ((string? quantity, string? price) in Breaks.Where(row => row.Quantity is not null || row.Price is not null))
        {
            if (!int.TryParse(quantity, NumberStyles.None, CultureInfo.InvariantCulture, out int from) || OperationsPriceText.Number(price) is not { } unit)
            {
                return null;
            }

            steps.Add(new OperationsSupplyBreak(from, unit));
        }

        return steps;
    }

    // Null: a price could not be read.
    public IReadOnlyList<OperationsSupplyWarehousePrice>? WarehouseValues()
    {
        List<OperationsSupplyWarehousePrice> places = new();

        foreach ((Guid warehouse, string typed) in WarehousePrices)
        {
            if (OperationsPriceText.Number(typed) is not { } unit)
            {
                return null;
            }

            places.Add(new OperationsSupplyWarehousePrice(warehouse, unit));
        }

        return places;
    }
}
