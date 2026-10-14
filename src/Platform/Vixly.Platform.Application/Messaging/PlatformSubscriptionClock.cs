using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformSubscriptionClock.cs
//
// PURPOSE:
// Moves subscriptions whose trial or period has ended (D-S12, owner D-S2).
//
// RESPONSIBILITIES:
// - Find subscriptions that are due
// - Expire trials; renew periods on the current version or the scheduled change; end those cancelled at the period end
// - Record history, audit and the message for each move
// - Start a subscription whose trial ended with nothing to pay (free months)
// - Announce discounts that end soon, when the Sales setting says how early (D-S25)
//
// IMPORTANT NOTES:
// - Runs once an hour from Platform.Web until the Worker (Slice 4)
// - Each subscription is saved on its own; the row version makes a second host lose the race, so a subscription is handled once
// - Renewal does not take payment: Billing turns the renewed message into an invoice (Slice 4)
// =========================================================
public sealed class PlatformSubscriptionClock
{
    public const int BatchSize = 50;

    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly IPlatformSalesSettingsRepository _salesSettings;

    private readonly PlatformOutboxWriter _outbox;

    public PlatformSubscriptionClock(
        PlatformSubscriptionAdministration administration,
        IPlatformUnitOfWork unitOfWork,
        IPlatformSalesSettingsRepository salesSettings,
        PlatformOutboxWriter outbox)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _salesSettings = salesSettings ?? throw new ArgumentNullException(nameof(salesSettings));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
    }

    public async Task<int> ProcessDueAsync(CancellationToken cancellationToken)
    {
        DateTimeOffset utcNow = _administration.UtcNow;
        IReadOnlyList<Guid> due = await _administration.Subscriptions.ListDueAsync(utcNow, BatchSize, cancellationToken);
        int handled = 0;

        foreach (Guid subscriptionId in due)
        {
            if (await ProcessOneAsync(subscriptionId, utcNow, cancellationToken))
            {
                handled++;
            }
        }

        return handled + await AnnounceEndingDiscountsAsync(utcNow, cancellationToken);
    }

    // D-S25: only when the sales manager has set how many days before.
    private async Task<int> AnnounceEndingDiscountsAsync(DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        if ((await _salesSettings.GetAsync(cancellationToken))?.DiscountEndNoticeDays is not int days)
        {
            return 0;
        }

        int announced = 0;

        foreach (PlatformSubscription subscription in await _administration.Subscriptions.ListWithUnnoticedDiscountsAsync(BatchSize, cancellationToken))
        {
            if (subscription.ActiveDiscount is not { } discount
                || discount.LastsUntil(subscription.PeriodEndUtc, subscription.Term) is not DateTimeOffset ends
                || ends > utcNow.AddDays(days))
            {
                continue;
            }

            discount.RecordEndNotice(utcNow);
            _outbox.Write(
                PlatformMessageTypes.DiscountEnding,
                subscription.PlatformTenantId,
                new PlatformDiscountEnding(subscription.Id, subscription.PlatformTenantId, discount.Id, ends, utcNow));

            if (await _unitOfWork.SaveChangesAsync(cancellationToken))
            {
                announced++;
            }
            else
            {
                _unitOfWork.DiscardChanges();
            }
        }

        return announced;
    }

    private async Task<bool> ProcessOneAsync(Guid subscriptionId, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        PlatformSubscription? subscription = await _administration.Subscriptions.GetAsync(subscriptionId, cancellationToken);
        PlatformPlan? plan = subscription is null ? null : await _administration.Plans.GetAsync(subscription.PlatformPlanId, cancellationToken);

        if (subscription is null || plan is null)
        {
            return false;
        }

        try
        {
            if (subscription.ExpireTrialIfDue(utcNow) is { } expired)
            {
                _administration.RecordChange(null, subscription, plan, expired, null, "platform.subscriptions.trial_expired");

                // Free months after a trial need no payment.
                if (plan.Versions.FirstOrDefault(item => item.Id == subscription.PlanVersionId) is { } version
                    && subscription.ActivateIfNothingDue(version, utcNow) is { } activated)
                {
                    _administration.RecordChange(null, subscription, plan, activated, null, "platform.subscriptions.activated_nothing_due");
                }
            }

            PlatformPlan? scheduledPlan = subscription.ScheduledChange is { } change && change.TargetPlanId != plan.Id
                ? await _administration.Plans.GetAsync(change.TargetPlanId, cancellationToken)
                : plan;

            if (subscription.RenewIfDue(plan, scheduledPlan, utcNow) is { } renewal)
            {
                PlatformPlan renewedOn = scheduledPlan is not null && scheduledPlan.Id == subscription.PlatformPlanId ? scheduledPlan : plan;
                _administration.RecordChange(
                    null,
                    subscription,
                    renewedOn,
                    renewal.Event,
                    null,
                    renewal.Ended ? "platform.subscriptions.ended" : "platform.subscriptions.renewed");

                if (renewal.DiscountEnded is { } discountEnded)
                {
                    _administration.RecordChange(null, subscription, renewedOn, discountEnded, null, "platform.discounts.ended");
                }
            }
        }
        catch (DomainException)
        {
            _unitOfWork.DiscardChanges();
            return false;
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return true;
        }

        _unitOfWork.DiscardChanges();
        return false;
    }
}
