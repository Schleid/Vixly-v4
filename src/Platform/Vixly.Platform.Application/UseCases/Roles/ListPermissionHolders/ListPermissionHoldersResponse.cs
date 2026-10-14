using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Roles.ListPermissionHolders;

// =========================================================
// FILE: ListPermissionHoldersResponse.cs
//
// PURPOSE:
// Result of ListPermissionHolders.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPermissionHoldersResponse
{
    private ListPermissionHoldersResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<PlatformPermissionHolderItem> Holders { get; private init; } = Array.Empty<PlatformPermissionHolderItem>();

    public static ListPermissionHoldersResponse Success(IReadOnlyList<PlatformPermissionHolderItem> holders)
    {
        return new ListPermissionHoldersResponse { Succeeded = true, Holders = holders };
    }

    public static ListPermissionHoldersResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPermissionHoldersResponse { Error = error };
    }
}
