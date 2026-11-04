using Vixly.Operations.Domain.Items;

namespace Vixly.Operations.Application.Items;

// =========================================================
// FILE: OperationsItemRow.cs
//
// PURPOSE:
// One item of a parcel as staff see it (owner, 2026-11-01).
//
// RESPONSIBILITIES:
// - Carry the customer's declaration and the classification
//
// IMPORTANT NOTES:
// - No item id: the parcel's single declaration, not yet an item
// =========================================================
public sealed record OperationsItemRow(Guid? ItemId, int Line, string Words, decimal Quantity, decimal ValueEach, string CurrencyCode, bool Used, string? Description, string? HsCode,
    string? OriginCountryCode, OperationsItemUnit Unit, bool Complete)
{
    public static OperationsItemRow Of(OperationsParcelItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        return new(item.Id, item.Line, item.Words, item.Quantity, item.ValueEach, item.CurrencyCode, item.Used, item.Description, OperationsHsCode.Format(item.HsCode),
            item.OriginCountryCode, item.Unit, item.IsComplete);
    }
}
