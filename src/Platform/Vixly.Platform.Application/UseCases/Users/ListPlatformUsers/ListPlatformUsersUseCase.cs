using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Users.ListPlatformUsers;

// =========================================================
// FILE: ListPlatformUsersUseCase.cs
//
// PURPOSE:
// Pages the staff user list for the Users screen.
//
// RESPONSIBILITIES:
// - Require platform.users.view
// - Clean the search text and cap the page size (P-1)
//
// IMPORTANT NOTES:
// - Read-only: no audit entry
// =========================================================
public sealed class ListPlatformUsersUseCase
{
    private const int SearchMaxLength = 100;

    private readonly IPlatformAuthorizationGate _gate;

    private readonly IPlatformUserQueries _queries;

    public ListPlatformUsersUseCase(IPlatformAuthorizationGate gate, IPlatformUserQueries queries)
    {
        _gate = gate ?? throw new ArgumentNullException(nameof(gate));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<ListPlatformUsersResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformUsersRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        await _gate.AuthorizeAsync(context, PlatformPermissions.UsersView, cancellationToken);

        string? search = string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim();

        if (search is { Length: > SearchMaxLength })
        {
            search = search[..SearchMaxLength];
        }

        if (request.Status is { } status && !Enum.IsDefined(status))
        {
            return ListPlatformUsersResponse.Failure(PlatformCommonErrors.RequestInvalid);
        }

        PagedResult<PlatformUserListItem> users = await _queries.ListAsync(
            new PlatformUserListFilter(search, request.Status),
            PagedRequest.Create(request.Page, request.PageSize),
            cancellationToken);

        return ListPlatformUsersResponse.Success(users);
    }
}
