namespace Vixly.Operations.Application.UseCases.Receiving.GetOperationsReceiptDocument;

// =========================================================
// FILE: GetOperationsReceiptDocumentRequest.cs
//
// PURPOSE:
// The warehouse receipt of a delivery as a PDF (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsReceiptDocumentRequest
{
    public Guid ReceiptId { get; init; }
}
