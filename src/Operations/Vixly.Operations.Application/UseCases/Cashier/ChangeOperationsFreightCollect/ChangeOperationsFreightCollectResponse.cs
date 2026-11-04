using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Cashier.ChangeOperationsFreightCollect;

// =========================================================
// FILE: ChangeOperationsFreightCollectResponse.cs
//
// PURPOSE:
// Freight collect or paid here, chosen at the cashier or the counter (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Report success, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeOperationsFreightCollectResponse
{
    private ChangeOperationsFreightCollectResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    // The waybill printed before shows the old term: the page asks for a reprint.
    public bool Changed { get; private init; }

    public static ChangeOperationsFreightCollectResponse Success(bool changed) => new() { Succeeded = true, Changed = changed };

    public static ChangeOperationsFreightCollectResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeOperationsFreightCollectResponse { Error = error };
    }
}
