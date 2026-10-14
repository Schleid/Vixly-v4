using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Roles.UpdateRolePermissions;

// =========================================================
// FILE: UpdateRolePermissionsUseCase.cs
//
// PURPOSE:
// Replaces the permissions of a custom role.
//
// RESPONSIBILITIES:
// - Require platform.roles.edit_permissions and a recent second
//   factor
// - Accept only codes in the permission catalogue that a role
//   may carry
// - Refuse to add a permission the actor does not hold
// - Audit the permissions before and after
//
// IMPORTANT NOTES:
// - Removing a permission needs no escalation check
// - platform.users.change_authority is not in the catalogue: only
//   the SuperUser authority level carries it
// =========================================================
public sealed class UpdateRolePermissionsUseCase
{
    private readonly PlatformRoleAdministration _administration;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public UpdateRolePermissionsUseCase(
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

    public async Task<UpdateRolePermissionsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdateRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (request.PermissionCodes is null)
        {
            return UpdateRolePermissionsResponse.Failure(PlatformCommonErrors.RequestInvalid);
        }

        if (!request.PermissionCodes.All(PlatformPermissions.IsGrantable))
        {
            return UpdateRolePermissionsResponse.Failure(PlatformCommonErrors.PermissionUnknown);
        }

        PlatformRoleTarget target = await _administration.LoadRoleAsync(
            context,
            PlatformPermissions.RolesEditPermissions,
            request.PlatformRoleId,
            requireStepUp: true,
            cancellationToken);

        if (target.Role is not PlatformRole role)
        {
            return UpdateRolePermissionsResponse.Failure(target.Error!);
        }

        List<string> before = Sorted(role.Permissions.Select(permission => permission.PermissionCode));
        List<string> after = Sorted(request.PermissionCodes);
        Error? escalation = await _administration.CheckWithinActorAccessAsync(context, after.Except(before), cancellationToken);

        if (escalation is not null)
        {
            return UpdateRolePermissionsResponse.Failure(escalation);
        }

        if (before.SequenceEqual(after, StringComparer.Ordinal))
        {
            return UpdateRolePermissionsResponse.Success();
        }

        try
        {
            role.ReplacePermissions(after, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return UpdateRolePermissionsResponse.Failure(exception.Error);
        }

        _audit.Record(
            context.PlatformUserId,
            "platform.roles.permissions_changed",
            PlatformPermissions.RolesEditPermissions,
            nameof(PlatformRole),
            role.Id,
            before: before,
            after: after);

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return UpdateRolePermissionsResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return UpdateRolePermissionsResponse.Success();
    }

    private static List<string> Sorted(IEnumerable<string> codes)
    {
        return codes.Distinct(StringComparer.Ordinal).OrderBy(code => code, StringComparer.Ordinal).ToList();
    }
}
