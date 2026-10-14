using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Tenants.SetPlatformTenantTags;

// =========================================================
// FILE: SetPlatformTenantTagsUseCase.cs
//
// PURPOSE:
// Replaces a tenant's internal tags (D-T16).
//
// RESPONSIBILITIES:
// - Check platform.tenants.edit
// - Normalise, de-duplicate and limit the tags
// - Audit before and after
//
// IMPORTANT NOTES:
// - Tags are internal and never shown to the tenant
// =========================================================
public sealed class SetPlatformTenantTagsUseCase
{
    private readonly PlatformTenantAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public SetPlatformTenantTagsUseCase(
        PlatformTenantAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SetPlatformTenantTagsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SetPlatformTenantTagsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformTenantTarget target = await _administration.LoadTenantAsync(
            context,
            PlatformPermissions.TenantsEdit,
            request.PlatformTenantId,
            requireStepUp: false,
            cancellationToken);

        if (target.Tenant is not PlatformTenant tenant)
        {
            return SetPlatformTenantTagsResponse.Failure(target.Error!);
        }

        object before = tenant.Tags.Select(tag => tag.Value).ToArray();

        try
        {
            tenant.SetTags(request.Tags, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return SetPlatformTenantTagsResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, tenant, PlatformPermissions.TenantsEdit, "platform.tenants.tags_changed", before, tenant.Tags.Select(tag => tag.Value).ToArray());

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return SetPlatformTenantTagsResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return SetPlatformTenantTagsResponse.Success();
    }
}
