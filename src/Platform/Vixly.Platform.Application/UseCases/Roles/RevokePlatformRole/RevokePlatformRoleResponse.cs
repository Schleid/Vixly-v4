using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Roles.RevokePlatformRole;

// =========================================================
// FILE: RevokePlatformRoleResponse.cs
//
// PURPOSE:
// Result of RevokePlatformRole.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RevokePlatformRoleResponse
{
    private RevokePlatformRoleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RevokePlatformRoleResponse Success()
    {
        return new RevokePlatformRoleResponse { Succeeded = true };
    }

    public static RevokePlatformRoleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RevokePlatformRoleResponse { Error = error };
    }
}
