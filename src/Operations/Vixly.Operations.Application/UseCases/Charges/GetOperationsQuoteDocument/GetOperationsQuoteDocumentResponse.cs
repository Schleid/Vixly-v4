using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.GetOperationsQuoteDocument;

// =========================================================
// FILE: GetOperationsQuoteDocumentResponse.cs
//
// PURPOSE:
// The PDF of a quote and its file name (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsQuoteDocumentResponse
{
    private GetOperationsQuoteDocumentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string FileName { get; private init; } = string.Empty;

    public static GetOperationsQuoteDocumentResponse Success(byte[] content, string fileName) => new() { Succeeded = true, Content = content, FileName = fileName };

    public static GetOperationsQuoteDocumentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsQuoteDocumentResponse { Error = error };
    }
}
