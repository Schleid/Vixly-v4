using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Subscriptions.CancelPlatformOneTimeCharge;

// =========================================================
// FILE: CancelPlatformOneTimeChargeUseCase.cs
//
// PURPOSE:
// Cancels a one-time charge that is not billed yet (D-S23).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class CancelPlatformOneTimeChargeUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public CancelPlatformOneTimeChargeUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CancelPlatformOneTimeChargeResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        CancelPlatformOneTimeChargeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSubscriptionTarget target = await _administration.LoadAsync(context, PlatformPermissions.SubscriptionsCharges, request.PlatformTenantId, requireStepUp: false, cancellationToken);

        if (target.Subscription is not PlatformSubscription subscription)
        {
            return CancelPlatformOneTimeChargeResponse.Failure(target.Error!);
        }

        PlatformPlan plan = target.Plan!;
        DateTimeOffset utcNow = _administration.UtcNow;
        PlatformSubscriptionEvent change;

        try
        {
            change = subscription.CancelCharge(request.ChargeId, request.Reason, context.PlatformUserId, utcNow);
        }
        catch (DomainException exception)
        {
            return CancelPlatformOneTimeChargeResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, subscription, plan, change, PlatformPermissions.SubscriptionsCharges, "platform.subscriptions.charge_cancelled");

        if (plan.Versions.FirstOrDefault(item => item.Id == subscription.PlanVersionId) is { } current
            && subscription.ActivateIfNothingDue(current, utcNow) is { } activated)
        {
            _administration.RecordChange(context, subscription, plan, activated, null, "platform.subscriptions.activated_nothing_due");
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? CancelPlatformOneTimeChargeResponse.Success()
            : CancelPlatformOneTimeChargeResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
