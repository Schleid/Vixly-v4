using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.CloseOperationsRepackBox;

// =========================================================
// FILE: CloseOperationsRepackBoxResponse.cs
//
// PURPOSE:
// Close a repack box with its weight and size once every parcel is in (Slice 6A, D-4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CloseOperationsRepackBoxResponse
{
    private CloseOperationsRepackBoxResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static CloseOperationsRepackBoxResponse Success()
    {
        return new CloseOperationsRepackBoxResponse { Succeeded = true };
    }

    public static CloseOperationsRepackBoxResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CloseOperationsRepackBoxResponse { Error = error };
    }
}
