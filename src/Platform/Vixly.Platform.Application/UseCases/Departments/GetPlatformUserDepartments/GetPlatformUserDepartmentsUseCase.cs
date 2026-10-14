using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Application.UseCases.Departments.GetPlatformUserDepartments;

// =========================================================
// FILE: GetPlatformUserDepartmentsUseCase.cs
//
// PURPOSE:
// Loads the departments a staff user belongs to and, for an
// administrator who may change them, the departments they can
// add the user to.
//
// RESPONSIBILITIES:
// - Require platform.users.view
// - Answer NotFound for an unknown user
// - Offer changes only when the actor holds
//   platform.departments.manage_members and is not outranked by
//   the target
//
// IMPORTANT NOTES:
// - A convenience for the UI: the member use cases check again
// =========================================================
public sealed class GetPlatformUserDepartmentsUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformUserQueries _users;

    private readonly IPlatformDepartmentQueries _departments;

    private readonly PlatformDepartmentAdministration _administration;

    private readonly TimeProvider _timeProvider;

    public GetPlatformUserDepartmentsUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformUserQueries users,
        IPlatformDepartmentQueries departments,
        PlatformDepartmentAdministration administration,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _departments = departments ?? throw new ArgumentNullException(nameof(departments));
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetPlatformUserDepartmentsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformUserDepartmentsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.UsersView, cancellationToken);

        PlatformUserActionFacts? facts = await _users.GetActionFactsAsync(request.PlatformUserId, _timeProvider.GetUtcNow(), cancellationToken);

        if (facts is null)
        {
            return GetPlatformUserDepartmentsResponse.Failure(PlatformCommonErrors.NotFound);
        }

        IReadOnlyList<PlatformUserDepartmentItem> memberships = await _departments.ListUserMembershipsAsync(request.PlatformUserId, cancellationToken);
        PlatformPermissionSnapshot? actor = await _administration.ReadActorAsync(context, cancellationToken);
        bool canManage = PlatformAccess.Holds(actor, PlatformPermissions.DepartmentsManageMembers)
            && actor!.AuthorityLevel >= facts.AuthorityLevel;

        if (!canManage)
        {
            return GetPlatformUserDepartmentsResponse.Success(memberships, Array.Empty<PlatformDepartmentListItem>(), false);
        }

        HashSet<Guid> joined = memberships.Select(membership => membership.PlatformDepartmentId).ToHashSet();
        List<PlatformDepartmentListItem> joinable = (await _departments.ListAsync(false, cancellationToken))
            .Where(department => !joined.Contains(department.PlatformDepartmentId))
            .ToList();

        return GetPlatformUserDepartmentsResponse.Success(memberships, joinable, true);
    }
}
