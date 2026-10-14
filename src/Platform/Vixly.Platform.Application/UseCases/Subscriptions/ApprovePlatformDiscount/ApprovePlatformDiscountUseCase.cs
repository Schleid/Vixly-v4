using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Subscriptions.ApprovePlatformDiscount;

// =========================================================
// FILE: ApprovePlatformDiscountUseCase.cs
//
// PURPOSE:
// Approves a discount waiting for the sales manager (D-S24).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs step-up
// - Nobody decides on their own request (four eyes)
// =========================================================
public sealed class ApprovePlatformDiscountUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public ApprovePlatformDiscountUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ApprovePlatformDiscountResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ApprovePlatformDiscountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSubscriptionTarget target = await _administration.LoadAsync(context, PlatformPermissions.DiscountsApprove, request.PlatformTenantId, requireStepUp: true, cancellationToken);

        if (target.Subscription is not PlatformSubscription subscription)
        {
            return ApprovePlatformDiscountResponse.Failure(target.Error!);
        }

        PlatformPlan plan = target.Plan!;
        DateTimeOffset utcNow = _administration.UtcNow;
        PlatformSubscriptionEvent change;

        try
        {
            change = subscription.ApproveDiscount(request.DiscountId, request.Reason, context.PlatformUserId, utcNow);
        }
        catch (DomainException exception)
        {
            return ApprovePlatformDiscountResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, subscription, plan, change, PlatformPermissions.DiscountsApprove, "platform.discounts.approved");

        if (plan.Versions.FirstOrDefault(item => item.Id == subscription.PlanVersionId) is { } current
            && subscription.ActivateIfNothingDue(current, utcNow) is { } activated)
        {
            _administration.RecordChange(context, subscription, plan, activated, null, "platform.subscriptions.activated_nothing_due");
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ApprovePlatformDiscountResponse.Success()
            : ApprovePlatformDiscountResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
