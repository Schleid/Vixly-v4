using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformZelleNoticeHandler.cs
//
// PURPOSE:
// Sends the Zelle emails: "we received your report" and "we could not confirm it".
//
// RESPONSIBILITIES:
// - Read the report and email the tenant's billing contact in the tenant's language
//
// IMPORTANT NOTES:
// - A missing report or tenant sends nothing
// - A confirmed report is told by the receipt email, not here
// =========================================================
public sealed class PlatformZelleNoticeHandler : IPlatformOutboxHandler
{
    private readonly PlatformZelleAdministration _zelle;

    private readonly IPlatformZelleMailer _mailer;

    public PlatformZelleNoticeHandler(PlatformZelleAdministration zelle, IPlatformZelleMailer mailer)
    {
        _zelle = zelle ?? throw new ArgumentNullException(nameof(zelle));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
    }

    public string MessageType => PlatformMessageTypes.ZelleNotice;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformZelleNotice notice = PlatformOutboxWriter.Read<PlatformZelleNotice>(message);
        PlatformZelleClaim? claim = await _zelle.Store.GetClaimAsync(notice.ClaimId, cancellationToken);
        PlatformTenant? tenant = claim is null ? null : await _zelle.Payments.Tenants.GetAsync(claim.PlatformTenantId, cancellationToken);

        if (claim is null || tenant is null || tenant.Contacts.Count == 0)
        {
            return;
        }

        await _mailer.SendAsync(
            tenant,
            tenant.ContactFor(PlatformTenantContactRole.Billing),
            new PlatformZelleMail(notice.Kind, claim.InvoiceNumber, claim.Amount, claim.CurrencyCode, claim.SentOn, notice.Kind == PlatformZelleNoticeKind.Rejected ? claim.RejectReason : null),
            cancellationToken);
    }
}
