using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Roles.GrantOperationsRole;

// =========================================================
// FILE: GrantOperationsRoleResponse.cs
//
// PURPOSE:
// Gives a staff member a role for the tenant, a branch or a warehouse (Slice 1D).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GrantOperationsRoleResponse
{
    private GrantOperationsRoleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid GrantId { get; private init; }

    public static GrantOperationsRoleResponse Success(Guid grantId)
    {
        return new GrantOperationsRoleResponse { Succeeded = true, GrantId = grantId };
    }

    public static GrantOperationsRoleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GrantOperationsRoleResponse { Error = error };
    }
}
