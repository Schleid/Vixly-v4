using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;

namespace Vixly.Platform.Application.UseCases.Roles.ListPermissionHolders;

// =========================================================
// FILE: ListPermissionHoldersUseCase.cs
//
// PURPOSE:
// Answers "who has this permission": every active staff user
// who holds it now, and through which role.
//
// RESPONSIBILITIES:
// - Require platform.roles.view
// - Accept only codes in the permission catalogue
// - Include SuperUsers, who hold every permission
//
// IMPORTANT NOTES:
// - Read-only; no audit
// =========================================================
public sealed class ListPermissionHoldersUseCase
{
    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformRoleQueries _queries;

    private readonly TimeProvider _timeProvider;

    public ListPermissionHoldersUseCase(
        IPlatformAuthorizationGate gate,
        IPlatformRoleQueries queries,
        TimeProvider timeProvider)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ListPermissionHoldersResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPermissionHoldersRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.RolesView, cancellationToken);

        if (!PlatformPermissions.IsKnown(request.PermissionCode))
        {
            return ListPermissionHoldersResponse.Failure(PlatformCommonErrors.PermissionUnknown);
        }

        IReadOnlyList<PlatformPermissionHolderItem> holders = await _queries.ListPermissionHoldersAsync(
            request.PermissionCode,
            _timeProvider.GetUtcNow(),
            cancellationToken);

        return ListPermissionHoldersResponse.Success(holders);
    }
}
