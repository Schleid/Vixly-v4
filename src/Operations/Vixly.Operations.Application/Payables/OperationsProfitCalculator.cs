using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Dispatch;

namespace Vixly.Operations.Application.Payables;

// =========================================================
// FILE: OperationsProfitCalculator.cs
//
// PURPOSE:
// Works out each shipment's profit (Slice 8; owner, 2026-10-22: the estimate, then what the carrier billed).
//
// RESPONSIBILITIES:
// - Revenue from its invoices (lines still owed after credits, without the government's duties and local taxes, without sales tax)
// - Carrier cost: its share of each dispatch it travelled on, from the carriers' approved bills once entered, the estimate until then
// - Direct cost: the approved bills put on the shipment itself (a partner's fee, a customs broker)
//
// IMPORTANT NOTES:
// - Voided invoices bring nothing; a dispatch with no cost known yet counts nothing and marks the result estimated
// =========================================================
public sealed class OperationsProfitCalculator
{
    // Collected for the government and paid over: not the company's revenue (O-10).
    private static readonly OperationsChargeLineKind[] PassThrough = { OperationsChargeLineKind.Duties, OperationsChargeLineKind.LocalTax };

    private readonly IOperationsPayablesRepository _payables;

    private readonly IOperationsBillingRepository _billing;

    public OperationsProfitCalculator(IOperationsPayablesRepository payables, IOperationsBillingRepository billing)
    {
        _payables = payables ?? throw new ArgumentNullException(nameof(payables));
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
    }

    public async Task<IReadOnlyList<OperationsShipmentProfit>> ForAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipmentIds);

        if (shipmentIds.Count == 0)
        {
            return Array.Empty<OperationsShipmentProfit>();
        }

        IReadOnlyList<OperationsInvoice> invoices = await _billing.ListShipmentInvoicesAsync(shipmentIds, cancellationToken);
        IReadOnlyList<(OperationsDispatchShipment Row, OperationsDispatch Dispatch)> rows = await _payables.ListDispatchRowsAsync(shipmentIds, cancellationToken);
        IReadOnlyDictionary<Guid, decimal> direct = await _payables.DirectCostsAsync(shipmentIds, cancellationToken);
        IReadOnlyDictionary<Guid, string> numbers = await _payables.ShipmentNumbersAsync(shipmentIds, cancellationToken);

        return shipmentIds.Select(id =>
        {
            decimal revenue = invoices.Where(invoice => invoice.ShipmentId == id && invoice.Status != OperationsInvoiceStatus.Voided)
                .Sum(invoice => invoice.Lines.Where(line => line.ChargeKind is not { } kind || !PassThrough.Contains(kind)).Sum(line => line.Remaining));
            List<(OperationsDispatchShipment Row, OperationsDispatch Dispatch)> legs = rows.Where(item => item.Row.ShipmentId == id).ToList();
            decimal carrier = legs.Sum(item => item.Row.CostShare ?? 0m);
            bool estimated = legs.Count == 0 || legs.Any(item => !item.Dispatch.BilledComplete);
            return new OperationsShipmentProfit(id, numbers.TryGetValue(id, out string? number) ? number : string.Empty, revenue, carrier,
                direct.TryGetValue(id, out decimal cost) ? cost : 0m, estimated, legs.Count > 0);
        }).ToList();
    }
}
