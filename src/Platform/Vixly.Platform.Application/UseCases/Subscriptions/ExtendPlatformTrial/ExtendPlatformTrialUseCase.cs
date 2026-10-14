using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Subscriptions.ExtendPlatformTrial;

// =========================================================
// FILE: ExtendPlatformTrialUseCase.cs
//
// PURPOSE:
// Moves a trial's end date (owner, D-S12).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Staff flexibility asked by the owner: a trial can run longer than the plan offers
// =========================================================
public sealed class ExtendPlatformTrialUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public ExtendPlatformTrialUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ExtendPlatformTrialResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ExtendPlatformTrialRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSubscriptionTarget target = await _administration.LoadAsync(context, PlatformPermissions.SubscriptionsManage, request.PlatformTenantId, requireStepUp: false, cancellationToken);

        if (target.Subscription is not PlatformSubscription subscription)
        {
            return ExtendPlatformTrialResponse.Failure(target.Error!);
        }

        PlatformSubscriptionEvent change;

        try
        {
            change = subscription.ExtendTrial(request.TrialEndsUtc, request.Reason, context.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return ExtendPlatformTrialResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, subscription, target.Plan!, change, PlatformPermissions.SubscriptionsManage, "platform.subscriptions.trial_extended");

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ExtendPlatformTrialResponse.Success()
            : ExtendPlatformTrialResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
