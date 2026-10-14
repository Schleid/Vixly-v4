using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Tenants.ReactivatePlatformTenant;

// =========================================================
// FILE: ReactivatePlatformTenantUseCase.cs
//
// PURPOSE:
// Lifts a suspension or a deactivation.
//
// RESPONSIBILITIES:
// - Check platform.tenants.reactivate and step-up
// - Apply the move in the domain (D-T1)
// - Record the history event, audit entry and outbox message
//   in one commit (D-2, D-T17)
//
// IMPORTANT NOTES:
// - A tenant that was never active returns to Draft
// =========================================================
public sealed class ReactivatePlatformTenantUseCase
{
    private readonly PlatformTenantAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public ReactivatePlatformTenantUseCase(
        PlatformTenantAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ReactivatePlatformTenantResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ReactivatePlatformTenantRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformTenantTarget target = await _administration.LoadTenantAsync(
            context,
            PlatformPermissions.TenantsReactivate,
            request.PlatformTenantId,
            requireStepUp: true,
            cancellationToken);

        if (target.Tenant is not PlatformTenant tenant)
        {
            return ReactivatePlatformTenantResponse.Failure(target.Error!);
        }

        PlatformTenantLifecycleEvent change;

        try
        {
            change = tenant.Reactivate(request.Reason, context.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return ReactivatePlatformTenantResponse.Failure(exception.Error);
        }

        _administration.RecordMove(context, tenant, change, PlatformPermissions.TenantsReactivate, "platform.tenants.reactivated");

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ReactivatePlatformTenantResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return ReactivatePlatformTenantResponse.Success();
    }
}
