using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.ReleaseOperationsHold;

// =========================================================
// FILE: ReleaseOperationsHoldResponse.cs
//
// PURPOSE:
// Releases a hold with a note, for example "customer sent the invoice by email" (Slice 3C design section 6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReleaseOperationsHoldResponse
{
    private ReleaseOperationsHoldResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ReleaseOperationsHoldResponse Success()
    {
        return new ReleaseOperationsHoldResponse { Succeeded = true };
    }

    public static ReleaseOperationsHoldResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReleaseOperationsHoldResponse { Error = error };
    }
}
