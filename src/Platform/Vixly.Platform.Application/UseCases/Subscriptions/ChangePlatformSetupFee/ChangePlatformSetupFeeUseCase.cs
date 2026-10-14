using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Subscriptions.ChangePlatformSetupFee;

// =========================================================
// FILE: ChangePlatformSetupFeeUseCase.cs
//
// PURPOSE:
// Changes or waives the setup fee for one tenant, before it is charged (D-S23).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Amount 0 waives it; an empty amount returns to the plan's fee
// =========================================================
public sealed class ChangePlatformSetupFeeUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public ChangePlatformSetupFeeUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ChangePlatformSetupFeeResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ChangePlatformSetupFeeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSubscriptionTarget target = await _administration.LoadAsync(context, PlatformPermissions.SubscriptionsCharges, request.PlatformTenantId, requireStepUp: false, cancellationToken);

        if (target.Subscription is not PlatformSubscription subscription)
        {
            return ChangePlatformSetupFeeResponse.Failure(target.Error!);
        }

        PlatformPlan plan = target.Plan!;
        DateTimeOffset utcNow = _administration.UtcNow;
        PlatformPlanVersion? version = plan.Versions.FirstOrDefault(item => item.Id == subscription.PlanVersionId);

        if (version is null)
        {
            return ChangePlatformSetupFeeResponse.Failure(PlatformCommonErrors.NotFound);
        }

        PlatformSubscriptionEvent change;

        try
        {
            change = subscription.ChangeSetupFee(request.Amount, version, request.Reason, context.PlatformUserId, utcNow);
        }
        catch (DomainException exception)
        {
            return ChangePlatformSetupFeeResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, subscription, plan, change, PlatformPermissions.SubscriptionsCharges, "platform.subscriptions.setup_fee_changed");

        if (plan.Versions.FirstOrDefault(item => item.Id == subscription.PlanVersionId) is { } current
            && subscription.ActivateIfNothingDue(current, utcNow) is { } activated)
        {
            _administration.RecordChange(context, subscription, plan, activated, null, "platform.subscriptions.activated_nothing_due");
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangePlatformSetupFeeResponse.Success()
            : ChangePlatformSetupFeeResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
