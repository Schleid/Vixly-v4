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

namespace Vixly.Operations.Application.UseCases.Tickets.TakeOperationsTicket;

// =========================================================
// FILE: TakeOperationsTicketUseCase.cs
//
// PURPOSE:
// Someone of the ticket's department takes it (I'm on it), gives it to a colleague of the department, or releases it (owner, 2026-10-29, ticket desk section 3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The person must answer the ticket's topic at its warehouse; a colleague given it must too
// =========================================================
public sealed class TakeOperationsTicketUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsTicketScope _scope;

    private readonly IOperationsTicketRepository _tickets;

    private readonly OperationsTicketPeople _people;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public TakeOperationsTicketUseCase(OperationsAccess access, OperationsTicketScope scope, IOperationsTicketRepository tickets, OperationsTicketPeople people, OperationsAuditTrail audit, IOperationsUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _people = people ?? throw new ArgumentNullException(nameof(people));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<TakeOperationsTicketResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        TakeOperationsTicketRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _access.LoadAsync(context, cancellationToken) is not { } access)
        {
            return TakeOperationsTicketResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _tickets.GetAsync(request.TicketId, cancellationToken) is not { } ticket || await _scope.SeenAsync(access, ticket, cancellationToken) is not { } warehouse)
        {
            return TakeOperationsTicketResponse.Failure(OperationsTicketErrors.NotFound);
        }

        if (!OperationsTicketDepartments.Answers(access, ticket.Topic, warehouse))
        {
            return TakeOperationsTicketResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        if (request.Release)
        {
            ticket.Release(utcNow);
        }
        else
        {
            Guid taker = request.GiveTo ?? access.User.Id;

            // Given only to someone of the same department, at the ticket's warehouse.
            if (taker != access.User.Id && (await _people.HandlersAsync(ticket.Topic, warehouse, cancellationToken)).All(user => user.Id != taker))
            {
                return TakeOperationsTicketResponse.Failure(OperationsTicketErrors.NotInDepartment);
            }

            ticket.Take(taker, utcNow);
        }

        _audit.Record(access.User.Id, request.Release ? "ops.tickets.released" : "ops.tickets.taken", nameof(OperationsTicket), ticket.Id, after: new { takenBy = ticket.TakenByUserId });
        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? TakeOperationsTicketResponse.Success()
            : TakeOperationsTicketResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
