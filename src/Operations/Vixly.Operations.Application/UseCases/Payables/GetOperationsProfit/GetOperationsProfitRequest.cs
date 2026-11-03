namespace Vixly.Operations.Application.UseCases.Payables.GetOperationsProfit;

// =========================================================
// FILE: GetOperationsProfitRequest.cs
//
// PURPOSE:
// The profit per shipment over a period (Slice 8): revenue, carrier cost, other costs and profit, with the totals.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsProfitRequest
{
    // Invoices issued from this day to that one (the company’s days); none: this month.
    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }
}
