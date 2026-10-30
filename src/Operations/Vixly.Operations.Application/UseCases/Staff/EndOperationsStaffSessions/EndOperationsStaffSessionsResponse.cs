using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Staff.EndOperationsStaffSessions;

// =========================================================
// FILE: EndOperationsStaffSessionsResponse.cs
//
// PURPOSE:
// Signs a staff member out everywhere (Slice 1D).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class EndOperationsStaffSessionsResponse
{
    private EndOperationsStaffSessionsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public int Ended { get; private init; }

    public static EndOperationsStaffSessionsResponse Success(int ended)
    {
        return new EndOperationsStaffSessionsResponse { Succeeded = true, Ended = ended };
    }

    public static EndOperationsStaffSessionsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new EndOperationsStaffSessionsResponse { Error = error };
    }
}
