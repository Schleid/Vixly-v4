namespace Vixly.Operations.Application.Payables;

// =========================================================
// FILE: OperationsShipmentProfit.cs
//
// PURPOSE:
// What a shipment earned and cost (Slice 8).
//
// RESPONSIBILITIES:
// - Carry its revenue, its carrier cost (share of its dispatches), the bills put on it directly, and the profit
//
// IMPORTANT NOTES:
// - Revenue: its invoices without taxes, duties and local taxes collected for the government, less what was credited
// - Estimated: a dispatch not yet billed by its carrier is counted at its estimate
// =========================================================
public sealed record OperationsShipmentProfit(Guid ShipmentId, string Number, decimal Revenue, decimal CarrierCost, decimal DirectCost, bool Estimated, bool Dispatched)
{
    public decimal Cost => CarrierCost + DirectCost;

    public decimal Profit => Revenue - Cost;

    // Profit as a share of revenue, in percent; none without revenue.
    public decimal? Margin => Revenue > 0m ? decimal.Round(Profit / Revenue * 100m, 1) : null;
}
