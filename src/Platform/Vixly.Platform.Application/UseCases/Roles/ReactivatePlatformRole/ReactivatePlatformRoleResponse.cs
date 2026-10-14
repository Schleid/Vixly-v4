using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Roles.ReactivatePlatformRole;

// =========================================================
// FILE: ReactivatePlatformRoleResponse.cs
//
// PURPOSE:
// Result of ReactivatePlatformRole.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReactivatePlatformRoleResponse
{
    private ReactivatePlatformRoleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ReactivatePlatformRoleResponse Success()
    {
        return new ReactivatePlatformRoleResponse { Succeeded = true };
    }

    public static ReactivatePlatformRoleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReactivatePlatformRoleResponse { Error = error };
    }
}
