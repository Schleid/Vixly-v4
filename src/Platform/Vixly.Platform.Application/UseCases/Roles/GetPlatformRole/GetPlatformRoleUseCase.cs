using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Application.UseCases.Roles.GetPlatformRole;

// =========================================================
// FILE: GetPlatformRoleUseCase.cs
//
// PURPOSE:
// Loads one role with its permissions and holders, and what
// the signed-in user may do to it.
//
// RESPONSIBILITIES:
// - Require platform.roles.view
// - Answer NotFound for an unknown role
// - Work out the allowed actions from fresh access
//
// IMPORTANT NOTES:
// - System roles are never renamed, deactivated or edited here
// - The permission catalogue comes from PlatformPermissions
// =========================================================
public sealed class GetPlatformRoleUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformRoleQueries _queries;

    private readonly PlatformRoleAdministration _administration;

    private readonly TimeProvider _timeProvider;

    public GetPlatformRoleUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformRoleQueries queries,
        PlatformRoleAdministration administration,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetPlatformRoleResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformRoleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.RolesView, cancellationToken);

        PlatformRoleDetail? role = await _queries.GetAsync(request.PlatformRoleId, _timeProvider.GetUtcNow(), cancellationToken);

        if (role is null)
        {
            return GetPlatformRoleResponse.Failure(PlatformCommonErrors.NotFound);
        }

        PlatformPermissionSnapshot? actor = await _administration.ReadActorAsync(context, cancellationToken);
        bool custom = role.SystemKey is null;

        return GetPlatformRoleResponse.Success(
            role,
            new PlatformRoleActions(
                CanRename: custom && PlatformAccess.Holds(actor, PlatformPermissions.RolesEdit),
                CanDeactivate: custom && role.IsActive && PlatformAccess.Holds(actor, PlatformPermissions.RolesDeactivate),
                CanReactivate: !role.IsActive && PlatformAccess.Holds(actor, PlatformPermissions.RolesDeactivate),
                CanEditPermissions: custom && PlatformAccess.Holds(actor, PlatformPermissions.RolesEditPermissions)));
    }
}
