using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Subscriptions.EndPlatformTrial;

// =========================================================
// FILE: EndPlatformTrialUseCase.cs
//
// PURPOSE:
// Ends a trial now: the subscription then awaits payment (owner, D-S12).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class EndPlatformTrialUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public EndPlatformTrialUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<EndPlatformTrialResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        EndPlatformTrialRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSubscriptionTarget target = await _administration.LoadAsync(context, PlatformPermissions.SubscriptionsManage, request.PlatformTenantId, requireStepUp: false, cancellationToken);

        if (target.Subscription is not PlatformSubscription subscription)
        {
            return EndPlatformTrialResponse.Failure(target.Error!);
        }

        PlatformSubscriptionEvent change;

        try
        {
            change = subscription.EndTrial(request.Reason, context.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return EndPlatformTrialResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, subscription, target.Plan!, change, PlatformPermissions.SubscriptionsManage, "platform.subscriptions.trial_ended");

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? EndPlatformTrialResponse.Success()
            : EndPlatformTrialResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
