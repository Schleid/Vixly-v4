using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Arrivals.ScanOperationsArrival;

// =========================================================
// FILE: ScanOperationsArrivalResponse.cs
//
// PURPOSE:
// Scan a bundle, a repack box or a parcel arriving at a destination warehouse (Slice 7, R-1, R-2, R-11).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ScanOperationsArrivalResponse
{
    private ScanOperationsArrivalResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string Outcome { get; private init; } = string.Empty;

    public string Number { get; private init; } = string.Empty;

    public int Ready { get; private init; }

    public static ScanOperationsArrivalResponse Success(string outcome, string number, int ready)
    {
        return new ScanOperationsArrivalResponse { Succeeded = true, Outcome = outcome, Number = number, Ready = ready };
    }

    public static ScanOperationsArrivalResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ScanOperationsArrivalResponse { Error = error };
    }
}
