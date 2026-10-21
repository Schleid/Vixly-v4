using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Staff.ResetOperationsStaffSecondFactor;

// =========================================================
// FILE: ResetOperationsStaffSecondFactorResponse.cs
//
// PURPOSE:
// Removes a staff member's authenticator app and recovery codes so they enrol again (Slice 1D, D-O14).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ResetOperationsStaffSecondFactorResponse
{
    private ResetOperationsStaffSecondFactorResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ResetOperationsStaffSecondFactorResponse Success()
    {
        return new ResetOperationsStaffSecondFactorResponse { Succeeded = true };
    }

    public static ResetOperationsStaffSecondFactorResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ResetOperationsStaffSecondFactorResponse { Error = error };
    }
}
