using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.SaveOperationsCounterSignature;

// =========================================================
// FILE: SaveOperationsCounterSignatureResponse.cs
//
// PURPOSE:
// The customer signs on screen at the counter for their declaration and insurance choice (Slice 5D, D-8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsCounterSignatureResponse
{
    private SaveOperationsCounterSignatureResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SaveOperationsCounterSignatureResponse Success()
    {
        return new SaveOperationsCounterSignatureResponse { Succeeded = true };
    }

    public static SaveOperationsCounterSignatureResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsCounterSignatureResponse { Error = error };
    }
}
