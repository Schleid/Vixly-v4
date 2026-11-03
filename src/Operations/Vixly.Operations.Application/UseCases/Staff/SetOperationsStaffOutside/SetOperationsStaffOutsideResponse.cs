using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Staff.SetOperationsStaffOutside;

// =========================================================
// FILE: SetOperationsStaffOutsideResponse.cs
//
// PURPOSE:
// Makes a staff account an outside one (a call centre's agent), with its end date and working hours, or an ordinary one again (owner, 2026-10-29, S-5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SetOperationsStaffOutsideResponse
{
    private SetOperationsStaffOutsideResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SetOperationsStaffOutsideResponse Success()
    {
        return new SetOperationsStaffOutsideResponse { Succeeded = true };
    }

    public static SetOperationsStaffOutsideResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SetOperationsStaffOutsideResponse { Error = error };
    }
}
