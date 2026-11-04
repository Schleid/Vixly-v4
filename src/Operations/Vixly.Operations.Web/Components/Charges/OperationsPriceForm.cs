using System.Globalization;
using Microsoft.AspNetCore.Http;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Domain.Charges;

namespace Vixly.Operations.Web.Components.Charges;

// =========================================================
// FILE: OperationsPriceForm.cs
//
// PURPOSE:
// Reads the prices staff type for a version of a price list: band rows, item prices, fees and the day it starts (Slice 4B).
//
// RESPONSIBILITIES:
// - Read any number of band rows (from, kind, amount, base); empty rows are left out
// - Read item prices by item type, the fees and the effective day
//
// IMPORTANT NOTES:
// - Valid is false when a value cannot be read; the use case checks the rest
// - A dot for decimals, whatever the language
// =========================================================
public sealed class OperationsPriceForm
{
    private const int MaxRows = OperationsChargeLimits.MaxBands + 10;

    public IReadOnlyList<OperationsRateBand> Bands { get; private init; } = Array.Empty<OperationsRateBand>();

    public IReadOnlyList<OperationsRateItem> Items { get; private init; } = Array.Empty<OperationsRateItem>();

    public decimal? MinimumCharge { get; private init; }

    public decimal? ServiceFee { get; private init; }

    public decimal? ConsolidationFee { get; private init; }

    // Slice 4C, ground.
    public decimal? PerPackage { get; private init; }

    public decimal? PerShipment { get; private init; }

    public DateOnly? EffectiveFrom { get; private init; }

    public bool Valid { get; private init; }

    public bool EffectiveRead { get; private init; }

    public static OperationsPriceForm Read(IFormCollection form)
    {
        ArgumentNullException.ThrowIfNull(form);
        bool valid = true;
        List<OperationsRateBand> bands = new();

        for (int row = 0; row < MaxRows; row++)
        {
            string from = Text(form, "band_from_" + row);
            string amount = Text(form, "band_amount_" + row);

            if (from.Length == 0 && amount.Length == 0)
            {
                continue;
            }

            string kindText = Text(form, "band_kind_" + row);
            bool kindRead = Enum.TryParse(kindText, ignoreCase: false, out OperationsBandKind kind) && Enum.IsDefined(kind) && !kindText.Any(char.IsDigit);
            decimal? start = OperationsPriceText.Number(from);
            decimal? price = OperationsPriceText.Number(amount);
            string baseText = Text(form, "band_base_" + row);
            decimal? @base = kind == OperationsBandKind.BasePlusPerUnit ? (baseText.Length == 0 ? 0m : OperationsPriceText.Number(baseText)) : null;

            // An unreadable base ("1O") is refused, never read as 0.
            if (!kindRead || start is null || price is null || (kind == OperationsBandKind.BasePlusPerUnit && @base is null))
            {
                valid = false;
                continue;
            }

            bands.Add(new OperationsRateBand(start.Value, kind, price.Value, @base));
        }

        List<OperationsRateItem> items = new();

        foreach (string key in form.Keys.Where(key => key.StartsWith("item_", StringComparison.Ordinal)).Take(OperationsChargeLimits.MaxItemTypes))
        {
            string value = Text(form, key);

            if (value.Length == 0)
            {
                continue;
            }

            if (Guid.TryParse(key["item_".Length..], out Guid typeId) && OperationsPriceText.Number(value) is { } price)
            {
                items.Add(new OperationsRateItem(typeId, price));
            }
            else
            {
                valid = false;
            }
        }

        (decimal? minimum, bool minimumRead) = Optional(form, "minimum");
        (decimal? serviceFee, bool serviceRead) = Optional(form, "service_fee");
        (decimal? consolidation, bool consolidationRead) = Optional(form, "consolidation_fee");
        (decimal? perPackage, bool perPackageRead) = Optional(form, "per_package");
        (decimal? perShipment, bool perShipmentRead) = Optional(form, "per_shipment");
        (DateOnly? effective, bool effectiveRead) = Day(form);

        return new OperationsPriceForm
        {
            Bands = bands,
            Items = items,
            MinimumCharge = minimum,
            ServiceFee = serviceFee,
            ConsolidationFee = consolidation,
            PerPackage = perPackage,
            PerShipment = perShipment,
            EffectiveFrom = effective,
            EffectiveRead = effectiveRead,
            Valid = valid && minimumRead && serviceRead && consolidationRead && perPackageRead && perShipmentRead
        };
    }

    // The day a new version starts: yyyy-MM-dd from a date field; empty means now.
    public static (DateOnly? Day, bool Read) Day(IFormCollection form)
    {
        ArgumentNullException.ThrowIfNull(form);
        string text = Text(form, "effective");
        return text.Length == 0 ? (null, true)
            : DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day) ? (day, true) : (null, false);
    }

    private static (decimal? Value, bool Read) Optional(IFormCollection form, string name)
    {
        string text = Text(form, name);
        return text.Length == 0 ? (null, true) : OperationsPriceText.Number(text) is { } number ? (number, true) : (null, false);
    }

    private static string Text(IFormCollection form, string name)
    {
        string value = form[name].ToString().Trim();
        return value.Length > 40 ? "x" : value;
    }
}
