using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Subscriptions.PreviewPlatformSubscriptionChange;

// =========================================================
// FILE: PreviewPlatformSubscriptionChangeUseCase.cs
//
// PURPOSE:
// Shows what a plan, term or licence change would do and cost (D-S4, D-S5).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Uses the same Quote as ChangePlatformSubscription, so the preview and the change agree
// - Amounts before tax
// =========================================================
public sealed class PreviewPlatformSubscriptionChangeUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    public PreviewPlatformSubscriptionChangeUseCase(
        PlatformSubscriptionAdministration administration)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
    }

    public async Task<PreviewPlatformSubscriptionChangeResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        PreviewPlatformSubscriptionChangeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSubscriptionTarget target = await _administration.LoadAsync(context, PlatformPermissions.SubscriptionsView, request.PlatformTenantId, requireStepUp: false, cancellationToken);

        if (target.Subscription is not PlatformSubscription subscription)
        {
            return PreviewPlatformSubscriptionChangeResponse.Failure(target.Error!);
        }

        PlatformPlan? targetPlan = request.PlatformPlanId == target.Plan!.Id
            ? target.Plan
            : await _administration.Plans.GetAsync(request.PlatformPlanId, cancellationToken);

        if (targetPlan is null)
        {
            return PreviewPlatformSubscriptionChangeResponse.Failure(PlatformSubscriptionErrors.PlanNotAvailable);
        }

        PlatformPlanVersion ceilingVersion = targetPlan.Id == subscription.PlatformPlanId
            ? targetPlan.Current ?? targetPlan.Version(subscription.PlanVersionId)
            : targetPlan.Current ?? targetPlan.Versions[0];
        int maxLicences = await _administration.MaxLicencesAsync(subscription.PlatformTenantId, ceilingVersion, cancellationToken);
        int activeLicences = await _administration.ActiveLicencesAsync(subscription.PlatformTenantId, cancellationToken);
        PlatformSubscriptionQuote quote;

        try
        {
            quote = subscription.Quote(target.Plan, targetPlan, request.Term, request.Licences, maxLicences, activeLicences, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return PreviewPlatformSubscriptionChangeResponse.Failure(exception.Error);
        }

        return PreviewPlatformSubscriptionChangeResponse.Success(PlatformSubscriptionQuoteItem.From(quote));
    }
}
