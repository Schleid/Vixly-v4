using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Staff.ChangeOperationsStaffStatus;

// =========================================================
// FILE: ChangeOperationsStaffStatusResponse.cs
//
// PURPOSE:
// Suspends, locks, deactivates or reactivates a staff member, with a reason (Slice 1D).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeOperationsStaffStatusResponse
{
    private ChangeOperationsStaffStatusResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeOperationsStaffStatusResponse Success()
    {
        return new ChangeOperationsStaffStatusResponse { Succeeded = true };
    }

    public static ChangeOperationsStaffStatusResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeOperationsStaffStatusResponse { Error = error };
    }
}
