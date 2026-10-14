using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Roles.DeactivatePlatformRole;

// =========================================================
// FILE: DeactivatePlatformRoleResponse.cs
//
// PURPOSE:
// Result of DeactivatePlatformRole.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DeactivatePlatformRoleResponse
{
    private DeactivatePlatformRoleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DeactivatePlatformRoleResponse Success()
    {
        return new DeactivatePlatformRoleResponse { Succeeded = true };
    }

    public static DeactivatePlatformRoleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DeactivatePlatformRoleResponse { Error = error };
    }
}
