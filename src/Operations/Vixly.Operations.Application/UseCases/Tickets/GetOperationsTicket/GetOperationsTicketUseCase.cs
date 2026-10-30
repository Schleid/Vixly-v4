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

namespace Vixly.Operations.Application.UseCases.Tickets.GetOperationsTicket;

// =========================================================
// FILE: GetOperationsTicketUseCase.cs
//
// PURPOSE:
// One ticket for staff: what the customer knows, the messages, and the undeclared packages that resemble it (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.parcels.view for the ticket's warehouse; answering needs ops.parcels.assign
// =========================================================
public sealed class GetOperationsTicketUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsTicketRepository _tickets;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsParcelReader _reader;

    public GetOperationsTicketUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsTicketRepository tickets,
        IOperationsCustomerRepository customers,
        OperationsParcelReader reader)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async Task<GetOperationsTicketResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsTicketRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsTicketResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _tickets.GetAsync(request.TicketId, cancellationToken) is not { } ticket
            || await _scope.CoveredAsync(access, OperationsPermissions.ParcelsView, ticket.WarehouseId, cancellationToken) is not { } warehouse)
        {
            return GetOperationsTicketResponse.Failure(OperationsTicketErrors.NotFound);
        }

        // The customer's code always (staff assign with it); the name only for staff who may see customers.
        Vixly.Operations.Domain.Customers.OperationsCustomer? customer = await _customers.GetAsync(ticket.CustomerId, cancellationToken);
        bool named = access.Has(OperationsPermissions.CustomersView) || access.IsAdministrator;
        IReadOnlyList<OperationsParcelSummary> lookalikes = ticket.Status == OperationsTicketStatus.Resolved
            ? Array.Empty<OperationsParcelSummary>()
            : await _reader.SummariesAsync(await _tickets.ListLookalikesAsync(ticket, 20, cancellationToken), cancellationToken);

        return GetOperationsTicketResponse.Success(ticket, warehouse, customer?.Code, named ? customer?.FullName : null, lookalikes,
            access.CoversWarehouse(OperationsPermissions.ParcelsAssign, warehouse.Id, warehouse.BranchId));
    }
}
