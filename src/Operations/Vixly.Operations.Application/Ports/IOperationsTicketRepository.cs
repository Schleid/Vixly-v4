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
// - The ticket desk's list by topic and warehouse, and the tickets waiting for an escalation
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

    // Staff (ticket desk, owner 2026-10-29): the tickets of each topic at the warehouses where the person sees it; escalated first, then the oldest.
    Task<(IReadOnlyList<OperationsTicket> Items, int TotalCount)> ListAsync(IReadOnlyDictionary<OperationsTicketTopic, IReadOnlyCollection<Guid>> scope,
        Vixly.Operations.Application.Tickets.OperationsTicketDeskFilter filter, int page, int pageSize, CancellationToken cancellationToken);

    // Not resolved, with an escalation step left, opened before this time (the escalation job checks each).
    Task<IReadOnlyList<OperationsTicket>> ListEscalatableAsync(DateTimeOffset openedBefore, int take, CancellationToken cancellationToken);

    // System scope: the companies with such tickets.
    Task<IReadOnlyList<Guid>> ListTenantsWithEscalatableAsync(DateTimeOffset openedBefore, CancellationToken cancellationToken);

    // Packages at the ticket's warehouse nobody could place (or already the ticket's customer's), with the same tracking number (or its end), or the same carrier near the day it was delivered.
    Task<IReadOnlyList<OperationsParcel>> ListLookalikesAsync(OperationsTicket ticket, int take, CancellationToken cancellationToken);
}
