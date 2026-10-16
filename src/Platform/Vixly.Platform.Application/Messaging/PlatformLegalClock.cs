using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformLegalClock.cs
//
// PURPOSE:
// Sends the automatic Terms notices and reminders (D-Lg7).
//
// RESPONSIBILITIES:
// - For the newest Terms that need acceptance: the step due for each tenant that has not accepted
// - For a notice-only current version: one notice per tenant
//
// IMPORTANT NOTES:
// - Each step is sent once per tenant and version (unique index); after downtime only the latest step
// - At most 500 tenants per run
// =========================================================
public sealed class PlatformLegalClock
{
    private const int Batch = 500;

    private readonly PlatformLegalAdministration _legal;

    public PlatformLegalClock(PlatformLegalAdministration legal)
    {
        _legal = legal ?? throw new ArgumentNullException(nameof(legal));
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = _legal.UtcNow;
        int sent = 0;

        if (await _legal.Store.LatestRequiredTermsAsync(cancellationToken) is { PublishedUtc: { } published } required)
        {
            foreach (PlatformLegalTenantRow row in await _legal.Queries.ListTenantsToRemindAsync(required.Id, published, Batch, cancellationToken))
            {
                IReadOnlyList<PlatformTermsReminderStep> steps = await _legal.Store.ListReminderStepsAsync(row.PlatformTenantId, required.Id, cancellationToken);
                DateTimeOffset deadline = PlatformLegalRules.Deadline(required, row.ActivatedUtc);
                DateTimeOffset start = row.ActivatedUtc is { } activated && activated > published ? activated : published;

                if (PlatformLegalRules.DueStep(start, deadline, steps, utcNow) is { } step)
                {
                    _legal.Store.AddReminder(PlatformTermsReminder.Automatic(row.PlatformTenantId, required.Id, step, row.Language, utcNow));
                    _legal.Notice(row.PlatformTenantId, required.Id, step);
                    sent++;
                }
            }
        }

        if (await _legal.Store.GetPublishedAsync(PlatformLegalDocumentKind.Terms, cancellationToken) is { NeedsAcceptance: false } notice)
        {
            foreach (PlatformLegalTenantRow row in await _legal.Queries.ListTenantsToRemindAsync(notice.Id, null, Batch, cancellationToken))
            {
                _legal.Store.AddReminder(PlatformTermsReminder.Automatic(row.PlatformTenantId, notice.Id, PlatformTermsReminderStep.Published, row.Language, utcNow));
                _legal.Notice(row.PlatformTenantId, notice.Id, PlatformTermsReminderStep.Published);
                sent++;
            }
        }

        return sent > 0 && await _legal.SaveAsync(cancellationToken) ? sent : 0;
    }
}
