namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsEstimateSupply.cs
//
// PURPOSE:
// A line of supplies in the shipment estimate (Slice 5E, S-16).
//
// RESPONSIBILITIES:
// - Carry the supply as sold, how many, the unit price, the discount and the amount
//
// IMPORTANT NOTES:
// - Counted in the estimate's total; the customer's portal shows them too
// =========================================================
public sealed record OperationsEstimateSupply(string Code, string Name, int Quantity, decimal UnitPrice, decimal DiscountPercent, decimal Amount)
{
    // In the reader's language when the supply is still in the catalogue; else the name it was sold under.
    public static IReadOnlyList<OperationsEstimateSupply> Of(Vixly.Operations.Domain.Supplies.OperationsSupplySale? sale,
        IReadOnlyList<Vixly.Operations.Domain.Supplies.OperationsSupply> catalogue, string? language) =>
        sale is null
            ? Array.Empty<OperationsEstimateSupply>()
            : sale.Lines.Select(line => new OperationsEstimateSupply(line.Code, catalogue.FirstOrDefault(supply => supply.Id == line.SupplyId)?.Name(language) ?? line.Name,
                line.Quantity, line.UnitPrice, line.DiscountPercent, line.Amount)).ToList();
}
