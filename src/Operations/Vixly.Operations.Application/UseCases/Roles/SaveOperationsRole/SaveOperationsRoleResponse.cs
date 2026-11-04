using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Roles.SaveOperationsRole;

// =========================================================
// FILE: SaveOperationsRoleResponse.cs
//
// PURPOSE:
// Creates a role or changes its name and permissions (Slice 1D).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsRoleResponse
{
    private SaveOperationsRoleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid RoleId { get; private init; }

    public static SaveOperationsRoleResponse Success(Guid roleId)
    {
        return new SaveOperationsRoleResponse { Succeeded = true, RoleId = roleId };
    }

    public static SaveOperationsRoleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsRoleResponse { Error = error };
    }
}
