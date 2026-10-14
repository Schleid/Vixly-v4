using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Subscriptions.CancelPlatformScheduledChange;

// =========================================================
// FILE: CancelPlatformScheduledChangeUseCase.cs
//
// PURPOSE:
// Cancels the change waiting for the period end.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class CancelPlatformScheduledChangeUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public CancelPlatformScheduledChangeUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CancelPlatformScheduledChangeResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        CancelPlatformScheduledChangeRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSubscriptionTarget target = await _administration.LoadAsync(context, PlatformPermissions.SubscriptionsManage, request.PlatformTenantId, requireStepUp: false, cancellationToken);

        if (target.Subscription is not PlatformSubscription subscription)
        {
            return CancelPlatformScheduledChangeResponse.Failure(target.Error!);
        }

        PlatformSubscriptionEvent change;

        try
        {
            change = subscription.CancelScheduledChange(context.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return CancelPlatformScheduledChangeResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, subscription, target.Plan!, change, PlatformPermissions.SubscriptionsManage, "platform.subscriptions.scheduled_change_cancelled");

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? CancelPlatformScheduledChangeResponse.Success()
            : CancelPlatformScheduledChangeResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
