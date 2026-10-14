using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Application.UseCases.Roles.GetPlatformUserRoles;

// =========================================================
// FILE: GetPlatformUserRolesUseCase.cs
//
// PURPOSE:
// Loads the roles a staff user holds now and, for an
// administrator who may change them, the roles they can give.
//
// RESPONSIBILITIES:
// - Require platform.users.view
// - Answer NotFound for an unknown user
// - Offer changes only when the actor holds platform.roles.assign,
//   is not the target, and is not outranked by the target
// - Offer only active roles not held yet whose permissions the
//   actor holds
//
// IMPORTANT NOTES:
// - A convenience for the UI: Assign and Revoke check again
// =========================================================
public sealed class GetPlatformUserRolesUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformUserQueries _users;

    private readonly IPlatformRoleQueries _roles;

    private readonly PlatformRoleAdministration _administration;

    private readonly TimeProvider _timeProvider;

    public GetPlatformUserRolesUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformUserQueries users,
        IPlatformRoleQueries roles,
        PlatformRoleAdministration administration,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetPlatformUserRolesResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformUserRolesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.UsersView, cancellationToken);

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        PlatformUserActionFacts? facts = await _users.GetActionFactsAsync(request.PlatformUserId, utcNow, cancellationToken);

        if (facts is null)
        {
            return GetPlatformUserRolesResponse.Failure(PlatformCommonErrors.NotFound);
        }

        IReadOnlyList<PlatformUserRoleItem> grants = await _roles.ListUserGrantsAsync(request.PlatformUserId, utcNow, cancellationToken);
        PlatformPermissionSnapshot? actor = await _administration.ReadActorAsync(context, cancellationToken);
        bool canManage = PlatformAccess.Holds(actor, PlatformPermissions.RolesAssign)
            && context.PlatformUserId != request.PlatformUserId
            && actor!.AuthorityLevel >= facts.AuthorityLevel;

        if (!canManage)
        {
            return GetPlatformUserRolesResponse.Success(grants, Array.Empty<PlatformRoleListItem>(), false);
        }

        HashSet<Guid> held = grants.Select(grant => grant.PlatformRoleId).ToHashSet();
        List<PlatformRoleListItem> assignable = (await _roles.ListAsync(false, utcNow, cancellationToken))
            .Where(role => !held.Contains(role.PlatformRoleId) && PlatformAccess.Covers(actor, role.PermissionCodes))
            .ToList();

        return GetPlatformUserRolesResponse.Success(grants, assignable, true);
    }
}
