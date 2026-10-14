using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Roles.GetPlatformRole;

// =========================================================
// FILE: GetPlatformRoleResponse.cs
//
// PURPOSE:
// Result of GetPlatformRole.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformRoleResponse
{
    private GetPlatformRoleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformRoleDetail? Role { get; private init; }

    public PlatformRoleActions Actions { get; private init; } = PlatformRoleActions.None;

    public static GetPlatformRoleResponse Success(PlatformRoleDetail? role, PlatformRoleActions actions)
    {
        return new GetPlatformRoleResponse { Succeeded = true, Role = role, Actions = actions };
    }

    public static GetPlatformRoleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformRoleResponse { Error = error };
    }
}
