using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Subscriptions.ResumePlatformSubscription;

// =========================================================
// FILE: ResumePlatformSubscriptionUseCase.cs
//
// PURPOSE:
// Withdraws a requested cancellation before the period ends.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ResumePlatformSubscriptionUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public ResumePlatformSubscriptionUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ResumePlatformSubscriptionResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ResumePlatformSubscriptionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformSubscriptionTarget target = await _administration.LoadAsync(context, PlatformPermissions.SubscriptionsManage, request.PlatformTenantId, requireStepUp: false, cancellationToken);

        if (target.Subscription is not PlatformSubscription subscription)
        {
            return ResumePlatformSubscriptionResponse.Failure(target.Error!);
        }

        PlatformSubscriptionEvent change;

        try
        {
            change = subscription.Resume(context.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return ResumePlatformSubscriptionResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, subscription, target.Plan!, change, PlatformPermissions.SubscriptionsManage, "platform.subscriptions.resumed");

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ResumePlatformSubscriptionResponse.Success()
            : ResumePlatformSubscriptionResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
