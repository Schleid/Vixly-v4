using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.GetOperationsInvoiceDocument;

// =========================================================
// FILE: GetOperationsInvoiceDocumentResponse.cs
//
// PURPOSE:
// The PDF of an invoice or of one of its approved credit notes (Slice 5F, I-2 and I-6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsInvoiceDocumentResponse
{
    private GetOperationsInvoiceDocumentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string FileName { get; private init; } = string.Empty;

    public static GetOperationsInvoiceDocumentResponse Success(byte[] content, string fileName)
    {
        return new GetOperationsInvoiceDocumentResponse { Succeeded = true, Content = content, FileName = fileName };
    }

    public static GetOperationsInvoiceDocumentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsInvoiceDocumentResponse { Error = error };
    }
}
