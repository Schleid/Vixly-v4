using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.ChangeOperationsCounterSupplyLine;

// =========================================================
// FILE: ChangeOperationsCounterSupplyLineResponse.cs
//
// PURPOSE:
// A line of supplies changed at the counter (Slice 5E, S-11, S-17 to S-19): its quantity (0 removes it) and its discount within the company's policy.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeOperationsCounterSupplyLineResponse
{
    private ChangeOperationsCounterSupplyLineResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool LowStock { get; private init; }

    public static ChangeOperationsCounterSupplyLineResponse Success(bool lowStock)
    {
        return new ChangeOperationsCounterSupplyLineResponse { Succeeded = true, LowStock = lowStock };
    }

    public static ChangeOperationsCounterSupplyLineResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeOperationsCounterSupplyLineResponse { Error = error };
    }
}
