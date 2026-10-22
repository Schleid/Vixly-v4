using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Tickets.ListOperationsTickets;

// =========================================================
// FILE: ListOperationsTicketsUseCase.cs
//
// PURPOSE:
// The tickets of the warehouses the staff member may see, open ones first (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.parcels.view for the warehouses shown
// =========================================================
public sealed class ListOperationsTicketsUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsTicketRepository _tickets;

    public ListOperationsTicketsUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsTicketRepository tickets)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
    }

    public async Task<ListOperationsTicketsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsTicketsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ListOperationsTicketsResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        IReadOnlyList<OperationsWarehouse> warehouses = await _scope.WarehousesAsync(access, OperationsPermissions.ParcelsView, false, cancellationToken);

        if (warehouses.Count == 0)
        {
            return ListOperationsTicketsResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        List<Guid> shown = warehouses.Where(warehouse => request.WarehouseId is null || warehouse.Id == request.WarehouseId).Select(warehouse => warehouse.Id).ToList();
        (IReadOnlyList<OperationsTicket> items, int total) = await _tickets.ListAsync(shown, request.Status, Math.Max(request.Page, 1), PageSize, cancellationToken);
        return ListOperationsTicketsResponse.Success(items, total, warehouses);
    }

    public const int PageSize = 25;
}
