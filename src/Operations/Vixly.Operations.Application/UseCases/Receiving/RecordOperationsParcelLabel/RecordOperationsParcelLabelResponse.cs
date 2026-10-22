using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.RecordOperationsParcelLabel;

// =========================================================
// FILE: RecordOperationsParcelLabelResponse.cs
//
// PURPOSE:
// Records what the label of an undeclared package shows: the name and mailbox code written on it, the sender (Slice 4D step 3, design section 6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RecordOperationsParcelLabelResponse
{
    private RecordOperationsParcelLabelResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RecordOperationsParcelLabelResponse Success()
    {
        return new RecordOperationsParcelLabelResponse { Succeeded = true };
    }

    public static RecordOperationsParcelLabelResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RecordOperationsParcelLabelResponse { Error = error };
    }
}
