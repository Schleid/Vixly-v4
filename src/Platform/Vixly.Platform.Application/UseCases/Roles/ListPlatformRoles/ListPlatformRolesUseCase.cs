using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Application.UseCases.Roles.ListPlatformRoles;

// =========================================================
// FILE: ListPlatformRolesUseCase.cs
//
// PURPOSE:
// Lists the Platform roles for the roles screen.
//
// RESPONSIBILITIES:
// - Require platform.roles.view
// - Return active roles, and inactive ones when asked
// - Say whether the signed-in user may create a role
//
// IMPORTANT NOTES:
// - Small, bounded list (tens of roles): no paging
// =========================================================
public sealed class ListPlatformRolesUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformRoleQueries _queries;

    private readonly PlatformRoleAdministration _administration;

    private readonly TimeProvider _timeProvider;

    public ListPlatformRolesUseCase(
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

    public async Task<ListPlatformRolesResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformRolesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.RolesView, cancellationToken);

        IReadOnlyList<PlatformRoleListItem> roles = await _queries.ListAsync(
            request.IncludeInactive,
            _timeProvider.GetUtcNow(),
            cancellationToken);
        PlatformPermissionSnapshot? actor = await _administration.ReadActorAsync(context, cancellationToken);

        return ListPlatformRolesResponse.Success(roles, PlatformAccess.Holds(actor, PlatformPermissions.RolesCreate));
    }
}
