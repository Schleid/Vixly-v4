using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.GetPlatformTenantEntitlements;

// =========================================================
// FILE: GetPlatformTenantEntitlementsUseCase.cs
//
// PURPOSE:
// Reads what a tenant may use: access, features and effective limits.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The same answer Operations will read through Contracts.Platform (Slice 9)
// =========================================================
public sealed class GetPlatformTenantEntitlementsUseCase
{
    private readonly PlatformSubscriptionAdministration _administration;

    public GetPlatformTenantEntitlementsUseCase(
        PlatformSubscriptionAdministration administration)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
    }

    public async Task<GetPlatformTenantEntitlementsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformTenantEntitlementsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        Error? refused = await _administration.AuthorizeAsync(context, PlatformPermissions.SubscriptionsView, requireStepUp: false, cancellationToken);

        if (refused is not null)
        {
            return GetPlatformTenantEntitlementsResponse.Failure(refused);
        }

        PlatformSubscription? subscription = await _administration.Subscriptions.GetCurrentAsync(request.PlatformTenantId, cancellationToken);
        PlatformPlan? plan = subscription is null ? null : await _administration.Plans.GetAsync(subscription.PlatformPlanId, cancellationToken);
        PlatformPlanVersion? version = plan?.Versions.FirstOrDefault(item => item.Id == subscription!.PlanVersionId);
        IReadOnlyList<PlatformLimitItem> limits = await _administration.LimitsAsync(request.PlatformTenantId, version, null, cancellationToken);

        return GetPlatformTenantEntitlementsResponse.Success(new PlatformTenantEntitlements(
            request.PlatformTenantId,
            subscription?.Status,
            subscription?.AccessAt(_administration.UtcNow, _administration.UnpaidReadOnlyPeriod) ?? PlatformSubscriptionAccess.None,
            version?.Features.Select(item => item.Feature).OrderBy(feature => feature).ToArray() ?? Array.Empty<PlatformPlanFeature>(),
            limits.ToDictionary(item => item.Kind, item => item.Effective)));
    }
}
