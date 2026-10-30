using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Holds.ChangeOperationsUnclaimedDays;

// =========================================================
// FILE: ChangeOperationsUnclaimedDaysResponse.cs
//
// PURPOSE:
// Changes after how many days a waiting package goes to the decision list (Slice 3E, Q-R6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeOperationsUnclaimedDaysResponse
{
    private ChangeOperationsUnclaimedDaysResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeOperationsUnclaimedDaysResponse Success()
    {
        return new ChangeOperationsUnclaimedDaysResponse { Succeeded = true };
    }

    public static ChangeOperationsUnclaimedDaysResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeOperationsUnclaimedDaysResponse { Error = error };
    }
}
