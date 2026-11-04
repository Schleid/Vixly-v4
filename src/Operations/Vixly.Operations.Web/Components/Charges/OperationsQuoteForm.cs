using Microsoft.AspNetCore.Http;
using Vixly.Operations.Application.Charges;

namespace Vixly.Operations.Web.Components.Charges;

// =========================================================
// FILE: OperationsQuoteForm.cs
//
// PURPOSE:
// What staff typed on the quote page, kept as typed so the page shows it again (Slice 4).
//
// RESPONSIBILITIES:
// - Read the form: customer, destination, service, options, five piece rows, extra quantities, preferred price
// - Give the pieces and quantities to the use cases
//
// IMPORTANT NOTES:
// - A row without a weight is left out; a row with an unreadable number makes the pieces invalid
// =========================================================
public sealed class OperationsQuoteForm
{
    public const int Rows = 5;

    public string? Customer { get; init; }

    // Slice 4C: the route (no origin: the company's only origin, else any), and how the package is handed over at the destination.
    public Guid? Origin { get; init; }

    public Guid? Destination { get; init; }

    public string? Delivery { get; init; }

    public string? Service { get; init; }

    public bool Consolidated { get; init; }

    public bool Insured { get; init; }

    public string?[] Weights { get; init; } = new string?[Rows];

    public string?[] Lengths { get; init; } = new string?[Rows];

    public string?[] Widths { get; init; } = new string?[Rows];

    public string?[] Heights { get; init; } = new string?[Rows];

    public string?[] Values { get; init; } = new string?[Rows];

    // Item type ids, as chosen.
    public string?[] Items { get; init; } = new string?[Rows];

    public string?[] Contents { get; init; } = new string?[Rows];

    public IReadOnlyDictionary<Guid, int> Quantities { get; init; } = new Dictionary<Guid, int>();

    public string? Preferred { get; init; }

    public string? Reason { get; init; }

    public string? Days { get; init; }

    public string? Key { get; init; }

    public static OperationsQuoteForm Read(IFormCollection form)
    {
        ArgumentNullException.ThrowIfNull(form);
        string?[] Column(string name) => Enumerable.Range(1, Rows).Select(row => Text(form, name + "_" + row)).ToArray();
        Dictionary<Guid, int> quantities = new();

        foreach (string key in form.Keys.Where(key => key.StartsWith("extra_", StringComparison.Ordinal)).Take(OperationsChargeLimitsForForm.MaxExtras))
        {
            if (Guid.TryParse(key["extra_".Length..], out Guid id) && OperationsPriceText.Number(form[key].ToString()) is { } quantity && quantity == decimal.Truncate(quantity) && quantity <= 1000)
            {
                quantities[id] = (int)quantity;
            }
        }

        return new OperationsQuoteForm
        {
            Customer = Text(form, "customer"),
            Origin = Guid.TryParse(Text(form, "origin"), out Guid origin) ? origin : null,
            Destination = Guid.TryParse(Text(form, "destination"), out Guid destination) ? destination : null,
            Delivery = Text(form, "delivery") is "Pickup" or "HomeDelivery" ? Text(form, "delivery") : null,
            Service = Text(form, "service"),
            Consolidated = form["consolidated"] == "true",
            Insured = form["insured"] == "true",
            Weights = Column("weight"),
            Lengths = Column("length"),
            Widths = Column("width"),
            Heights = Column("height"),
            Values = Column("value"),
            Items = Column("item"),
            Contents = Column("contents"),
            Quantities = quantities,
            Preferred = Text(form, "preferred"),
            Reason = Text(form, "reason"),
            Days = Text(form, "days"),
            Key = Text(form, "key")
        };
    }

    // The rows with a weight; an unreadable number gives a piece the use case refuses.
    public IReadOnlyList<OperationsPieceInput> Pieces() =>
        Enumerable.Range(0, Rows).Where(row => Weights[row] is not null)
            .Select(row => new OperationsPieceInput(
                OperationsPriceText.Number(Weights[row]),
                Optional(Lengths[row]),
                Optional(Widths[row]),
                Optional(Heights[row]),
                Optional(Values[row]),
                Guid.TryParse(Items[row], out Guid itemType) ? itemType : null,
                Contents[row]))
            .ToList();

    private static decimal? Optional(string? value) => value is null ? 0m : OperationsPriceText.Number(value) ?? -1m;

    private static string? Text(IFormCollection form, string name)
    {
        string value = form[name].ToString().Trim();
        return value.Length is 0 or > 500 ? null : value;
    }
}
