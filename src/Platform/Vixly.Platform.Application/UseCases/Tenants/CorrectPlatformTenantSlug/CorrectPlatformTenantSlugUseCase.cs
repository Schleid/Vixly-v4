using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Tenants.CorrectPlatformTenantSlug;

// =========================================================
// FILE: CorrectPlatformTenantSlugUseCase.cs
//
// PURPOSE:
// Corrects the portal slug of a tenant that has never been
// active (D-T11).
//
// RESPONSIBILITIES:
// - Check platform.tenants.edit and step-up
// - Refuse once the tenant has been active
// - Check the new slug, move the permanent claim, record the
//   history event, audit entry and outbox message
//
// IMPORTANT NOTES:
// - The old slug was never public, so its claim is removed
// - A racing claim fails the save: reported as slug_taken
// =========================================================
public sealed class CorrectPlatformTenantSlugUseCase
{
    private readonly PlatformTenantAdministration _administration;

    private readonly PlatformPortalSlugs _slugs;

    private readonly IPlatformPortalSlugRepository _slugClaims;

    private readonly IPlatformTenantRepository _tenants;

    private readonly PlatformOutboxWriter _outbox;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public CorrectPlatformTenantSlugUseCase(
        PlatformTenantAdministration administration,
        PlatformPortalSlugs slugs,
        IPlatformPortalSlugRepository slugClaims,
        IPlatformTenantRepository tenants,
        PlatformOutboxWriter outbox,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _slugs = slugs ?? throw new ArgumentNullException(nameof(slugs));
        _slugClaims = slugClaims ?? throw new ArgumentNullException(nameof(slugClaims));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CorrectPlatformTenantSlugResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        CorrectPlatformTenantSlugRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformTenantTarget target = await _administration.LoadTenantAsync(
            context,
            PlatformPermissions.TenantsEdit,
            request.PlatformTenantId,
            requireStepUp: true,
            cancellationToken);

        if (target.Tenant is not PlatformTenant tenant)
        {
            return CorrectPlatformTenantSlugResponse.Failure(target.Error!);
        }

        if (!tenant.CanCorrectSlug)
        {
            return CorrectPlatformTenantSlugResponse.Failure(PlatformTenantErrors.SlugFixed);
        }

        DateTimeOffset utcNow = _administration.UtcNow;
        string previous = tenant.CurrentSlug;

        if (PlatformPortalSlugFormat.TryNormalize(request.Slug, out string? wanted) && wanted == previous)
        {
            return CorrectPlatformTenantSlugResponse.Failure(PlatformTenantErrors.SlugUnchanged);
        }

        PlatformSlugCheck slug = await _slugs.CheckAsync(request.Slug, utcNow, cancellationToken);

        if (slug.Error is not null)
        {
            return CorrectPlatformTenantSlugResponse.Failure(slug.Error);
        }

        PlatformTenantLifecycleEvent change;

        try
        {
            change = tenant.CorrectSlug(slug.Slug!, request.Reason, context.PlatformUserId, utcNow);

            if (await _slugClaims.FindPermanentForTenantAsync(tenant.Id, cancellationToken) is { } old)
            {
                _slugClaims.Remove(old);
            }

            if (slug.LapsedClaim is { } lapsed)
            {
                lapsed.ReclaimForTenant(tenant.Id, utcNow);
            }
            else
            {
                _slugClaims.Add(PlatformPortalSlug.ClaimForTenant(slug.Slug!, tenant.Id, utcNow));
            }
        }
        catch (DomainException exception)
        {
            return CorrectPlatformTenantSlugResponse.Failure(exception.Error);
        }

        _tenants.AddEvents(new[] { change });
        _administration.RecordChange(
            context,
            tenant,
            PlatformPermissions.TenantsEdit,
            "platform.tenants.slug_corrected",
            new { slug = previous },
            new { slug = tenant.CurrentSlug, reason = change.Reason });
        _outbox.Write(
            PlatformMessageTypes.TenantSlugCorrected,
            tenant.Id,
            new PlatformTenantSlugCorrected(tenant.Id, previous, tenant.CurrentSlug, utcNow));

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return CorrectPlatformTenantSlugResponse.Failure(PlatformTenantErrors.SlugTaken);
        }

        return CorrectPlatformTenantSlugResponse.Success();
    }
}
