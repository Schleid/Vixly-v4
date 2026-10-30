using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Arrivals.OpenOperationsArrivalBundle;

// =========================================================
// FILE: OpenOperationsArrivalBundleResponse.cs
//
// PURPOSE:
// Open a bundle at its destination, so its parcels can be scanned (Slice 7, R-1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class OpenOperationsArrivalBundleResponse
{
    private OpenOperationsArrivalBundleResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static OpenOperationsArrivalBundleResponse Success()
    {
        return new OpenOperationsArrivalBundleResponse { Succeeded = true };
    }

    public static OpenOperationsArrivalBundleResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new OpenOperationsArrivalBundleResponse { Error = error };
    }
}
