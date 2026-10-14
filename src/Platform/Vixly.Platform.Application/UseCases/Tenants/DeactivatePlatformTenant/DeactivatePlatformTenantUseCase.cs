using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Tenants.DeactivatePlatformTenant;

// =========================================================
// FILE: DeactivatePlatformTenantUseCase.cs
//
// PURPOSE:
// Deactivates a tenant, for example when the business closes.
//
// RESPONSIBILITIES:
// - Check platform.tenants.deactivate and step-up
// - Apply the move in the domain (D-T1)
// - Record the history event, audit entry and outbox message
//   in one commit (D-2, D-T17)
//
// IMPORTANT NOTES:
// - Reversible; the slug stays the tenant's for good
// =========================================================
public sealed class DeactivatePlatformTenantUseCase
{
    private readonly PlatformTenantAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public DeactivatePlatformTenantUseCase(
        PlatformTenantAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<DeactivatePlatformTenantResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        DeactivatePlatformTenantRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformTenantTarget target = await _administration.LoadTenantAsync(
            context,
            PlatformPermissions.TenantsDeactivate,
            request.PlatformTenantId,
            requireStepUp: true,
            cancellationToken);

        if (target.Tenant is not PlatformTenant tenant)
        {
            return DeactivatePlatformTenantResponse.Failure(target.Error!);
        }

        PlatformTenantLifecycleEvent change;

        try
        {
            change = tenant.Deactivate(request.Reason, context.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return DeactivatePlatformTenantResponse.Failure(exception.Error);
        }

        _administration.RecordMove(context, tenant, change, PlatformPermissions.TenantsDeactivate, "platform.tenants.deactivated");

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return DeactivatePlatformTenantResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return DeactivatePlatformTenantResponse.Success();
    }
}
