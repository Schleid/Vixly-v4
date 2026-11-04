using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Credit.DecideOperationsCreditCharge;

// =========================================================
// FILE: DecideOperationsCreditChargeResponse.cs
//
// PURPOSE:
// The credit department approves or refuses a charge above the limit (Slice 5F step 2b, K-3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DecideOperationsCreditChargeResponse
{
    private DecideOperationsCreditChargeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DecideOperationsCreditChargeResponse Success()
    {
        return new DecideOperationsCreditChargeResponse { Succeeded = true };
    }

    public static DecideOperationsCreditChargeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DecideOperationsCreditChargeResponse { Error = error };
    }
}
