using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Subscriptions.GrantPlatformDiscount;

// =========================================================
// FILE: GrantPlatformDiscountUseCase.cs
//
// PURPOSE:
// Grants a discount to one tenant, or asks the sales manager when it is beyond the limits (D-S18, D-S22, D-S24).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Nothing is hard-coded: the Sales settings decide what needs approval; empty settings send everything to approval
// - One discount at a time: an applied discount replaces the current one, with history
// =========================================================
public sealed class GrantPlatformDiscountUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public GrantPlatformDiscountUseCase(
        PlatformSubscriptionAdministration administration,
        PlatformSalesAdministration sales,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<GrantPlatformDiscountResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GrantPlatformDiscountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSubscriptionTarget target = await _administration.LoadAsync(context, PlatformPermissions.DiscountsGrant, request.PlatformTenantId, requireStepUp: false, cancellationToken);

        if (target.Subscription is not PlatformSubscription subscription)
        {
            return GrantPlatformDiscountResponse.Failure(target.Error!);
        }

        PlatformPlan plan = target.Plan!;
        DateTimeOffset utcNow = _administration.UtcNow;

        if (request.Terms is null)
        {
            return GrantPlatformDiscountResponse.Failure(PlatformCommonErrors.RequestInvalid);
        }

        PlatformSalesSettings settings = await _sales.SettingsAsync(cancellationToken);
        PlatformSubscriptionEvent change;
        bool needsApproval;

        try
        {
            PlatformDiscountTerms terms = request.Terms.ToTerms(utcNow);
            needsApproval = settings.DiscountNeedsApproval(terms, plan.CurrencyCode);
            change = subscription.RequestDiscount(terms, PlatformDiscountSource.Staff, null, request.Reason, needsApproval, context.PlatformUserId, utcNow);
        }
        catch (DomainException exception)
        {
            return GrantPlatformDiscountResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, subscription, plan, change, PlatformPermissions.DiscountsGrant, needsApproval ? "platform.discounts.requested" : "platform.discounts.applied");

        if (needsApproval)
        {
            _sales.RequestApproval(PlatformApprovalKind.Discount, subscription.PendingDiscount!.Id, PlatformPermissions.DiscountsApprove, plan.Code, context.PlatformUserId);
        }

        if (plan.Versions.FirstOrDefault(item => item.Id == subscription.PlanVersionId) is { } current
            && subscription.ActivateIfNothingDue(current, utcNow) is { } activated)
        {
            _administration.RecordChange(context, subscription, plan, activated, null, "platform.subscriptions.activated_nothing_due");
        }

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? GrantPlatformDiscountResponse.Success(needsApproval)
            : GrantPlatformDiscountResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
