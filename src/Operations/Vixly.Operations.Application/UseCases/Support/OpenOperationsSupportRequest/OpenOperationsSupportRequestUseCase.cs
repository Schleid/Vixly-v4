using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Support;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Support;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Support.OpenOperationsSupportRequest;

// =========================================================
// FILE: OpenOperationsSupportRequestUseCase.cs
//
// PURPOSE:
// A support agent opens a call ticket for staff: what the caller asks about their shipment or package (owner, 2026-10-29, S-3).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The agent changes nothing else; staff act on the ticket
// =========================================================
public sealed class OpenOperationsSupportRequestUseCase
{
    private readonly OperationsAccess _access;

    private readonly OperationsSupportDesk _desk;

    private readonly IOperationsTicketRepository _tickets;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public OpenOperationsSupportRequestUseCase(OperationsAccess access, OperationsSupportDesk desk, IOperationsTicketRepository tickets, OperationsAuditTrail audit, IOperationsUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<OpenOperationsSupportRequestResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        OpenOperationsSupportRequestRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _access.LoadAsync(context, cancellationToken) is not { } access)
        {
            return OpenOperationsSupportRequestResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (!access.HasTenantWide(OperationsPermissions.SupportLookup))
        {
            return OpenOperationsSupportRequestResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        // Only once the caller's phone and the tracking number matched (design 9.3).
        if (await _desk.MatchAsync(request.CallerId, request.Number, cancellationToken) is not { } match)
        {
            return OpenOperationsSupportRequestResponse.Failure(OperationsSupportErrors.NotFound);
        }

        Vixly.Operations.Domain.Tickets.OperationsTicket opened;

        // Owner, 2026-10-29 (ticket desk): a ticket of the desk, for the topic's department, never shown to the customer.
        try
        {
            opened = Vixly.Operations.Domain.Tickets.OperationsTicket.FromCall(context.RequiredTenantId, match.CustomerId, match.WarehouseId, match.ShipmentId, match.ParcelId,
                match.Number, request.Topic, access.User.Id, request.Text, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return OpenOperationsSupportRequestResponse.Failure(exception.Error);
        }

        _tickets.Add(opened);
        _audit.Record(access.User.Id, "ops.support.request_opened", "OperationsTicket", opened.Id, arguments: new { number = match.Number, topic = request.Topic.ToString() });
        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? OpenOperationsSupportRequestResponse.Success(opened.Id)
            : OpenOperationsSupportRequestResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
