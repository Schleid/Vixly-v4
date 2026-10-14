using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Subscriptions.EndPlatformSubscriptionNow;

// =========================================================
// FILE: EndPlatformSubscriptionNowUseCase.cs
//
// PURPOSE:
// Ends a subscription at once.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs a reason and step-up; the tenant loses access at once
// =========================================================
public sealed class EndPlatformSubscriptionNowUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public EndPlatformSubscriptionNowUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<EndPlatformSubscriptionNowResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        EndPlatformSubscriptionNowRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSubscriptionTarget target = await _administration.LoadAsync(context, PlatformPermissions.SubscriptionsManage, request.PlatformTenantId, requireStepUp: true, cancellationToken);

        if (target.Subscription is not PlatformSubscription subscription)
        {
            return EndPlatformSubscriptionNowResponse.Failure(target.Error!);
        }

        PlatformSubscriptionEvent change;

        try
        {
            change = subscription.EndNow(request.Reason, context.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return EndPlatformSubscriptionNowResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, subscription, target.Plan!, change, PlatformPermissions.SubscriptionsManage, "platform.subscriptions.ended");

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? EndPlatformSubscriptionNowResponse.Success()
            : EndPlatformSubscriptionNowResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
