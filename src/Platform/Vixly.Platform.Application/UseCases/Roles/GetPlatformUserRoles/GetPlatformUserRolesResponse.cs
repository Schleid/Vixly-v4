using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Roles.GetPlatformUserRoles;

// =========================================================
// FILE: GetPlatformUserRolesResponse.cs
//
// PURPOSE:
// Result of GetPlatformUserRoles.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformUserRolesResponse
{
    private GetPlatformUserRolesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<PlatformUserRoleItem> Grants { get; private init; } = Array.Empty<PlatformUserRoleItem>();

    public IReadOnlyList<PlatformRoleListItem> AssignableRoles { get; private init; } = Array.Empty<PlatformRoleListItem>();

    public bool CanManage { get; private init; }

    public static GetPlatformUserRolesResponse Success(IReadOnlyList<PlatformUserRoleItem> grants, IReadOnlyList<PlatformRoleListItem> assignableRoles, bool canManage)
    {
        return new GetPlatformUserRolesResponse { Succeeded = true, Grants = grants, AssignableRoles = assignableRoles, CanManage = canManage };
    }

    public static GetPlatformUserRolesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformUserRolesResponse { Error = error };
    }
}
