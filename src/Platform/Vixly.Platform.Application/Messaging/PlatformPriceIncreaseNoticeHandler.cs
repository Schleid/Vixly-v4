using Vixly.Platform.Application.Ports;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformPriceIncreaseNoticeHandler.cs
//
// PURPOSE:
// Tells subscribers when a new plan version raises their next price (D-S13).
//
// RESPONSIBILITIES:
// - For each current subscription on the plan whose next period costs more, email the billing contact (or the owner)
//
// IMPORTANT NOTES:
// - No notice for decreases, for a term the new version no longer offers, or when a change to another plan is scheduled
// - The current period never changes (owner, D-S2); the notice gives the date the new price applies
// =========================================================
public sealed class PlatformPriceIncreaseNoticeHandler : IPlatformOutboxHandler
{
    private readonly IPlatformPlanRepository _plans;

    private readonly IPlatformSubscriptionRepository _subscriptions;

    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformSubscriptionMailer _mailer;

    public PlatformPriceIncreaseNoticeHandler(
        IPlatformPlanRepository plans,
        IPlatformSubscriptionRepository subscriptions,
        IPlatformTenantRepository tenants,
        IPlatformSubscriptionMailer mailer)
    {
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
    }

    public string MessageType => PlatformMessageTypes.PlanVersionPublished;

    public async Task HandleAsync(PlatformOutboxMessage message, CancellationToken cancellationToken)
    {
        PlatformPlanVersionPublished published = PlatformOutboxWriter.Read<PlatformPlanVersionPublished>(message);

        if (published.PreviousVersionId is null)
        {
            return;
        }

        PlatformPlan? plan = await _plans.GetAsync(published.PlatformPlanId, cancellationToken);
        PlatformPlanVersion? next = plan?.Versions.FirstOrDefault(version => version.Id == published.VersionId);

        if (plan is null || next is null)
        {
            return;
        }

        foreach (PlatformSubscription subscription in await _subscriptions.ListCurrentOnPlanAsync(plan.Id, cancellationToken))
        {
            PlatformPlanVersion? current = plan.Versions.FirstOrDefault(version => version.Id == subscription.PlanVersionId);

            if (current is null
                || current.Id == next.Id
                || !next.Offers(subscription.Term)
                || (subscription.ScheduledChange is { } change && change.TargetPlanId != plan.Id))
            {
                continue;
            }

            int licences = subscription.ScheduledChange?.TargetLicences ?? subscription.Licences;
            decimal before = PlatformPricing.PeriodAmount(current, subscription.Term, subscription.Licences);
            decimal after = PlatformPricing.PeriodAmount(next, subscription.ScheduledChange?.TargetTerm ?? subscription.Term, licences);

            if (after <= before)
            {
                continue;
            }

            PlatformTenant? tenant = await _tenants.GetAsync(subscription.PlatformTenantId, cancellationToken);

            if (tenant is null || tenant.Contacts.Count == 0)
            {
                continue;
            }

            DateTimeOffset effective = subscription.PeriodEndUtc ?? subscription.TrialEndsUtc ?? published.OccurredUtc;

            await _mailer.SendPriceIncreaseAsync(
                tenant,
                tenant.ContactFor(PlatformTenantContactRole.Billing),
                plan.NameIn(tenant.DefaultLanguage),
                subscription.Term,
                before,
                after,
                next.CurrencyCode,
                effective,
                cancellationToken);
        }
    }
}
