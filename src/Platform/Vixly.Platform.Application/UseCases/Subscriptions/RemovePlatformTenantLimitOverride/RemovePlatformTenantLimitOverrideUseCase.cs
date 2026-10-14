using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.RemovePlatformTenantLimitOverride;

// =========================================================
// FILE: RemovePlatformTenantLimitOverrideUseCase.cs
//
// PURPOSE:
// Removes a limit override; the plan value applies again.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs step-up; the removed override stays as history
// =========================================================
public sealed class RemovePlatformTenantLimitOverrideUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public RemovePlatformTenantLimitOverrideUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<RemovePlatformTenantLimitOverrideResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RemovePlatformTenantLimitOverrideRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _administration.AuthorizeAsync(context, PlatformPermissions.SubscriptionsOverrideLimits, requireStepUp: true, cancellationToken);

        if (refused is not null)
        {
            return RemovePlatformTenantLimitOverrideResponse.Failure(refused);
        }

        PlatformTenantLimitOverride? limitOverride = await _administration.Overrides.GetAsync(request.OverrideId, cancellationToken);

        if (limitOverride is null || limitOverride.PlatformTenantId != request.PlatformTenantId)
        {
            return RemovePlatformTenantLimitOverrideResponse.Failure(PlatformCommonErrors.NotFound);
        }

        try
        {
            limitOverride.Remove(context.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return RemovePlatformTenantLimitOverrideResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, limitOverride, PlatformPermissions.SubscriptionsOverrideLimits, "platform.subscriptions.override_removed");

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RemovePlatformTenantLimitOverrideResponse.Success()
            : RemovePlatformTenantLimitOverrideResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
