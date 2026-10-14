using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Subscriptions.ApplyPlatformPromotion;

// =========================================================
// FILE: ApplyPlatformPromotionUseCase.cs
//
// PURPOSE:
// Applies an approved promotion code to a tenant's subscription (D-S20).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - A promotion was approved by the CFO, so it needs no further approval
// - Only for a subscription without a discount; one use per tenant
// =========================================================
public sealed class ApplyPlatformPromotionUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public ApplyPlatformPromotionUseCase(
        PlatformSubscriptionAdministration administration,
        PlatformSalesAdministration sales,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ApplyPlatformPromotionResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ApplyPlatformPromotionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSubscriptionTarget target = await _administration.LoadAsync(context, PlatformPermissions.DiscountsGrant, request.PlatformTenantId, requireStepUp: false, cancellationToken);

        if (target.Subscription is not PlatformSubscription subscription)
        {
            return ApplyPlatformPromotionResponse.Failure(target.Error!);
        }

        PlatformPlan plan = target.Plan!;
        DateTimeOffset utcNow = _administration.UtcNow;

        PlatformPromotion? promotion = string.IsNullOrWhiteSpace(request.Code)
            ? null
            : await _sales.Promotions.GetByCodeAsync(request.Code.Trim().ToUpperInvariant(), cancellationToken);

        if (promotion is null)
        {
            return ApplyPlatformPromotionResponse.Failure(PlatformSalesErrors.PromotionNotFound);
        }

        if (subscription.ActiveDiscount is not null || subscription.PendingDiscount is not null)
        {
            return ApplyPlatformPromotionResponse.Failure(PlatformDiscountErrors.AlreadyDiscounted);
        }

        if (await _administration.Subscriptions.HasUsedPromotionAsync(subscription.PlatformTenantId, promotion.Id, cancellationToken))
        {
            return ApplyPlatformPromotionResponse.Failure(PlatformSalesErrors.PromotionAlreadyUsed);
        }

        PlatformSubscriptionEvent change;

        try
        {
            promotion.Redeem(subscription.PlatformPlanId, subscription.Term, atRegistration: false, utcNow);
            change = subscription.RequestDiscount(promotion.Terms, PlatformDiscountSource.Promotion, promotion.Id, promotion.Code, false, context.PlatformUserId, utcNow);
        }
        catch (DomainException exception)
        {
            return ApplyPlatformPromotionResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, subscription, plan, change, PlatformPermissions.DiscountsGrant, "platform.discounts.applied");
        _sales.Audit(context, "platform.promotions.redeemed", PlatformPermissions.DiscountsGrant, nameof(PlatformPromotion), promotion.Id, after: new { tenant = subscription.PlatformTenantId });

        if (plan.Versions.FirstOrDefault(item => item.Id == subscription.PlanVersionId) is { } current
            && subscription.ActivateIfNothingDue(current, utcNow) is { } activated)
        {
            _administration.RecordChange(context, subscription, plan, activated, null, "platform.subscriptions.activated_nothing_due");
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ApplyPlatformPromotionResponse.Success()
            : ApplyPlatformPromotionResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
