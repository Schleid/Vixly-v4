using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Tenants.ActivatePlatformTenant;

// =========================================================
// FILE: ActivatePlatformTenantUseCase.cs
//
// PURPOSE:
// Activates a Draft or Pending activation tenant.
//
// RESPONSIBILITIES:
// - Check platform.tenants.activate
// - Apply the move in the domain (D-T1)
// - Record the history event, audit entry and outbox message
//   in one commit (D-2, D-T17)
//
// IMPORTANT NOTES:
// - The first activation fixes the slug for good (D-T11)
// =========================================================
public sealed class ActivatePlatformTenantUseCase
{
    private readonly PlatformTenantAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public ActivatePlatformTenantUseCase(
        PlatformTenantAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ActivatePlatformTenantResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ActivatePlatformTenantRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformTenantTarget target = await _administration.LoadTenantAsync(
            context,
            PlatformPermissions.TenantsActivate,
            request.PlatformTenantId,
            requireStepUp: false,
            cancellationToken);

        if (target.Tenant is not PlatformTenant tenant)
        {
            return ActivatePlatformTenantResponse.Failure(target.Error!);
        }

        PlatformTenantLifecycleEvent change;

        try
        {
            change = tenant.Activate(context.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return ActivatePlatformTenantResponse.Failure(exception.Error);
        }

        _administration.RecordMove(context, tenant, change, PlatformPermissions.TenantsActivate, "platform.tenants.activated");

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ActivatePlatformTenantResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return ActivatePlatformTenantResponse.Success();
    }
}
