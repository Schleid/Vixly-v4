using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Tenants.SuspendPlatformTenant;

// =========================================================
// FILE: SuspendPlatformTenantUseCase.cs
//
// PURPOSE:
// Suspends an active tenant with a category and a reason (D-T2).
//
// RESPONSIBILITIES:
// - Check platform.tenants.suspend and step-up
// - Apply the move in the domain (D-T1)
// - Record the history event, audit entry and outbox message
//   in one commit (D-2, D-T17)
//
// IMPORTANT NOTES:
// - The tenant's contacts get a notice with the category only
// - Slice 3 suspends the subscription from the outbox (D-T6)
// =========================================================
public sealed class SuspendPlatformTenantUseCase
{
    private readonly PlatformTenantAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public SuspendPlatformTenantUseCase(
        PlatformTenantAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SuspendPlatformTenantResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SuspendPlatformTenantRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformTenantTarget target = await _administration.LoadTenantAsync(
            context,
            PlatformPermissions.TenantsSuspend,
            request.PlatformTenantId,
            requireStepUp: true,
            cancellationToken);

        if (target.Tenant is not PlatformTenant tenant)
        {
            return SuspendPlatformTenantResponse.Failure(target.Error!);
        }

        PlatformTenantLifecycleEvent change;

        try
        {
            change = tenant.Suspend(request.Category, request.Reason, context.PlatformUserId, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return SuspendPlatformTenantResponse.Failure(exception.Error);
        }

        _administration.RecordMove(context, tenant, change, PlatformPermissions.TenantsSuspend, "platform.tenants.suspended");

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return SuspendPlatformTenantResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return SuspendPlatformTenantResponse.Success();
    }
}
