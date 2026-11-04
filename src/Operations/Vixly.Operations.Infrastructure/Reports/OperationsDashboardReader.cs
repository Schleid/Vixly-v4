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
// Counts what the staff home page and the menu show (Slice 11).
//
// RESPONSIBILITIES:
// - Every asked counter in one database command (harmonization H-3, H-4), each over the warehouses given
//
// IMPORTANT NOTES:
// - Read only; each count is an ordinary tenant-filtered query inside the one command (the company filter applies to every one)
// - A counter not asked is left out of the command, not counted and hidden
// - The same rules as the lists the counters open
// =========================================================
internal sealed class OperationsDashboardReader : IOperationsDashboardReader
{
    // The order of the counts in the command.
    private static readonly string[] Keys =
    {
        "to_inspect", "holds", "tickets", "billing_tickets", "service_tickets", "open_shipments", "to_dispatch", "arriving", "ready_for_pickup",
        "payments_to_verify", "invoices_overdue", "credit_charges", "bills_to_approve", "bills_overdue", "webhooks_off", "reviews_new"
    };

    private readonly OperationsDbContext _context;

    public OperationsDashboardReader(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyDictionary<string, int>> CountManyAsync(IReadOnlyDictionary<string, IReadOnlyCollection<Guid>?> asks, DateOnly today, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(asks);
        Dictionary<string, int> counts = asks.Keys.ToDictionary(key => key, _ => 0, StringComparer.Ordinal);

        if (!Keys.Any(asks.ContainsKey))
        {
            return counts;
        }

        Ask inspect = Of(asks, "to_inspect"), holds = Of(asks, "holds"), tickets = Of(asks, "tickets"), billing = Of(asks, "billing_tickets"), service = Of(asks, "service_tickets"),
            open = Of(asks, "open_shipments"), dispatch = Of(asks, "to_dispatch"), arriving = Of(asks, "arriving"), pickup = Of(asks, "ready_for_pickup"),
            verify = Of(asks, "payments_to_verify"), overdue = Of(asks, "invoices_overdue"), credit = Of(asks, "credit_charges"), approve = Of(asks, "bills_to_approve"),
            billsOverdue = Of(asks, "bills_overdue"), webhooks = Of(asks, "webhooks_off"), reviews = Of(asks, "reviews_new");

        // One row, one column per asked counter; EF leaves the counters not asked out of the command.
        int[] values = await _context.Database.SqlQuery<int>($"SELECT 1 AS [Value]").Select(_ => new[]
        {
            inspect.On ? _context.Parcels.Count(item => (inspect.All || (item.WarehouseId != null && inspect.Mine.Contains(item.WarehouseId.Value)))
                && (item.Flags & (OperationsParcelFlags.InspectionPending | OperationsParcelFlags.InspectionInProgress)) != 0) : 0,
            holds.On ? _context.Parcels.Count(item => (holds.All || (item.WarehouseId != null && holds.Mine.Contains(item.WarehouseId.Value))) && item.HoldReasons != OperationsHoldReasons.None) : 0,
            tickets.On ? _context.Tickets.Count(item => (tickets.All || tickets.Mine.Contains(item.WarehouseId)) && item.Status == OperationsTicketStatus.Open
                && item.Topic == OperationsTicketTopic.Package) : 0,
            billing.On ? _context.Tickets.Count(item => (billing.All || billing.Mine.Contains(item.WarehouseId)) && item.Status == OperationsTicketStatus.Open
                && item.Topic == OperationsTicketTopic.Billing) : 0,
            service.On ? _context.Tickets.Count(item => item.Status == OperationsTicketStatus.Open
                && (item.Topic == OperationsTicketTopic.PickupDelivery || item.Topic == OperationsTicketTopic.Claim || item.Topic == OperationsTicketTopic.Other)) : 0,
            open.On ? _context.Shipments.Count(item => (open.All || open.Mine.Contains(item.OriginWarehouseId)) && item.Status == OperationsShipmentStatus.Open
                && item.DestinationCountryCode != null) : 0,
            dispatch.On ? _context.Dispatches.Count(item => (dispatch.All || dispatch.Mine.Contains(item.WarehouseId))
                && (item.Status == OperationsDispatchStatus.Planned || item.Status == OperationsDispatchStatus.Closed)) : 0,
            arriving.On ? _context.Dispatches.Count(item => item.Status == OperationsDispatchStatus.Departed && item.ReceivedUtc == null
                && (arriving.All || (item.DestinationWarehouseId != null && arriving.Mine.Contains(item.DestinationWarehouseId.Value)))) : 0,
            pickup.On ? _context.Parcels.Count(item => (pickup.All || (item.WarehouseId != null && pickup.Mine.Contains(item.WarehouseId.Value)))
                && item.Milestone == OperationsMilestone.ReadyForPickup) : 0,
            verify.On ? _context.Payments.Count(item => (verify.All || verify.Mine.Contains(item.WarehouseId)) && item.Pending) : 0,
            overdue.On ? _context.Invoices.Count(item => (overdue.All || overdue.Mine.Contains(item.WarehouseId)) && item.Status == OperationsInvoiceStatus.Issued
                && (item.DueOn ?? item.IssuedOn) < today && item.Total - item.CreditedTotal - item.PaidTotal - item.PendingTotal + item.RefundedTotal > 0m) : 0,
            credit.On ? _context.Invoices.Count(item => item.Status == OperationsInvoiceStatus.Issued && item.OnAccount == OperationsAccountCharge.AwaitingApproval) : 0,
            approve.On ? _context.Bills.Count(item => item.Status == OperationsBillStatus.Entered) : 0,
            billsOverdue.On ? _context.Bills.Count(item => (item.Status == OperationsBillStatus.Entered || item.Status == OperationsBillStatus.Approved) && item.DueOn < today
                && item.Amount - item.PaidTotal > 0m) : 0,
            webhooks.On ? _context.WebhookEndpoints.Count(item => item.DisabledForFailures && !item.Active) : 0,
            reviews.On ? _context.SiteTestimonials.Count(item => item.CustomerId != null && item.ReviewedUtc == null) : 0
        }).SingleAsync(cancellationToken);

        for (int index = 0; index < Keys.Length; index++)
        {
            if (asks.ContainsKey(Keys[index]))
            {
                counts[Keys[index]] = values[index];
            }
        }

        return counts;
    }

    private static Ask Of(IReadOnlyDictionary<string, IReadOnlyCollection<Guid>?> asks, string key) =>
        asks.TryGetValue(key, out IReadOnlyCollection<Guid>? warehouses)
            ? new Ask(true, warehouses is null, warehouses?.ToList() ?? new List<Guid>())
            : new Ask(false, false, new List<Guid>());

    // Whether a counter is asked, over the whole company or the given warehouses.
    private sealed record Ask(bool On, bool All, List<Guid> Mine);
}
