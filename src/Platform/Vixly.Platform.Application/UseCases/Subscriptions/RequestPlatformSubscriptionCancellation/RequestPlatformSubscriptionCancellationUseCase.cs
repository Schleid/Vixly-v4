using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Subscriptions.RequestPlatformSubscriptionCancellation;

// =========================================================
// FILE: RequestPlatformSubscriptionCancellationUseCase.cs
//
// PURPOSE:
// Cancels at the period end, or at once for a trial or an unpaid subscription.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - A paid subscription stays Active until its period ends; it can be resumed before then
// =========================================================
public sealed class RequestPlatformSubscriptionCancellationUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public RequestPlatformSubscriptionCancellationUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<RequestPlatformSubscriptionCancellationResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RequestPlatformSubscriptionCancellationRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSubscriptionTarget target = await _administration.LoadAsync(context, PlatformPermissions.SubscriptionsManage, request.PlatformTenantId, requireStepUp: false, cancellationToken);

        if (target.Subscription is not PlatformSubscription subscription)
        {
            return RequestPlatformSubscriptionCancellationResponse.Failure(target.Error!);
        }

        PlatformSubscriptionEvent change;

        try
        {
            change = subscription.RequestCancellation(request.Reason, context.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return RequestPlatformSubscriptionCancellationResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, subscription, target.Plan!, change, PlatformPermissions.SubscriptionsManage, "platform.subscriptions.cancellation_requested");

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RequestPlatformSubscriptionCancellationResponse.Success()
            : RequestPlatformSubscriptionCancellationResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
