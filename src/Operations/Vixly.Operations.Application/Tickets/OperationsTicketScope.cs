using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tickets;

namespace Vixly.Operations.Application.Tickets;

// =========================================================
// FILE: OperationsTicketScope.cs
//
// PURPOSE:
// The tickets a staff member may see: per topic, the warehouses where their roles cover its department (owner, 2026-10-29, ticket desk).
//
// RESPONSIBILITIES:
// - Build the topic-to-warehouses scope of the desk list and its counters
// - Load one ticket's warehouse when the person sees that ticket
//
// IMPORTANT NOTES:
// - Active warehouses only, as the parcel lists
// - A ticket outside the scope is "not found", never "not allowed" (nothing is revealed)
// =========================================================
public sealed class OperationsTicketScope
{
    private readonly IOperationsLocationRepository _locations;

    public OperationsTicketScope(IOperationsLocationRepository locations)
    {
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
    }

    public async Task<(IReadOnlyDictionary<OperationsTicketTopic, IReadOnlyCollection<Guid>> Scope, IReadOnlyList<OperationsWarehouse> Warehouses)> ForAsync(OperationsAccessView access,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(access);

        List<OperationsWarehouse> active = (await _locations.ListWarehousesAsync(cancellationToken))
            .Where(warehouse => warehouse.Status == OperationsLocationStatus.Active).OrderBy(warehouse => warehouse.Code, StringComparer.Ordinal).ToList();
        Dictionary<OperationsTicketTopic, IReadOnlyCollection<Guid>> scope = new();

        foreach (OperationsTicketTopic topic in OperationsTicketDepartments.Topics)
        {
            List<Guid> seen = active.Where(warehouse => OperationsTicketDepartments.Sees(access, topic, warehouse)).Select(warehouse => warehouse.Id).ToList();

            if (seen.Count > 0)
            {
                scope[topic] = seen;
            }
        }

        HashSet<Guid> any = scope.Values.SelectMany(ids => ids).ToHashSet();
        return (scope, active.Where(warehouse => any.Contains(warehouse.Id)).ToList());
    }

    public async Task<OperationsWarehouse?> SeenAsync(OperationsAccessView access, OperationsTicket ticket, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(access);
        ArgumentNullException.ThrowIfNull(ticket);

        return await _locations.GetWarehouseAsync(ticket.WarehouseId, cancellationToken) is { } warehouse && OperationsTicketDepartments.Sees(access, ticket.Topic, warehouse)
            ? warehouse
            : null;
    }
}
