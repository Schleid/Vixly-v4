using Vixly.Operations.Application.Staff;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Roles.ListOperationsRoles;

// =========================================================
// FILE: ListOperationsRolesResponse.cs
//
// PURPOSE:
// Lists the roles of the tenant with their holders (Slice 1D).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsRolesResponse
{
    private ListOperationsRolesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsRoleSummary> Roles { get; private init; } = Array.Empty<OperationsRoleSummary>();

    public static ListOperationsRolesResponse Success(IReadOnlyList<OperationsRoleSummary> roles)
    {
        return new ListOperationsRolesResponse { Succeeded = true, Roles = roles };
    }

    public static ListOperationsRolesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsRolesResponse { Error = error };
    }
}
