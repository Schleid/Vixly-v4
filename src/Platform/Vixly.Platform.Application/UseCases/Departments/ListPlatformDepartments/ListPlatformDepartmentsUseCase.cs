using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Application.UseCases.Departments.ListPlatformDepartments;

// =========================================================
// FILE: ListPlatformDepartmentsUseCase.cs
//
// PURPOSE:
// Lists the departments for the departments screen.
//
// RESPONSIBILITIES:
// - Require platform.departments.view
// - Return active departments, and inactive ones when asked
// - Say whether the signed-in user may create one
//
// IMPORTANT NOTES:
// - Small, bounded list: no paging
// =========================================================
public sealed class ListPlatformDepartmentsUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformDepartmentQueries _queries;

    private readonly PlatformDepartmentAdministration _administration;

    public ListPlatformDepartmentsUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformDepartmentQueries queries,
        PlatformDepartmentAdministration administration)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
    }

    public async Task<ListPlatformDepartmentsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformDepartmentsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.DepartmentsView, cancellationToken);

        IReadOnlyList<PlatformDepartmentListItem> departments = await _queries.ListAsync(request.IncludeInactive, cancellationToken);
        PlatformPermissionSnapshot? actor = await _administration.ReadActorAsync(context, cancellationToken);

        return ListPlatformDepartmentsResponse.Success(departments, PlatformAccess.Holds(actor, PlatformPermissions.DepartmentsCreate));
    }
}
