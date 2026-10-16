using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Messaging;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformApprovalRequestHandler.cs
//
// PURPOSE:
// Emails the approvers when something waits for them (D-S24, D-S26).
//
// RESPONSIBILITIES:
// - Find every holder of the approving permission
// - Email each of them except the requester
//
// IMPORTANT NOTES:
// - Runs from the outbox after the request is saved
// - Holders are read at sending time, so a role change is respected
// =========================================================
public sealed class PlatformApprovalRequestHandler : IPlatformOutboxHandler
{
    private readonly IPlatformRoleQueries _roles;

    private readonly IPlatformSalesMailer _mailer;

    private readonly TimeProvider _timeProvider;

    public PlatformApprovalRequestHandler(IPlatformRoleQueries roles, IPlatformSalesMailer mailer, TimeProvider timeProvider)
    {
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public string MessageType => PlatformMessageTypes.ApprovalRequested;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformApprovalRequested request = PlatformOutboxWriter.Read<PlatformApprovalRequested>(message);
        string kindKey = "ui.approvals.kind." + request.Kind switch
        {
            PlatformApprovalKind.Discount => "discount",
            PlatformApprovalKind.Offer => "offer",
            PlatformApprovalKind.Promotion => "promotion",
            PlatformApprovalKind.CreditNote => "credit_note",
            _ => "plan_version"
        };

        foreach (PlatformPermissionHolderItem holder in (await _roles.ListPermissionHoldersAsync(request.PermissionCode, _timeProvider.GetUtcNow(), cancellationToken)).DistinctBy(item => item.PlatformUserId))
        {
            if (holder.PlatformUserId != request.RequestedByUserId)
            {
                await _mailer.SendApprovalRequestAsync(holder, kindKey, request.Title, cancellationToken);
            }
        }
    }
}
