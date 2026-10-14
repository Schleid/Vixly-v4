using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Roles.DeactivatePlatformRole;

// =========================================================
// FILE: DeactivatePlatformRoleUseCase.cs
//
// PURPOSE:
// Deactivates a custom role: everyone holding it loses its
// permissions at once.
//
// RESPONSIBILITIES:
// - Require platform.roles.deactivate
// - Refuse a system role
// - Audit the change
//
// IMPORTANT NOTES:
// - Grants are kept, so reactivating restores them
// - Access is read fresh on every request, so holders lose the
//   permissions on their next click
// =========================================================
public sealed class DeactivatePlatformRoleUseCase
{
    private readonly PlatformRoleAdministration _administration;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public DeactivatePlatformRoleUseCase(
        PlatformRoleAdministration administration,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<DeactivatePlatformRoleResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        DeactivatePlatformRoleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformRoleTarget target = await _administration.LoadRoleAsync(
            context,
            PlatformPermissions.RolesDeactivate,
            request.PlatformRoleId,
            requireStepUp: false,
            cancellationToken);

        if (target.Role is not PlatformRole role)
        {
            return DeactivatePlatformRoleResponse.Failure(target.Error!);
        }

        try
        {
            role.Deactivate(_timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return DeactivatePlatformRoleResponse.Failure(exception.Error);
        }

        _audit.Record(context.PlatformUserId, "platform.roles.deactivated", PlatformPermissions.RolesDeactivate, nameof(PlatformRole), role.Id);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return DeactivatePlatformRoleResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return DeactivatePlatformRoleResponse.Success();
    }
}
