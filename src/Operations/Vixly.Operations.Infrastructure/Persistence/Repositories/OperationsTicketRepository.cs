using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Tickets;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsTicketRepository.cs
//
// PURPOSE:
// EF Core store for the customers' tickets about missing packages (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Implement IOperationsTicketRepository
//
// IMPORTANT NOTES:
// - The tenant query filter scopes every read; lookalikes are packages nobody could place, or already the ticket's customer's
//   (given from the parcel page, or a link that stopped half-way)
// =========================================================
internal sealed class OperationsTicketRepository : IOperationsTicketRepository
{
    // The end of a tracking number customers often know (the carrier shows the last digits).
    private const int TrackingEndLength = 8;

    private readonly OperationsDbContext _context;

    public OperationsTicketRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsTicket ticket) => _context.Tickets.Add(ticket ?? throw new ArgumentNullException(nameof(ticket)));

    public Task<OperationsTicket?> GetAsync(Guid ticketId, CancellationToken cancellationToken) =>
        _context.Tickets.FirstOrDefaultAsync(item => item.Id == ticketId, cancellationToken);

    // The customer's own (an agent's tickets are never the customer's).
    public Task<OperationsTicket?> FindOpenAsync(Guid customerId, string trackingNumber, CancellationToken cancellationToken) =>
        _context.Tickets.FirstOrDefaultAsync(item => item.CustomerId == customerId && item.TrackingNumber == trackingNumber && item.Status != OperationsTicketStatus.Resolved
            && item.OpenedBy == OperationsTicketSource.Customer, cancellationToken);

    public async Task<IReadOnlyList<OperationsTicket>> ListForCustomerAsync(Guid customerId, int take, CancellationToken cancellationToken) =>
        await _context.Tickets.AsNoTracking().Where(item => item.CustomerId == customerId && item.OpenedBy == OperationsTicketSource.Customer)
            .OrderByDescending(item => item.CreatedUtc).Take(take).ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<OperationsTicket> Items, int TotalCount)> ListAsync(IReadOnlyDictionary<OperationsTicketTopic, IReadOnlyCollection<Guid>> scope,
        Vixly.Operations.Application.Tickets.OperationsTicketDeskFilter filter, int page, int pageSize, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(filter);

        // One list per topic: a ticket shows only where its department is the person's.
        List<Guid> package = Of(OperationsTicketTopic.Package), billing = Of(OperationsTicketTopic.Billing), pickup = Of(OperationsTicketTopic.PickupDelivery),
            claim = Of(OperationsTicketTopic.Claim), other = Of(OperationsTicketTopic.Other);
        IQueryable<OperationsTicket> query = _context.Tickets.AsNoTracking().Where(item =>
            (item.Topic == OperationsTicketTopic.Package && package.Contains(item.WarehouseId))
            || (item.Topic == OperationsTicketTopic.Billing && billing.Contains(item.WarehouseId))
            || (item.Topic == OperationsTicketTopic.PickupDelivery && pickup.Contains(item.WarehouseId))
            || (item.Topic == OperationsTicketTopic.Claim && claim.Contains(item.WarehouseId))
            || (item.Topic == OperationsTicketTopic.Other && other.Contains(item.WarehouseId)));

        if (filter.Topic is { } topic)
        {
            query = query.Where(item => item.Topic == topic);
        }

        if (filter.WarehouseId is { } warehouse)
        {
            query = query.Where(item => item.WarehouseId == warehouse);
        }

        if (filter.TakenBy is { } person)
        {
            query = query.Where(item => item.TakenByUserId == person);
        }

        query = filter.State switch
        {
            "new" => query.Where(item => item.Status == OperationsTicketStatus.Open && item.TakenByUserId == null),
            "taken" => query.Where(item => item.Status == OperationsTicketStatus.Open && item.TakenByUserId != null),
            "open" => query.Where(item => item.Status == OperationsTicketStatus.Open),
            "answered" => query.Where(item => item.Status == OperationsTicketStatus.Answered),
            "resolved" => query.Where(item => item.Status == OperationsTicketStatus.Resolved),
            "escalated" => query.Where(item => item.Status != OperationsTicketStatus.Resolved && item.EscalationsSent > 0),
            _ => query
        };

        int total = await query.CountAsync(cancellationToken);
        List<OperationsTicket> items = await query
            .OrderBy(item => item.Status == OperationsTicketStatus.Resolved)
            .ThenByDescending(item => item.EscalationsSent)
            .ThenBy(item => item.Status)
            .ThenBy(item => item.CreatedUtc)
            .Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);

        List<Guid> Of(OperationsTicketTopic topic) => scope.TryGetValue(topic, out IReadOnlyCollection<Guid>? ids) ? ids.ToList() : new List<Guid>();
    }

    public async Task<IReadOnlyList<OperationsTicket>> ListEscalatableAsync(DateTimeOffset openedBefore, int take, CancellationToken cancellationToken) =>
        await Escalatable(openedBefore).OrderBy(item => item.CreatedUtc).Take(take).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListTenantsWithEscalatableAsync(DateTimeOffset openedBefore, CancellationToken cancellationToken) =>
        await Escalatable(openedBefore).AsNoTracking().Select(item => item.TenantId).Distinct().OrderBy(id => id).Take(10_000).ToListAsync(cancellationToken);

    private IQueryable<OperationsTicket> Escalatable(DateTimeOffset openedBefore) =>
        _context.Tickets.Where(item => item.Status != OperationsTicketStatus.Resolved && item.EscalationsSent < 2 && item.CreatedUtc <= openedBefore);

    public async Task<IReadOnlyList<OperationsParcel>> ListLookalikesAsync(OperationsTicket ticket, int take, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ticket);
        string tracking = ticket.TrackingNumber;
        string end = tracking.Length > TrackingEndLength ? tracking[^TrackingEndLength..] : tracking;

        // The days around the delivery the customer gives; without one, the month before the ticket.
        DateTimeOffset from = ticket.DeliveredOn is { } day ? new DateTimeOffset(day.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(-2) : ticket.CreatedUtc.AddDays(-30);
        DateTimeOffset to = ticket.DeliveredOn is { } last ? new DateTimeOffset(last.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddDays(7) : ticket.CreatedUtc.AddDays(1);
        string carrier = ticket.Carrier;

        return await _context.Parcels.AsNoTracking()
            .Where(item => item.WarehouseId == ticket.WarehouseId && (item.CustomerId == null || item.CustomerId == ticket.CustomerId) && item.Milestone == OperationsMilestone.ReceivedAtOrigin)
            .Where(item => item.CarrierTrackingNumber == tracking || (item.CarrierTrackingNumber != null && item.CarrierTrackingNumber.EndsWith(end))
                || (item.Carrier == carrier && item.CreatedUtc >= from && item.CreatedUtc <= to))
            .OrderByDescending(item => item.CarrierTrackingNumber == tracking)
            .ThenByDescending(item => item.CarrierTrackingNumber != null && item.CarrierTrackingNumber.EndsWith(end))
            .ThenBy(item => item.CreatedUtc)
            .Take(take).ToListAsync(cancellationToken);
    }
}
