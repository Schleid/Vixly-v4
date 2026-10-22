using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Billing.GetMyOperationsInvoiceDocument;

// =========================================================
// FILE: GetMyOperationsInvoiceDocumentResponse.cs
//
// PURPOSE:
// The PDF of one of the customer's own invoices or approved credit notes (Slice 5F, I-8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsInvoiceDocumentResponse
{
    private GetMyOperationsInvoiceDocumentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string FileName { get; private init; } = string.Empty;

    public static GetMyOperationsInvoiceDocumentResponse Success(byte[] content, string fileName)
    {
        return new GetMyOperationsInvoiceDocumentResponse { Succeeded = true, Content = content, FileName = fileName };
    }

    public static GetMyOperationsInvoiceDocumentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsInvoiceDocumentResponse { Error = error };
    }
}
