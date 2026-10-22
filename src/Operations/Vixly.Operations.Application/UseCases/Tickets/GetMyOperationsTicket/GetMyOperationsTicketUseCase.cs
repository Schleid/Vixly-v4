using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Tickets.GetMyOperationsTicket;

// =========================================================
// FILE: GetMyOperationsTicketUseCase.cs
//
// PURPOSE:
// One of the customer's tickets with its messages (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Another customer's ticket answers "not found" (S-3)
// =========================================================
public sealed class GetMyOperationsTicketUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsTicketRepository _tickets;

    public GetMyOperationsTicketUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsTicketRepository tickets)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
    }

    public async Task<GetMyOperationsTicketResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsTicketRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return GetMyOperationsTicketResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        return await _tickets.GetAsync(request.TicketId, cancellationToken) is { } ticket && ticket.CustomerId == signedIn.Customer.Id
            ? GetMyOperationsTicketResponse.Success(ticket)
            : GetMyOperationsTicketResponse.Failure(OperationsTicketErrors.NotFound);
    }
}
