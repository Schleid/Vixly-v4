using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.UseCases.Subscriptions.GetPlatformTenantSubscription;

// =========================================================
// FILE: GetPlatformTenantSubscriptionUseCase.cs
//
// PURPOSE:
// Reads a tenant's current subscription for the tenant page.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - A tenant without a current subscription gets an empty detail, its abilities and the plan options
// - Start is offered only while the tenant is draft, pending activation or active
// =========================================================
public sealed class GetPlatformTenantSubscriptionUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformSubscriptionQueries _queries;

    private readonly IPlatformTenantRepository _tenants;

    public GetPlatformTenantSubscriptionUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformSubscriptionQueries queries,
        IPlatformTenantRepository tenants)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<GetPlatformTenantSubscriptionResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformTenantSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _administration.AuthorizeAsync(context, PlatformPermissions.SubscriptionsView, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return GetPlatformTenantSubscriptionResponse.Failure(refused);
        }

        if (await _tenants.GetAsync(request.PlatformTenantId, cancellationToken) is not PlatformTenant tenant)
        {
            return GetPlatformTenantSubscriptionResponse.Failure(PlatformCommonErrors.NotFound);
        }

        LanguageCode language = PlatformLanguageInput.Read(request.Language);
        PlatformSubscription? subscription = await _administration.Subscriptions.GetCurrentAsync(request.PlatformTenantId, cancellationToken);
        PlatformSubscriptionAbilities abilities = await _administration.ReadAbilitiesAsync(context, subscription, tenant.CanStartSubscription, cancellationToken);
        IReadOnlyList<PlatformPlanOption> options = abilities.CanStart || abilities.CanChange
            ? await _administration.PlanOptionsAsync(request.PlatformTenantId, language, cancellationToken)
            : Array.Empty<PlatformPlanOption>();
        IReadOnlyList<PlatformTenantLimitOverride> open = await _administration.Overrides.ListOpenAsync(request.PlatformTenantId, cancellationToken);
        IReadOnlyDictionary<Guid, string> names = await _queries.StaffNamesAsync(open.Select(item => item.SetByUserId), cancellationToken);
        PlatformPlan? plan = subscription is null ? null : await _administration.Plans.GetAsync(subscription.PlatformPlanId, cancellationToken);
        PlatformPlanVersion? version = plan?.Versions.FirstOrDefault(item => item.Id == subscription!.PlanVersionId);
        IReadOnlyList<PlatformLimitItem> limits = await _administration.LimitsAsync(request.PlatformTenantId, version, names, cancellationToken);

        if (subscription is null || plan is null || version is null)
        {
            return GetPlatformTenantSubscriptionResponse.Success(null, abilities, options, limits);
        }

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
        bool newer = renewalVersion.Id != version.Id;
        decimal nextAmount = scheduled?.PeriodAmount
            ?? (renewalVersion.Offers(subscription.Term) ? PlatformPricing.PeriodAmount(renewalVersion, subscription.Term, subscription.Licences) : 0m);
        decimal periodAmount = PlatformPricing.PeriodAmount(version, subscription.Term, subscription.Licences);
        IReadOnlyDictionary<Guid, string> people = await _queries.StaffNamesAsync(
            subscription.Discounts.Where(item => item.GrantedByUserId is not null).Select(item => item.GrantedByUserId!.Value)
                .Concat(subscription.Charges.Where(item => item.AddedByUserId is not null).Select(item => item.AddedByUserId!.Value))
                .Distinct(),
            cancellationToken);

        PlatformTenantSubscriptionDetail detail = new(
            subscription.Id,
            subscription.PlatformTenantId,
            plan.Id,
            plan.Code,
            plan.NameIn(language),
            version.Number,
            newer,
            subscription.Status,
            subscription.Term,
            subscription.Licences,
            await _administration.ActiveLicencesAsync(subscription.PlatformTenantId, cancellationToken),
            version.PricePerLicence(subscription.Term),
            periodAmount,
            nextAmount,
            subscription.SetupFeeFor(version),
            subscription.SetupFeeCharged,
            version.CurrencyCode,
            subscription.TrialEndsUtc,
            subscription.PeriodStartUtc,
            subscription.PeriodEndUtc,
            subscription.CancelAtPeriodEnd,
            subscription.StatusChangedUtc,
            subscription.AccessAt(_administration.UtcNow, await _administration.AccessRulesAsync(cancellationToken)),
            scheduled,
            limits,
            version.Features.Select(item => item.Feature).OrderBy(feature => feature).ToArray(),
            subscription.CurrentPeriodOff(periodAmount),
            nextAmount - subscription.NextPeriodOff(nextAmount, _administration.UtcNow),
            subscription.SetupFeeOverride is not null,
            subscription.ActiveDiscount is { } active ? PlatformDiscountItem.From(active, subscription, people) : null,
            subscription.PendingDiscount is { } pending ? PlatformDiscountItem.From(pending, subscription, people) : null,
            subscription.Charges
                .OrderByDescending(item => item.CreatedUtc)
                .Select(item => new PlatformOneTimeChargeItem(
                    item.Id,
                    item.Description,
                    item.Amount,
                    item.CurrencyCode,
                    item.Status,
                    item.Reason,
                    item.AddedByUserId is Guid by && people.TryGetValue(by, out string? name) ? name : null,
                    item.CreatedUtc))
                .ToArray());

        return GetPlatformTenantSubscriptionResponse.Success(detail, abilities, options, limits);
    }
}
