using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Payables;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tickets;
using Vixly.Operations.Domain.Tracking;
using Vixly.Operations.Infrastructure.Persistence;

namespace Vixly.Operations.Infrastructure.Reports;

// =========================================================
// FILE: OperationsDashboardReader.cs
//
// PURPOSE:
// Counts what the staff home page shows (Slice 11).
//
// RESPONSIBILITIES:
// - One COUNT per counter, over the warehouses given
//
// IMPORTANT NOTES:
// - Read only, tenant-filtered; the same rules as the lists the counters open
// =========================================================
internal sealed class OperationsDashboardReader : IOperationsDashboardReader
{
    private readonly OperationsDbContext _context;

    public OperationsDashboardReader(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<int> CountAsync(string key, IReadOnlyCollection<Guid>? warehouses, DateOnly today, CancellationToken cancellationToken)
    {
        List<Guid> mine = warehouses?.ToList() ?? new List<Guid>();
        bool all = warehouses is null;

        return key switch
        {
            "to_inspect" => _context.Parcels.CountAsync(item => (all || (item.WarehouseId != null && mine.Contains(item.WarehouseId.Value)))
                && (item.Flags & (OperationsParcelFlags.InspectionPending | OperationsParcelFlags.InspectionInProgress)) != 0, cancellationToken),
            "holds" => _context.Parcels.CountAsync(item => (all || (item.WarehouseId != null && mine.Contains(item.WarehouseId.Value))) && item.HoldReasons != OperationsHoldReasons.None,
                cancellationToken),
            "tickets" => _context.Tickets.CountAsync(item => (all || mine.Contains(item.WarehouseId)) && item.Status == OperationsTicketStatus.Open
                && item.Topic == OperationsTicketTopic.Package, cancellationToken),
            "billing_tickets" => _context.Tickets.CountAsync(item => (all || mine.Contains(item.WarehouseId)) && item.Status == OperationsTicketStatus.Open
                && item.Topic == OperationsTicketTopic.Billing, cancellationToken),
            "service_tickets" => _context.Tickets.CountAsync(item => item.Status == OperationsTicketStatus.Open
                && (item.Topic == OperationsTicketTopic.PickupDelivery || item.Topic == OperationsTicketTopic.Claim || item.Topic == OperationsTicketTopic.Other), cancellationToken),
            "open_shipments" => _context.Shipments.CountAsync(item => (all || mine.Contains(item.OriginWarehouseId)) && item.Status == OperationsShipmentStatus.Open
                && item.DestinationCountryCode != null, cancellationToken),
            "to_dispatch" => _context.Dispatches.CountAsync(item => (all || mine.Contains(item.WarehouseId))
                && (item.Status == OperationsDispatchStatus.Planned || item.Status == OperationsDispatchStatus.Closed), cancellationToken),
            "arriving" => _context.Dispatches.CountAsync(item => item.Status == OperationsDispatchStatus.Departed && item.ReceivedUtc == null
                && (all || (item.DestinationWarehouseId != null && mine.Contains(item.DestinationWarehouseId.Value))), cancellationToken),
            "ready_for_pickup" => _context.Parcels.CountAsync(item => (all || (item.WarehouseId != null && mine.Contains(item.WarehouseId.Value)))
                && item.Milestone == OperationsMilestone.ReadyForPickup, cancellationToken),
            "payments_to_verify" => _context.Payments.CountAsync(item => (all || mine.Contains(item.WarehouseId)) && item.Pending, cancellationToken),
            "invoices_overdue" => _context.Invoices.CountAsync(item => (all || mine.Contains(item.WarehouseId)) && item.Status == OperationsInvoiceStatus.Issued
                && (item.DueOn ?? item.IssuedOn) < today && item.Total - item.CreditedTotal - item.PaidTotal - item.PendingTotal + item.RefundedTotal > 0m, cancellationToken),
            "credit_charges" => _context.Invoices.CountAsync(item => item.Status == OperationsInvoiceStatus.Issued && item.OnAccount == OperationsAccountCharge.AwaitingApproval,
                cancellationToken),
            "bills_to_approve" => _context.Bills.CountAsync(item => item.Status == OperationsBillStatus.Entered, cancellationToken),
            "bills_overdue" => _context.Bills.CountAsync(item => (item.Status == OperationsBillStatus.Entered || item.Status == OperationsBillStatus.Approved) && item.DueOn < today
                && item.Amount - item.PaidTotal > 0m, cancellationToken),
            "webhooks_off" => _context.WebhookEndpoints.CountAsync(item => item.DisabledForFailures && !item.Active, cancellationToken),
            _ => Task.FromResult(0)
        };
    }
}
