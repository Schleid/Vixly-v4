using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.ChangeOperationsCardAcceptance;

// =========================================================
// FILE: ChangeOperationsCardAcceptanceResponse.cs
//
// PURPOSE:
// Turns cards on or off for the company (Slice 5F step 4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeOperationsCardAcceptanceResponse
{
    private ChangeOperationsCardAcceptanceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeOperationsCardAcceptanceResponse Success()
    {
        return new ChangeOperationsCardAcceptanceResponse { Succeeded = true };
    }

    public static ChangeOperationsCardAcceptanceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeOperationsCardAcceptanceResponse { Error = error };
    }
}
