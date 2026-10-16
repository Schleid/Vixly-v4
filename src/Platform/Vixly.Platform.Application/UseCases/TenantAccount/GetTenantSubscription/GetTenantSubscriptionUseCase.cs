using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.UseCases.TenantAccount.GetTenantSubscription;

// =========================================================
// FILE: GetTenantSubscriptionUseCase.cs
//
// PURPOSE:
// The tenant's own subscription (call 19).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel for the tenant in the service token only (S-13, D-K8)
// - Any tenant role; null when the tenant has no subscription
// =========================================================
public sealed class GetTenantSubscriptionUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    public GetTenantSubscriptionUseCase(
        PlatformSubscriptionAdministration administration)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
    }

    public async Task<GetTenantSubscriptionResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        GetTenantSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        LanguageCode language = PlatformLanguageInput.Read(request.Language);
        PlatformSubscription? subscription = await _administration.Subscriptions.GetCurrentAsync(context.PlatformTenantId, cancellationToken);
        PlatformPlan? plan = subscription is null ? null : await _administration.Plans.GetAsync(subscription.PlatformPlanId, cancellationToken);
        PlatformPlanVersion? version = plan?.Versions.FirstOrDefault(item => item.Id == subscription!.PlanVersionId);

        if (subscription is null || plan is null || version is null)
        {
            return GetTenantSubscriptionResponse.Success(null);
        }

        IReadOnlyList<PlatformLimitItem> limits = await _administration.LimitsAsync(context.PlatformTenantId, version, null, cancellationToken);
        PlatformScheduledChangeItem? scheduled = null;

        if (subscription.ScheduledChange is { } change)
        {
            PlatformPlan? targetPlan = change.TargetPlanId == plan.Id ? plan : await _administration.Plans.GetAsync(change.TargetPlanId, cancellationToken);
            PlatformPlanVersion? targetVersion = targetPlan is null ? null : targetPlan.Current ?? targetPlan.Versions.FirstOrDefault(item => item.Id == change.TargetVersionId);

            if (targetPlan is not null && targetVersion is not null)
            {
                scheduled = new PlatformScheduledChangeItem(
                    targetPlan.Code,
                    targetPlan.NameIn(language),
                    change.TargetTerm,
                    change.TargetLicences,
                    change.EffectiveUtc,
                    PlatformPricing.PeriodAmount(targetVersion, change.TargetTerm, change.TargetLicences));
            }
        }

        PlatformPlanVersion renewalVersion = plan.Current ?? version;
        decimal nextAmount = scheduled?.PeriodAmount
            ?? (renewalVersion.Offers(subscription.Term) ? PlatformPricing.PeriodAmount(renewalVersion, subscription.Term, subscription.Licences) : 0m);
        decimal periodAmount = PlatformPricing.PeriodAmount(version, subscription.Term, subscription.Licences);

        return GetTenantSubscriptionResponse.Success(new PlatformTenantSubscriptionView(
            plan.Code,
            plan.NameIn(language),
            version.Number,
            subscription.Status,
            subscription.Term,
            subscription.Licences,
            await _administration.ActiveLicencesAsync(context.PlatformTenantId, cancellationToken),
            version.PricePerLicence(subscription.Term),
            periodAmount,
            nextAmount - subscription.NextPeriodOff(nextAmount, _administration.UtcNow),
            version.CurrencyCode,
            subscription.TrialEndsUtc,
            subscription.PeriodStartUtc,
            subscription.PeriodEndUtc,
            subscription.CancelAtPeriodEnd,
            subscription.AccessAt(_administration.UtcNow, await _administration.AccessRulesAsync(cancellationToken)),
            scheduled,
            limits.ToDictionary(item => item.Kind, item => item.Effective),
            version.Features.Select(item => item.Feature).OrderBy(feature => feature).ToArray()));
    }
}
