using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.GetPlatformPortalAccess;

// =========================================================
// FILE: GetPlatformPortalAccessUseCase.cs
//
// PURPOSE:
// Tells Edge and Operations which tenant a portal slug belongs to, its status and access level (D-L10).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Internal channel only (Slice 9); customers are never device-checked and their portal follows the tenant status only
// =========================================================
public sealed class GetPlatformPortalAccessUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public GetPlatformPortalAccessUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<GetPlatformPortalAccessResponse> ExecuteAsync(
        PlatformInternalAuthorizationContext context,
        GetPlatformPortalAccessRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        string slug = request.Slug?.Trim().ToLowerInvariant() ?? string.Empty;
        PlatformPortalSlug? row = slug.Length == 0 ? null : await _licensing.Slugs.FindAsync(slug, cancellationToken);
        // Only a tenant's own addresses resolve; a slug still held by a registration does not (D-T11).
        PlatformTenant? tenant = row is { State: PlatformPortalSlugState.Permanent, PlatformTenantId: Guid tenantId } ? await _licensing.Tenants.GetAsync(tenantId, cancellationToken) : null;

        // Operations serves the owner's setup pages while the tenant is still being set up (Stage 3, Slice 0):
        // a registration's tenant stays Draft until its first device activates it.
        if (tenant is { Status: PlatformTenantStatus.Draft or PlatformTenantStatus.PendingActivation } && request.IncludeSettingUp)
        {
            return GetPlatformPortalAccessResponse.Success(new PlatformPortalAccess(
                true,
                tenant.Id,
                tenant.Status,
                PlatformSubscriptionAccess.None,
                tenant.CurrentSlug,
                tenant.DefaultLanguage.Value,
                (await _licensing.LegalAsync(tenant.Id, cancellationToken)).Status));
        }

        // Unknown, never-active and closed tenants all look the same from outside.
        if (tenant is null || tenant.Status is PlatformTenantStatus.Draft or PlatformTenantStatus.PendingActivation or PlatformTenantStatus.Deactivated or PlatformTenantStatus.Banned)
        {
            return GetPlatformPortalAccessResponse.Success(new PlatformPortalAccess(false, null, null, PlatformSubscriptionAccess.None));
        }

        // An old address answers the current one, so Edge can redirect (slug corrections, Slice 2).
        return GetPlatformPortalAccessResponse.Success(new PlatformPortalAccess(
            true,
            tenant.Id,
            tenant.Status,
            await _licensing.AccessAsync(tenant.Id, cancellationToken),
            tenant.CurrentSlug,
            tenant.DefaultLanguage.Value,
            (await _licensing.LegalAsync(tenant.Id, cancellationToken)).Status));
    }
}
