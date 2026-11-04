using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformRenewalReminderClock.cs
//
// PURPOSE:
// Emails the billing contact 7 days before an annual renewal that will be charged to a saved card (2026-11-02).
//
// RESPONSIBILITIES:
// - Find active annual subscriptions renewing within 7 days, not cancelled at period end
// - Remind only when automatic charging is on and a default, unexpired card is saved
// - Say which card is charged and how the charge reads on the statement
// - Send once per renewal (the period end is recorded on the subscription)
//
// IMPORTANT NOTES:
// - Runs in the hourly clock; a reminder is saved as sent before the next one is looked at
// - The statement wording comes from Payments settings; left out when not set
// =========================================================
public sealed class PlatformRenewalReminderClock
{
    public const int BatchSize = 50;

    public static TimeSpan Ahead { get; } = TimeSpan.FromDays(7);

    private readonly IPlatformSubscriptionRepository _subscriptions;

    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformPlanRepository _plans;

    private readonly PlatformPaymentsAdministration _payments;

    private readonly IPlatformSubscriptionMailer _mailer;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public PlatformRenewalReminderClock(
        IPlatformSubscriptionRepository subscriptions,
        IPlatformTenantRepository tenants,
        IPlatformPlanRepository plans,
        PlatformPaymentsAdministration payments,
        IPlatformSubscriptionMailer mailer,
        IPlatformUnitOfWork unitOfWork)
    {
        _subscriptions = subscriptions ?? throw new ArgumentNullException(nameof(subscriptions));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _mailer = mailer ?? throw new ArgumentNullException(nameof(mailer));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<int> RunAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = _payments.UtcNow;
        string? wording = (await _payments.Store.GetMerchantAsync(cancellationToken))?.StatementWording;
        int sent = 0;

        foreach (PlatformSubscription subscription in await _subscriptions.ListRenewalRemindersDueAsync(utcNow, utcNow + Ahead, BatchSize, cancellationToken))
        {
            if (await _payments.Store.GetProfileAsync(subscription.PlatformTenantId, cancellationToken) is not { AutoCharge: true }
                || await _payments.DefaultCardAsync(subscription.PlatformTenantId, cancellationToken) is not { } card
                || card.IsExpiredAt(subscription.PeriodEndUtc!.Value)
                || await _tenants.GetAsync(subscription.PlatformTenantId, cancellationToken) is not { Contacts.Count: > 0 } tenant
                || await _plans.GetAsync(subscription.PlatformPlanId, cancellationToken) is not { } plan)
            {
                continue;
            }

            // Recorded first: a lost email is better than two.
            if (!subscription.MarkRenewalReminded(utcNow) || !await _unitOfWork.SaveChangesAsync(cancellationToken))
            {
                continue;
            }

            await _mailer.SendRenewalReminderAsync(
                tenant,
                tenant.ContactFor(PlatformTenantContactRole.Billing),
                plan.NameIn(tenant.DefaultLanguage),
                subscription.PeriodEndUtc!.Value,
                card.Brand,
                card.Last4,
                wording,
                cancellationToken);
            sent++;
        }

        return sent;
    }
}
