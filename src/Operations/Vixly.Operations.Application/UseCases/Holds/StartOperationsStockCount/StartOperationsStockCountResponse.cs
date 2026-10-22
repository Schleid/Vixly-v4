using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Holds.StartOperationsStockCount;

// =========================================================
// FILE: StartOperationsStockCountResponse.cs
//
// PURPOSE:
// Starts a count of the undeclared parcels area of a warehouse: every package nobody could place, with its shelf (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class StartOperationsStockCountResponse
{
    private StartOperationsStockCountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid CountId { get; private init; }

    public static StartOperationsStockCountResponse Success(Guid countId)
    {
        return new StartOperationsStockCountResponse { Succeeded = true, CountId = countId };
    }

    public static StartOperationsStockCountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new StartOperationsStockCountResponse { Error = error };
    }
}
