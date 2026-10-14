using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Subscriptions.ChangePlatformSubscription;

// =========================================================
// FILE: ChangePlatformSubscriptionUseCase.cs
//
// PURPOSE:
// Changes plan, term or licences: upgrades and more licences now, the rest at the period end (D-S4, D-S5).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - A new change replaces the scheduled one
// - The charge is published for Billing (Slice 4); nothing is invoiced here
// =========================================================
public sealed class ChangePlatformSubscriptionUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public ChangePlatformSubscriptionUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ChangePlatformSubscriptionResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ChangePlatformSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSubscriptionTarget target = await _administration.LoadAsync(context, PlatformPermissions.SubscriptionsManage, request.PlatformTenantId, requireStepUp: false, cancellationToken);

        if (target.Subscription is not PlatformSubscription subscription)
        {
            return ChangePlatformSubscriptionResponse.Failure(target.Error!);
        }

        string currentCode = target.Plan!.Code;
        PlatformSubscriptionEvent change;

        PlatformPlan? targetPlan = request.PlatformPlanId == target.Plan!.Id
            ? target.Plan
            : await _administration.Plans.GetAsync(request.PlatformPlanId, cancellationToken);

        if (targetPlan is null)
        {
            return ChangePlatformSubscriptionResponse.Failure(PlatformSubscriptionErrors.PlanNotAvailable);
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
            change = subscription.Apply(quote, currentCode, context.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return ChangePlatformSubscriptionResponse.Failure(exception.Error);
        }

        _administration.RecordChange(
            context,
            subscription,
            targetPlan.Id == subscription.PlatformPlanId ? targetPlan : target.Plan,
            change,
            PlatformPermissions.SubscriptionsManage,
            quote.Timing == PlatformSubscriptionChangeTiming.Now ? "platform.subscriptions.changed" : "platform.subscriptions.change_scheduled",
            quote.ChargeNow);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangePlatformSubscriptionResponse.Success(PlatformSubscriptionQuoteItem.From(quote))
            : ChangePlatformSubscriptionResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
