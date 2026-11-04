using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.OpenOperationsRepackBox;

// =========================================================
// FILE: OpenOperationsRepackBoxResponse.cs
//
// PURPOSE:
// Start repacking a consolidated shipment into one box (Slice 6A, D-4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class OpenOperationsRepackBoxResponse
{
    private OpenOperationsRepackBoxResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid BoxId { get; private init; }

    public static OpenOperationsRepackBoxResponse Success(Guid boxId)
    {
        return new OpenOperationsRepackBoxResponse { Succeeded = true, BoxId = boxId };
    }

    public static OpenOperationsRepackBoxResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new OpenOperationsRepackBoxResponse { Error = error };
    }
}
