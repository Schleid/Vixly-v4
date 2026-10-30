using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.GetOperationsRepackBoxLabel;

// =========================================================
// FILE: GetOperationsRepackBoxLabelResponse.cs
//
// PURPOSE:
// The 4 x 6 label of a repack box (Slice 6A, D-4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsRepackBoxLabelResponse
{
    private GetOperationsRepackBoxLabelResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Pdf { get; private init; } = Array.Empty<byte>();

    public string Number { get; private init; } = string.Empty;

    public static GetOperationsRepackBoxLabelResponse Success(byte[] pdf, string number)
    {
        return new GetOperationsRepackBoxLabelResponse { Succeeded = true, Pdf = pdf, Number = number };
    }

    public static GetOperationsRepackBoxLabelResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsRepackBoxLabelResponse { Error = error };
    }
}
