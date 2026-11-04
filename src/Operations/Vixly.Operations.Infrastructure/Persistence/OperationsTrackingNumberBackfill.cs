using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Billing;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Infrastructure.Persistence;

// =========================================================
// FILE: OperationsTrackingNumberBackfill.cs
//
// PURPOSE:
// Gives a tracking number to booked shipments that are paid, charged to an account or collect and still have none (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Oldest first, a batch at a time
//
// IMPORTANT NOTES:
// - A shipment waiting for payment (or for finance to confirm a Zelle or a transfer) gets nothing
// =========================================================
internal sealed class OperationsTrackingNumberBackfill : IOperationsTrackingNumberBackfill
{
    private readonly OperationsDbContext _context;

    public OperationsTrackingNumberBackfill(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<int> RunAsync(int batchSize, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        // In the system scope the company filter is off, so every company's shipments are seen.
        // Collect, or an invoice paid in full or charged to the account; the rule itself is checked again below.
        List<OperationsShipment> waiting = await _context.Shipments
            .Where(item => item.TrackingNumber == null && item.Status == OperationsShipmentStatus.Booked
                && ((item.Options & OperationsShipmentOptions.FreightCollect) != 0
                    || _context.Invoices.Any(invoice => invoice.ShipmentId == item.Id && invoice.Status != OperationsInvoiceStatus.Voided
                        && (invoice.OnAccount == OperationsAccountCharge.Charged
                            || (invoice.PendingTotal == 0m && invoice.Total - invoice.CreditedTotal - invoice.PaidTotal + invoice.RefundedTotal <= 0m)))))
            .OrderBy(item => item.BookedUtc)
            .Take(Math.Clamp(batchSize, 1, 500))
            .ToListAsync(cancellationToken);

        if (waiting.Count == 0)
        {
            return 0;
        }

        List<Guid> ids = waiting.Select(item => item.Id).ToList();
        List<OperationsInvoice> invoices = await _context.Invoices
            .Where(item => item.ShipmentId != null && ids.Contains(item.ShipmentId.Value))
            .ToListAsync(cancellationToken);
        int given = 0;

        foreach (OperationsShipment shipment in waiting)
        {
            if (shipment.FreightCollect || invoices.Any(invoice => invoice.ShipmentId == shipment.Id && invoice.Released))
            {
                await _context.GiveTrackingNumberAsync(shipment, utcNow, true, cancellationToken);
                given += shipment.TrackingNumber is null ? 0 : 1;
            }
        }

        if (given > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }

        return given;
    }
}
