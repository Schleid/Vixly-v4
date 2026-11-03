using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Messaging;

namespace Vixly.Platform.Application.SalesFeed;

// =========================================================
// FILE: PlatformSalesFeedAlertHandler.cs
//
// PURPOSE:
// Emails the staff who manage payments when the sales feed to TechOS needs a person.
//
// RESPONSIBILITIES:
// - Send the alert to every holder of the payments settings permission, once each
//
// IMPORTANT NOTES:
// - Runs from the outbox; a mail failure retries the message
// =========================================================
public sealed class PlatformSalesFeedAlertHandler : IPlatformOutboxHandler
{
    private readonly IPlatformRoleQueries _roles;

    private readonly IPlatformSalesFeedMailer _mailer;

    private readonly TimeProvider _timeProvider;

    public PlatformSalesFeedAlertHandler(IPlatformRoleQueries roles, IPlatformSalesFeedMailer mailer, TimeProvider timeProvider)
    {
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public string MessageType => PlatformMessageTypes.SalesFeedAlert;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformSalesFeedAlert alert = PlatformOutboxWriter.Read<PlatformSalesFeedAlert>(message);

        foreach (PlatformPermissionHolderItem holder in (await _roles.ListPermissionHoldersAsync(PlatformPermissions.PaymentsConfigure, _timeProvider.GetUtcNow(), cancellationToken)).DistinctBy(item => item.PlatformUserId))
        {
            await _mailer.SendAlertAsync(holder, alert, cancellationToken);
        }
    }
}
