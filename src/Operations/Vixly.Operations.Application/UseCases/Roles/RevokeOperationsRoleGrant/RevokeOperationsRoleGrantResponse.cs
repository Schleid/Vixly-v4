using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Roles.RevokeOperationsRoleGrant;

// =========================================================
// FILE: RevokeOperationsRoleGrantResponse.cs
//
// PURPOSE:
// Takes a role back from a staff member (Slice 1D).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RevokeOperationsRoleGrantResponse
{
    private RevokeOperationsRoleGrantResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RevokeOperationsRoleGrantResponse Success()
    {
        return new RevokeOperationsRoleGrantResponse { Succeeded = true };
    }

    public static RevokeOperationsRoleGrantResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RevokeOperationsRoleGrantResponse { Error = error };
    }
}
