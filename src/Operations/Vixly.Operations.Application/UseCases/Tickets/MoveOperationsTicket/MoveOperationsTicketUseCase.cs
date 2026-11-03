using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tickets;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Tickets.MoveOperationsTicket;

// =========================================================
// FILE: MoveOperationsTicketUseCase.cs
//
// PURPOSE:
// Moves a ticket to another department by changing its topic (owner, 2026-10-29, ticket desk section 2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Only who answers the ticket now; the new department sees it, and nobody has taken it there yet
// =========================================================
public sealed class MoveOperationsTicketUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsTicketScope _scope;

    private readonly IOperationsTicketRepository _tickets;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public MoveOperationsTicketUseCase(OperationsAccess access, OperationsTicketScope scope, IOperationsTicketRepository tickets, OperationsAuditTrail audit, IOperationsUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<MoveOperationsTicketResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        MoveOperationsTicketRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _access.LoadAsync(context, cancellationToken) is not { } access)
        {
            return MoveOperationsTicketResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _tickets.GetAsync(request.TicketId, cancellationToken) is not { } ticket || await _scope.SeenAsync(access, ticket, cancellationToken) is not { } warehouse)
        {
            return MoveOperationsTicketResponse.Failure(OperationsTicketErrors.NotFound);
        }

        if (!OperationsTicketDepartments.Answers(access, ticket.Topic, warehouse))
        {
            return MoveOperationsTicketResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        // A customer's missing-package ticket stays with the warehouse: its package details mean nothing elsewhere.
        if (ticket.FromCustomer && ticket.Topic == OperationsTicketTopic.Package)
        {
            return MoveOperationsTicketResponse.Failure(OperationsTicketErrors.TopicInvalid);
        }

        OperationsTicketTopic before = ticket.Topic;

        try
        {
            ticket.Move(request.Topic, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return MoveOperationsTicketResponse.Failure(exception.Error);
        }

        _audit.Record(access.User.Id, "ops.tickets.moved", nameof(OperationsTicket), ticket.Id, before: new { topic = before.ToString() }, after: new { topic = request.Topic.ToString() });
        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? MoveOperationsTicketResponse.Success()
            : MoveOperationsTicketResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
