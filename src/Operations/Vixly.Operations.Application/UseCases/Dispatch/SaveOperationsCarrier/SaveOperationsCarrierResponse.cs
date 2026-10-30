using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.SaveOperationsCarrier;

// =========================================================
// FILE: SaveOperationsCarrierResponse.cs
//
// PURPOSE:
// Add a carrier or change one (Slice 6B, D-1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsCarrierResponse
{
    private SaveOperationsCarrierResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid CarrierId { get; private init; }

    public static SaveOperationsCarrierResponse Success(Guid carrierId)
    {
        return new SaveOperationsCarrierResponse { Succeeded = true, CarrierId = carrierId };
    }

    public static SaveOperationsCarrierResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsCarrierResponse { Error = error };
    }
}
