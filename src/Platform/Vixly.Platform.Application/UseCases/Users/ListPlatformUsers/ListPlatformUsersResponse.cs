using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Users.ListPlatformUsers;

// =========================================================
// FILE: ListPlatformUsersResponse.cs
//
// PURPOSE:
// Result of ListPlatformUsers.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformUsersResponse
{
    private ListPlatformUsersResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformUserListItem>? Users { get; private init; }

    public static ListPlatformUsersResponse Success(PagedResult<PlatformUserListItem>? users)
    {
        return new ListPlatformUsersResponse { Succeeded = true, Users = users };
    }

    public static ListPlatformUsersResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformUsersResponse { Error = error };
    }
}
