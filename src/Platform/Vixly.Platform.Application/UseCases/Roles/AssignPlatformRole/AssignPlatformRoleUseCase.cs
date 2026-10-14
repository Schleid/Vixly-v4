using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Users;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Roles.AssignPlatformRole;

// =========================================================
// FILE: AssignPlatformRoleUseCase.cs
//
// PURPOSE:
// Gives a staff user a role, for good or until a date.
//
// RESPONSIBILITIES:
// - Require platform.roles.assign and a recent second factor
// - Refuse an inactive role, your own account, a target with a
//   higher authority level, and a role granting a permission
//   the actor does not hold
// - Audit the grant
//
// IMPORTANT NOTES:
// - A time-boxed grant ends by itself at ExpiresUtc
// - Access is read fresh, so the grant applies on the next click
// =========================================================
public sealed class AssignPlatformRoleUseCase
{
    private readonly PlatformUserAdministration _users;

    private readonly PlatformRoleAdministration _administration;

    private readonly IPlatformRoleRepository _roles;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public AssignPlatformRoleUseCase(
        PlatformUserAdministration users,
        PlatformRoleAdministration administration,
        IPlatformRoleRepository roles,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<AssignPlatformRoleResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        AssignPlatformRoleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformUserTarget target = await _users.LoadTargetAsync(
            context,
            PlatformPermissions.RolesAssign,
            request.PlatformUserId,
            requireStepUp: true,
            cancellationToken);

        if (target.User is not PlatformUser user)
        {
            return AssignPlatformRoleResponse.Failure(target.Error!);
        }

        PlatformRole? role = await _roles.GetAsync(request.PlatformRoleId, cancellationToken);

        if (role is null)
        {
            return AssignPlatformRoleResponse.Failure(PlatformCommonErrors.NotFound);
        }

        if (!role.IsActive)
        {
            return AssignPlatformRoleResponse.Failure(PlatformRoleErrors.RoleInactive);
        }

        Error? escalation = await _administration.CheckWithinActorAccessAsync(
            context,
            role.Permissions.Select(permission => permission.PermissionCode),
            cancellationToken);

        if (escalation is not null)
        {
            return AssignPlatformRoleResponse.Failure(escalation);
        }

        try
        {
            user.AssignRole(role.Id, context.PlatformUserId, request.ExpiresUtc, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return AssignPlatformRoleResponse.Failure(exception.Error);
        }

        _audit.Record(
            context.PlatformUserId,
            "platform.roles.assigned",
            PlatformPermissions.RolesAssign,
            nameof(PlatformUser),
            user.Id,
            arguments: new { roleId = role.Id, role = role.Name, expiresUtc = request.ExpiresUtc });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return AssignPlatformRoleResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return AssignPlatformRoleResponse.Success();
    }
}
