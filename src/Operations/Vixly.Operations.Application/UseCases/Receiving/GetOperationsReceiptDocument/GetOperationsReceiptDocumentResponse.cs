using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.GetOperationsReceiptDocument;

// =========================================================
// FILE: GetOperationsReceiptDocumentResponse.cs
//
// PURPOSE:
// The warehouse receipt of a delivery as a PDF (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsReceiptDocumentResponse
{
    private GetOperationsReceiptDocumentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string FileName { get; private init; } = string.Empty;

    public static GetOperationsReceiptDocumentResponse Success(byte[] content, string fileName)
    {
        return new GetOperationsReceiptDocumentResponse { Succeeded = true, Content = content, FileName = fileName };
    }

    public static GetOperationsReceiptDocumentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsReceiptDocumentResponse { Error = error };
    }
}
