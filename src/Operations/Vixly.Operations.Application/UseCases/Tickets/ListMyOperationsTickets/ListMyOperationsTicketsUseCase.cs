using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Tickets.ListMyOperationsTickets;

// =========================================================
// FILE: ListMyOperationsTicketsUseCase.cs
//
// PURPOSE:
// The customer's tickets, newest first (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Only the customer's own (S-3)
// =========================================================
public sealed class ListMyOperationsTicketsUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsTicketRepository _tickets;

    private readonly OperationsShipmentIntake _intake;

    public ListMyOperationsTicketsUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsTicketRepository tickets,
        OperationsShipmentIntake intake)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _intake = intake ?? throw new ArgumentNullException(nameof(intake));
    }

    public async Task<ListMyOperationsTicketsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListMyOperationsTicketsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return ListMyOperationsTicketsResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        return ListMyOperationsTicketsResponse.Success(await _tickets.ListForCustomerAsync(signedIn.Customer.Id, 50, cancellationToken), await _intake.OriginsAsync(signedIn.Customer, cancellationToken));
    }
}
