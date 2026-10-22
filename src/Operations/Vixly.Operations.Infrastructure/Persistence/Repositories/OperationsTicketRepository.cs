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

    public Task<OperationsTicket?> FindOpenAsync(Guid customerId, string trackingNumber, CancellationToken cancellationToken) =>
        _context.Tickets.FirstOrDefaultAsync(item => item.CustomerId == customerId && item.TrackingNumber == trackingNumber && item.Status != OperationsTicketStatus.Resolved,
            cancellationToken);

    public async Task<IReadOnlyList<OperationsTicket>> ListForCustomerAsync(Guid customerId, int take, CancellationToken cancellationToken) =>
        await _context.Tickets.AsNoTracking().Where(item => item.CustomerId == customerId).OrderByDescending(item => item.CreatedUtc).Take(take).ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<OperationsTicket> Items, int TotalCount)> ListAsync(IReadOnlyCollection<Guid> warehouses, OperationsTicketStatus? status, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        List<Guid> shown = warehouses.ToList();
        IQueryable<OperationsTicket> query = _context.Tickets.AsNoTracking().Where(item => shown.Contains(item.WarehouseId));

        if (status is { } wanted)
        {
            query = query.Where(item => item.Status == wanted);
        }

        int total = await query.CountAsync(cancellationToken);
        List<OperationsTicket> items = await query.OrderBy(item => item.Status).ThenBy(item => item.CreatedUtc).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

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
