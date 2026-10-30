using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Holds.GetOperationsHoldAnswerDocument;

// =========================================================
// FILE: GetOperationsHoldAnswerDocumentResponse.cs
//
// PURPOSE:
// The document a customer sent with an answer, for staff to read (Slice 3E).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsHoldAnswerDocumentResponse
{
    private GetOperationsHoldAnswerDocumentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string ContentType { get; private init; } = string.Empty;

    public string FileName { get; private init; } = string.Empty;

    public static GetOperationsHoldAnswerDocumentResponse Success(byte[] content, string contentType, string fileName)
    {
        return new GetOperationsHoldAnswerDocumentResponse { Succeeded = true, Content = content, ContentType = contentType, FileName = fileName };
    }

    public static GetOperationsHoldAnswerDocumentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsHoldAnswerDocumentResponse { Error = error };
    }
}
