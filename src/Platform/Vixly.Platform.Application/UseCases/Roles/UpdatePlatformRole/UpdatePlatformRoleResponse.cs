using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Roles.UpdatePlatformRole;

// =========================================================
// FILE: UpdatePlatformRoleResponse.cs
//
// PURPOSE:
// Result of UpdatePlatformRole.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdatePlatformRoleResponse
{
    private UpdatePlatformRoleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdatePlatformRoleResponse Success()
    {
        return new UpdatePlatformRoleResponse { Succeeded = true };
    }

    public static UpdatePlatformRoleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdatePlatformRoleResponse { Error = error };
    }
}
