using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.VoidOperationsRepackBox;

// =========================================================
// FILE: VoidOperationsRepackBoxResponse.cs
//
// PURPOSE:
// Void a repack box in no bundle, with a reason (Slice 6A, D-4): its parcels are loose again.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class VoidOperationsRepackBoxResponse
{
    private VoidOperationsRepackBoxResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static VoidOperationsRepackBoxResponse Success()
    {
        return new VoidOperationsRepackBoxResponse { Succeeded = true };
    }

    public static VoidOperationsRepackBoxResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new VoidOperationsRepackBoxResponse { Error = error };
    }
}
