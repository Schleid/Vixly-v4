using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Tickets.ReplyMyOperationsTicket;

// =========================================================
// FILE: ReplyMyOperationsTicketUseCase.cs
//
// PURPOSE:
// The customer answers on its ticket; it is open again for staff (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Another customer's ticket answers "not found" (S-3)
// =========================================================
public sealed class ReplyMyOperationsTicketUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsTicketRepository _tickets;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public ReplyMyOperationsTicketUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsTicketRepository tickets,
        IOperationsUnitOfWork unitOfWork)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ReplyMyOperationsTicketResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ReplyMyOperationsTicketRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return ReplyMyOperationsTicketResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        if (await _tickets.GetAsync(request.TicketId, cancellationToken) is not { } ticket || ticket.CustomerId != signedIn.Customer.Id)
        {
            return ReplyMyOperationsTicketResponse.Failure(OperationsTicketErrors.NotFound);
        }

        if (!Vixly.Operations.Application.Receiving.OperationsCommandKey.IsValid(request.CommandKey))
        {
            return ReplyMyOperationsTicketResponse.Failure(Vixly.Operations.Domain.Tracking.OperationsTrackingErrors.IdempotencyKeyInvalid);
        }

        if (ticket.Repeats(request.CommandKey))
        {
            return ReplyMyOperationsTicketResponse.Success();
        }

        // A resolved ticket opens again only while no other report about the same package is open.
        if (ticket.Status == OperationsTicketStatus.Resolved && await _tickets.FindOpenAsync(ticket.CustomerId, ticket.TrackingNumber, cancellationToken) is not null)
        {
            return ReplyMyOperationsTicketResponse.Failure(OperationsTicketErrors.AnotherOpen);
        }

        try
        {
            ticket.CustomerReply(request.Message, _accounts.UtcNow);
            ticket.Keyed(request.CommandKey);
        }
        catch (DomainException exception)
        {
            return ReplyMyOperationsTicketResponse.Failure(exception.Error);
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ReplyMyOperationsTicketResponse.Success();
        }

        // Sent twice at once: the other request saved it, so this one is a repeat.
        _unitOfWork.DiscardChanges();
        return await _tickets.GetAsync(request.TicketId, cancellationToken) is { } saved && saved.Repeats(request.CommandKey)
            ? ReplyMyOperationsTicketResponse.Success()
            : ReplyMyOperationsTicketResponse.Failure(Vixly.Operations.Domain.Common.OperationsCommonErrors.ConcurrencyConflict);
    }
}
