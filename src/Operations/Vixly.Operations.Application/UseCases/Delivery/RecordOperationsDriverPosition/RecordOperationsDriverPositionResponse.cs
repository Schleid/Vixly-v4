using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Delivery.RecordOperationsDriverPosition;

// =========================================================
// FILE: RecordOperationsDriverPositionResponse.cs
//
// PURPOSE:
// Keeps the position the driver's phone sends while the route is out (Slice 12C, owner H-5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RecordOperationsDriverPositionResponse
{
    private RecordOperationsDriverPositionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool Kept { get; private init; }

    public static RecordOperationsDriverPositionResponse Success(bool kept)
    {
        return new RecordOperationsDriverPositionResponse { Succeeded = true, Kept = kept };
    }

    public static RecordOperationsDriverPositionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RecordOperationsDriverPositionResponse { Error = error };
    }
}
