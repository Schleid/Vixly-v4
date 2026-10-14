using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Roles.ReactivatePlatformRole;

// =========================================================
// FILE: ReactivatePlatformRoleUseCase.cs
//
// PURPOSE:
// Reactivates a role: everyone still holding it gets its
// permissions back.
//
// RESPONSIBILITIES:
// - Require platform.roles.deactivate and a recent second factor
// - Refuse when the role grants a permission the actor does not
//   hold (no privilege escalation)
// - Audit the change
//
// IMPORTANT NOTES:
// - Step-up because reactivation hands out access to many people
//   at once (decision taken while building, slice 1)
// =========================================================
public sealed class ReactivatePlatformRoleUseCase
{
    private readonly PlatformRoleAdministration _administration;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ReactivatePlatformRoleUseCase(
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

    public async Task<ReactivatePlatformRoleResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ReactivatePlatformRoleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformRoleTarget target = await _administration.LoadRoleAsync(
            context,
            PlatformPermissions.RolesDeactivate,
            request.PlatformRoleId,
            requireStepUp: true,
            cancellationToken);

        if (target.Role is not PlatformRole role)
        {
            return ReactivatePlatformRoleResponse.Failure(target.Error!);
        }

        Error? escalation = await _administration.CheckWithinActorAccessAsync(
            context,
            role.Permissions.Select(permission => permission.PermissionCode),
            cancellationToken);

        if (escalation is not null)
        {
            return ReactivatePlatformRoleResponse.Failure(escalation);
        }

        try
        {
            role.Reactivate(_timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return ReactivatePlatformRoleResponse.Failure(exception.Error);
        }

        _audit.Record(context.PlatformUserId, "platform.roles.reactivated", PlatformPermissions.RolesDeactivate, nameof(PlatformRole), role.Id);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return ReactivatePlatformRoleResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return ReactivatePlatformRoleResponse.Success();
    }
}
