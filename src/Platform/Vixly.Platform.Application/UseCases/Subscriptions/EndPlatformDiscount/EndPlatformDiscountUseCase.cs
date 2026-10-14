using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Subscriptions.EndPlatformDiscount;

// =========================================================
// FILE: EndPlatformDiscountUseCase.cs
//
// PURPOSE:
// Ends the tenant's current discount now, with a reason.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The next period is billed at the full price
// =========================================================
public sealed class EndPlatformDiscountUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public EndPlatformDiscountUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<EndPlatformDiscountResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        EndPlatformDiscountRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSubscriptionTarget target = await _administration.LoadAsync(context, PlatformPermissions.DiscountsGrant, request.PlatformTenantId, requireStepUp: false, cancellationToken);

        if (target.Subscription is not PlatformSubscription subscription)
        {
            return EndPlatformDiscountResponse.Failure(target.Error!);
        }

        PlatformPlan plan = target.Plan!;
        DateTimeOffset utcNow = _administration.UtcNow;
        PlatformSubscriptionEvent change;

        try
        {
            change = subscription.EndDiscount(request.Reason, context.PlatformUserId, utcNow);
        }
        catch (DomainException exception)
        {
            return EndPlatformDiscountResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, subscription, plan, change, PlatformPermissions.DiscountsGrant, "platform.discounts.ended");

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? EndPlatformDiscountResponse.Success()
            : EndPlatformDiscountResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
