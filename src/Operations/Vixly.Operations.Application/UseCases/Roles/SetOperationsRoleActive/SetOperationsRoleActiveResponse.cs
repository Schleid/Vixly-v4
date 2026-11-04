using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Roles.SetOperationsRoleActive;

// =========================================================
// FILE: SetOperationsRoleActiveResponse.cs
//
// PURPOSE:
// Deactivates or reactivates a role (Slice 1D).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SetOperationsRoleActiveResponse
{
    private SetOperationsRoleActiveResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SetOperationsRoleActiveResponse Success()
    {
        return new SetOperationsRoleActiveResponse { Succeeded = true };
    }

    public static SetOperationsRoleActiveResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SetOperationsRoleActiveResponse { Error = error };
    }
}
