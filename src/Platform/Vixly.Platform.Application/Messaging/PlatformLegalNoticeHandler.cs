using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformLegalNoticeHandler.cs
//
// PURPOSE:
// Sends a Terms notice or reminder to the tenant's owner in the tenant's language (D-Lg7).
//
// RESPONSIBILITIES:
// - Read the version, the deadline and the summary in the tenant's language
// - Skip tenants that accepted meanwhile (reminders only)
//
// IMPORTANT NOTES:
// - A missing tenant or document sends nothing
// =========================================================
public sealed class PlatformLegalNoticeHandler : IPlatformOutboxHandler
{
    private readonly PlatformLegalAdministration _legal;

    private readonly IPlatformLegalMailer _mailer;

    public PlatformLegalNoticeHandler(PlatformLegalAdministration legal, IPlatformLegalMailer mailer)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
    }

    public string MessageType => PlatformMessageTypes.LegalNotice;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformLegalNotice notice = PlatformOutboxWriter.Read<PlatformLegalNotice>(message);
        PlatformTenant? tenant = await _legal.Tenants.GetAsync(notice.PlatformTenantId, cancellationToken);
        PlatformLegalDocument? document = await _legal.Store.GetDocumentAsync(notice.DocumentId, cancellationToken);

        if (tenant is null || document is null || tenant.Contacts.Count == 0 || !PlatformLegalGate.Asked(tenant))
        {
            return;
        }

        PlatformTenantLegalState state = await _legal.Legal.StateAsync(tenant, cancellationToken);

        // A reminder for terms already accepted is not sent; a notice-only notice always is.
        if (document.NeedsAcceptance && notice.Step != PlatformTermsReminderStep.Published && state.Status == PlatformLegalStatus.Accepted)
        {
            return;
        }

        PlatformLegalFileItem? file = (await _legal.Queries.ListFilesAsync(document.Id, cancellationToken)).FirstOrDefault(item => item.Language == tenant.DefaultLanguage.Value);

        await _mailer.SendAsync(
            tenant,
            tenant.ContactFor(PlatformTenantContactRole.Owner),
            new PlatformLegalMail(notice.Step, document.Version, document.NeedsAcceptance, document.NeedsAcceptance ? state.DeadlineUtc ?? document.DeadlineUtc : null, file?.Summary, _legal.Links.LegalLink(tenant.CurrentSlug), document.NeedsAcceptance && state.Overdue),
            cancellationToken);
    }
}
