using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Roles.ListPlatformRoles;

// =========================================================
// FILE: ListPlatformRolesResponse.cs
//
// PURPOSE:
// Result of ListPlatformRoles.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformRolesResponse
{
    private ListPlatformRolesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<PlatformRoleListItem> Roles { get; private init; } = Array.Empty<PlatformRoleListItem>();

    public bool CanCreate { get; private init; }

    public static ListPlatformRolesResponse Success(IReadOnlyList<PlatformRoleListItem> roles, bool canCreate)
    {
        return new ListPlatformRolesResponse { Succeeded = true, Roles = roles, CanCreate = canCreate };
    }

    public static ListPlatformRolesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformRolesResponse { Error = error };
    }
}
