using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.ChangeOperationsCounterParcelCount;

// =========================================================
// FILE: ChangeOperationsCounterParcelCountResponse.cs
//
// PURPOSE:
// Corrects the parcel count of a shipment made at the counter, before its first parcel (Slice 5C, C-1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeOperationsCounterParcelCountResponse
{
    private ChangeOperationsCounterParcelCountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeOperationsCounterParcelCountResponse Success()
    {
        return new ChangeOperationsCounterParcelCountResponse { Succeeded = true };
    }

    public static ChangeOperationsCounterParcelCountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeOperationsCounterParcelCountResponse { Error = error };
    }
}
