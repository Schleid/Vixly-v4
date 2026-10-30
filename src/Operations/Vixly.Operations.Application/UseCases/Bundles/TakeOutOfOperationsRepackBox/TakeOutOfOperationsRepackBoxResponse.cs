using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.TakeOutOfOperationsRepackBox;

// =========================================================
// FILE: TakeOutOfOperationsRepackBoxResponse.cs
//
// PURPOSE:
// Take a parcel out of an open repack box (Slice 6A, D-4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class TakeOutOfOperationsRepackBoxResponse
{
    private TakeOutOfOperationsRepackBoxResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static TakeOutOfOperationsRepackBoxResponse Success()
    {
        return new TakeOutOfOperationsRepackBoxResponse { Succeeded = true };
    }

    public static TakeOutOfOperationsRepackBoxResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new TakeOutOfOperationsRepackBoxResponse { Error = error };
    }
}
