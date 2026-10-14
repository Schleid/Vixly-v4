using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.SetPlatformTenantLimitOverride;

// =========================================================
// FILE: SetPlatformTenantLimitOverrideUseCase.cs
//
// PURPOSE:
// Overrides one of a tenant's plan limits (D-S8).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs step-up
// - A new override closes the open one for the same limit
// =========================================================
public sealed class SetPlatformTenantLimitOverrideUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public SetPlatformTenantLimitOverrideUseCase(
        PlatformSubscriptionAdministration administration,
        IPlatformTenantRepository tenants,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SetPlatformTenantLimitOverrideResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SetPlatformTenantLimitOverrideRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _administration.AuthorizeAsync(context, PlatformPermissions.SubscriptionsOverrideLimits, requireStepUp: true, cancellationToken);

        if (refused is not null)
        {
            return SetPlatformTenantLimitOverrideResponse.Failure(refused);
        }

        if (await _tenants.GetAsync(request.PlatformTenantId, cancellationToken) is null)
        {
            return SetPlatformTenantLimitOverrideResponse.Failure(PlatformCommonErrors.NotFound);
        }

        DateTimeOffset utcNow = _administration.UtcNow;
        PlatformTenantLimitOverride created;

        try
        {
            created = PlatformTenantLimitOverride.Set(request.PlatformTenantId, request.Kind, request.Value, request.Reason, request.EndsUtc, context.PlatformUserId, utcNow);

            foreach (PlatformTenantLimitOverride open in (await _administration.Overrides.ListOpenAsync(request.PlatformTenantId, cancellationToken)).Where(item => item.Kind == request.Kind))
            {
                open.Remove(context.PlatformUserId, utcNow);
                _administration.RecordChange(context, open, PlatformPermissions.SubscriptionsOverrideLimits, "platform.subscriptions.override_removed");
            }
        }
        catch (DomainException exception)
        {
            return SetPlatformTenantLimitOverrideResponse.Failure(exception.Error);
        }

        _administration.Overrides.Add(created);
        _administration.RecordChange(context, created, PlatformPermissions.SubscriptionsOverrideLimits, "platform.subscriptions.override_set");

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SetPlatformTenantLimitOverrideResponse.Success(created.Id)
            : SetPlatformTenantLimitOverrideResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
