using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Application.UseCases.Departments.GetPlatformDepartment;

// =========================================================
// FILE: GetPlatformDepartmentUseCase.cs
//
// PURPOSE:
// Loads one department with its members, and what the
// signed-in user may do to it.
//
// RESPONSIBILITIES:
// - Require platform.departments.view
// - Answer NotFound for an unknown department
// - Work out the allowed actions from fresh access
//
// IMPORTANT NOTES:
// - Members are added from the user page, where the person is
//   already chosen
// =========================================================
public sealed class GetPlatformDepartmentUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformDepartmentQueries _queries;

    private readonly PlatformDepartmentAdministration _administration;

    public GetPlatformDepartmentUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformDepartmentQueries queries,
        PlatformDepartmentAdministration administration)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
    }

    public async Task<GetPlatformDepartmentResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformDepartmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.DepartmentsView, cancellationToken);

        PlatformDepartmentDetail? department = await _queries.GetAsync(request.PlatformDepartmentId, cancellationToken);

        if (department is null)
        {
            return GetPlatformDepartmentResponse.Failure(PlatformCommonErrors.NotFound);
        }

        PlatformPermissionSnapshot? actor = await _administration.ReadActorAsync(context, cancellationToken);

        return GetPlatformDepartmentResponse.Success(
            department,
            new PlatformDepartmentActions(
                CanEdit: PlatformAccess.Holds(actor, PlatformPermissions.DepartmentsEdit),
                CanDeactivate: department.IsActive && PlatformAccess.Holds(actor, PlatformPermissions.DepartmentsDeactivate),
                CanReactivate: !department.IsActive && PlatformAccess.Holds(actor, PlatformPermissions.DepartmentsDeactivate),
                CanManageMembers: PlatformAccess.Holds(actor, PlatformPermissions.DepartmentsManageMembers)));
    }
}
