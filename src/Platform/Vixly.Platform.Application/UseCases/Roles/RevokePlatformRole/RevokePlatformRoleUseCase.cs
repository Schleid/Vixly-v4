using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Users;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Roles.RevokePlatformRole;

// =========================================================
// FILE: RevokePlatformRoleUseCase.cs
//
// PURPOSE:
// Ends a staff user's grant of a role now.
//
// RESPONSIBILITIES:
// - Require platform.roles.assign and a recent second factor
// - Refuse your own account and a target with a higher authority
//   level
// - Audit the revocation
//
// IMPORTANT NOTES:
// - The grant stays as history with who revoked it and when
// - Taking access away needs no escalation check
// =========================================================
public sealed class RevokePlatformRoleUseCase
{
    private readonly PlatformUserAdministration _users;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RevokePlatformRoleUseCase(
        PlatformUserAdministration users,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RevokePlatformRoleResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RevokePlatformRoleRequest request,
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
            return RevokePlatformRoleResponse.Failure(target.Error!);
        }

        try
        {
            user.RevokeRole(request.PlatformRoleId, context.PlatformUserId, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return RevokePlatformRoleResponse.Failure(exception.Error);
        }

        _audit.Record(
            context.PlatformUserId,
            "platform.roles.revoked",
            PlatformPermissions.RolesAssign,
            nameof(PlatformUser),
            user.Id,
            arguments: new { roleId = request.PlatformRoleId });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return RevokePlatformRoleResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return RevokePlatformRoleResponse.Success();
    }
}
