using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformSalesLeadMailHandler.cs
//
// PURPOSE:
// Emails the staff who follow up leads when one arrives (Stage 2, D-U3).
//
// RESPONSIBILITIES:
// - Send one email to each holder of platform.leads.manage
//
// IMPORTANT NOTES:
// - A lead deleted before delivery is skipped
// - Each staff member gets the email in their own language
// =========================================================
public sealed class PlatformSalesLeadMailHandler : IPlatformOutboxHandler
{
    private readonly IPlatformSalesLeadRepository _leads;

    private readonly IPlatformRoleQueries _roles;

    private readonly IPlatformLeadMailer _mailer;

    private readonly TimeProvider _timeProvider;

    public PlatformSalesLeadMailHandler(IPlatformSalesLeadRepository leads, IPlatformRoleQueries roles, IPlatformLeadMailer mailer, TimeProvider timeProvider)
    {
        _leads = leads ?? throw new ArgumentNullException(nameof(leads));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public string MessageType => PlatformMessageTypes.SalesLeadReceived;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformSalesLeadReceived received = PlatformOutboxWriter.Read<PlatformSalesLeadReceived>(message);

        if (await _leads.GetAsync(received.LeadId, cancellationToken) is not PlatformSalesLead lead)
        {
            return;
        }

        foreach (PlatformPermissionHolderItem holder in (await _roles.ListPermissionHoldersAsync(PlatformPermissions.LeadsManage, _timeProvider.GetUtcNow(), cancellationToken)).DistinctBy(item => item.PlatformUserId))
        {
            await _mailer.SendLeadReceivedAsync(holder, lead, cancellationToken);
        }
    }
}
