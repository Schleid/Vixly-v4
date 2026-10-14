using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Roles.AssignPlatformRole;

// =========================================================
// FILE: AssignPlatformRoleResponse.cs
//
// PURPOSE:
// Result of AssignPlatformRole.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AssignPlatformRoleResponse
{
    private AssignPlatformRoleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static AssignPlatformRoleResponse Success()
    {
        return new AssignPlatformRoleResponse { Succeeded = true };
    }

    public static AssignPlatformRoleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AssignPlatformRoleResponse { Error = error };
    }
}
