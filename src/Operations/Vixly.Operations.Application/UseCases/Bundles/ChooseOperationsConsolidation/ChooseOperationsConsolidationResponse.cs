using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.ChooseOperationsConsolidation;

// =========================================================
// FILE: ChooseOperationsConsolidationResponse.cs
//
// PURPOSE:
// Switch how a consolidated shipment travels: repacked or kept together (Slice 6A, D-4; owner, A-3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChooseOperationsConsolidationResponse
{
    private ChooseOperationsConsolidationResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChooseOperationsConsolidationResponse Success()
    {
        return new ChooseOperationsConsolidationResponse { Succeeded = true };
    }

    public static ChooseOperationsConsolidationResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChooseOperationsConsolidationResponse { Error = error };
    }
}
