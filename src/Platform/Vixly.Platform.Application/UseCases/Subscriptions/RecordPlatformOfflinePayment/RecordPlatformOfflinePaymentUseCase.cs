using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Subscriptions.RecordPlatformOfflinePayment;

// =========================================================
// FILE: RecordPlatformOfflinePaymentUseCase.cs
//
// PURPOSE:
// Records a payment made outside Vixly (bank transfer, cheque) and activates the subscription.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs a reason (the payment reference) and step-up
// - Card payments arrive with Slice 5
// =========================================================
public sealed class RecordPlatformOfflinePaymentUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public RecordPlatformOfflinePaymentUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<RecordPlatformOfflinePaymentResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RecordPlatformOfflinePaymentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSubscriptionTarget target = await _administration.LoadAsync(context, PlatformPermissions.SubscriptionsManage, request.PlatformTenantId, requireStepUp: true, cancellationToken);

        if (target.Subscription is not PlatformSubscription subscription)
        {
            return RecordPlatformOfflinePaymentResponse.Failure(target.Error!);
        }

        PlatformSubscriptionEvent change;

        try
        {
            change = subscription.RecordOfflinePayment(request.Reason, context.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return RecordPlatformOfflinePaymentResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, subscription, target.Plan!, change, PlatformPermissions.SubscriptionsManage, "platform.subscriptions.offline_payment_recorded");

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RecordPlatformOfflinePaymentResponse.Success()
            : RecordPlatformOfflinePaymentResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
