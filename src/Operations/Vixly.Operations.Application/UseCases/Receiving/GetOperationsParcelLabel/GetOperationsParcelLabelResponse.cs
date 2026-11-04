using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.GetOperationsParcelLabel;

// =========================================================
// FILE: GetOperationsParcelLabelResponse.cs
//
// PURPOSE:
// The ID barcode label of a parcel, as a PDF (Slice 3B, D-R3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsParcelLabelResponse
{
    private GetOperationsParcelLabelResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string FileName { get; private init; } = string.Empty;

    public static GetOperationsParcelLabelResponse Success(byte[] content, string fileName)
    {
        return new GetOperationsParcelLabelResponse { Succeeded = true, Content = content, FileName = fileName };
    }

    public static GetOperationsParcelLabelResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsParcelLabelResponse { Error = error };
    }
}
