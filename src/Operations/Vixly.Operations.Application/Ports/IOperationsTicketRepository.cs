using Vixly.Operations.Domain.Tickets;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsTicketRepository.cs
//
// PURPOSE:
// Stores the customers' tickets about missing packages (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Add, read and list tickets; the packages that resemble a ticket, for staff
//
// IMPORTANT NOTES:
// - Scoped to the current tenant by the query filter
// =========================================================
public interface IOperationsTicketRepository
{
    void Add(OperationsTicket ticket);

    Task<OperationsTicket?> GetAsync(Guid ticketId, CancellationToken cancellationToken);

    // The customer's ticket not yet resolved about this package (one at a time).
    Task<OperationsTicket?> FindOpenAsync(Guid customerId, string trackingNumber, CancellationToken cancellationToken);

    // The customer's own, newest first.
    Task<IReadOnlyList<OperationsTicket>> ListForCustomerAsync(Guid customerId, int take, CancellationToken cancellationToken);

    // Staff: the tickets of the warehouses, oldest open first; null status: every ticket.
    Task<(IReadOnlyList<OperationsTicket> Items, int TotalCount)> ListAsync(IReadOnlyCollection<Guid> warehouses, OperationsTicketStatus? status, int page, int pageSize,
        CancellationToken cancellationToken);

    // Packages at the ticket's warehouse nobody could place (or already the ticket's customer's), with the same tracking number (or its end), or the same carrier near the day it was delivered.
    Task<IReadOnlyList<OperationsParcel>> ListLookalikesAsync(OperationsTicket ticket, int take, CancellationToken cancellationToken);
}
