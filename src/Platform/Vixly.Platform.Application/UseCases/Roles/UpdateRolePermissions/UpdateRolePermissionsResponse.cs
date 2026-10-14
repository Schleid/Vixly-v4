using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Roles.UpdateRolePermissions;

// =========================================================
// FILE: UpdateRolePermissionsResponse.cs
//
// PURPOSE:
// Result of UpdateRolePermissions.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdateRolePermissionsResponse
{
    private UpdateRolePermissionsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdateRolePermissionsResponse Success()
    {
        return new UpdateRolePermissionsResponse { Succeeded = true };
    }

    public static UpdateRolePermissionsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdateRolePermissionsResponse { Error = error };
    }
}
