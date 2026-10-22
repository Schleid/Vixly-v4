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

namespace Vixly.Operations.Application.UseCases.Tickets.AnswerOperationsTicket;

// =========================================================
// FILE: AnswerOperationsTicketUseCase.cs
//
// PURPOSE:
// Staff answer a ticket, or resolve it with the package they found (now the customer's) or a last word (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.parcels.assign for the ticket's warehouse
// - The customer gets an email with a link to the ticket
// =========================================================
public sealed class AnswerOperationsTicketUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsTicketRepository _tickets;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly OperationsOutboxWriter _outbox;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public AnswerOperationsTicketUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsTicketRepository tickets,
        IOperationsTrackingRepository tracking,
        OperationsOutboxWriter outbox,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<AnswerOperationsTicketResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        AnswerOperationsTicketRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return AnswerOperationsTicketResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _tickets.GetAsync(request.TicketId, cancellationToken) is not { } ticket
            || await _scope.CoveredAsync(access, OperationsPermissions.ParcelsView, ticket.WarehouseId, cancellationToken) is not { } warehouse)
        {
            return AnswerOperationsTicketResponse.Failure(OperationsTicketErrors.NotFound);
        }

        if (!access.CoversWarehouse(OperationsPermissions.ParcelsAssign, warehouse.Id, warehouse.BranchId))
        {
            return AnswerOperationsTicketResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if (!OperationsCommandKey.IsValid(request.CommandKey))
        {
            return AnswerOperationsTicketResponse.Failure(Vixly.Operations.Domain.Tracking.OperationsTrackingErrors.IdempotencyKeyInvalid);
        }

        // The same form sent again (a double click, a resubmit): already done, nothing new and no second email (ADR-0019).
        if (ticket.Repeats(request.CommandKey))
        {
            return AnswerOperationsTicketResponse.Success();
        }

        // A package linked only once it is the ticket's customer's, at the ticket's warehouse (staff assign it first, with what the customer proved).
        if (request.ParcelId is { } parcelId
            && (await _tracking.GetParcelAsync(parcelId, cancellationToken) is not { } parcel || parcel.CustomerId != ticket.CustomerId || parcel.WarehouseId != ticket.WarehouseId))
        {
            return AnswerOperationsTicketResponse.Failure(OperationsTicketErrors.ParcelNotTheirs);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            if (request.Resolve)
            {
                ticket.Resolve(access.User.Id, request.ParcelId, request.Message, utcNow);
            }
            else
            {
                ticket.StaffReply(access.User.Id, request.Message, utcNow);
            }
        }
        catch (DomainException exception)
        {
            return AnswerOperationsTicketResponse.Failure(exception.Error);
        }

        ticket.Keyed(request.CommandKey);
        _outbox.Write(Vixly.Operations.Application.Tickets.OperationsTicketNotice.MessageType, context.RequiredTenantId, ticket.Id, new Vixly.Operations.Application.Tickets.OperationsTicketNotice(ticket.CustomerId, ticket.Id, request.PortalSlug));
        _audit.Record(access.User.Id, request.Resolve ? "ops.tickets.resolved" : "ops.tickets.answered", nameof(OperationsTicket), ticket.Id, after: new { parcel = request.ParcelId });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return AnswerOperationsTicketResponse.Success();
        }

        // Sent twice at once: the other request saved it, so this one is a repeat.
        _unitOfWork.DiscardChanges();
        return await _tickets.GetAsync(request.TicketId, cancellationToken) is { } saved && saved.Repeats(request.CommandKey)
            ? AnswerOperationsTicketResponse.Success()
            : AnswerOperationsTicketResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
