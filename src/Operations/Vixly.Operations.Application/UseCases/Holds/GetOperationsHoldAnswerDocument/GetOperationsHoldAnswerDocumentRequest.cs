namespace Vixly.Operations.Application.UseCases.Holds.GetOperationsHoldAnswerDocument;

// =========================================================
// FILE: GetOperationsHoldAnswerDocumentRequest.cs
//
// PURPOSE:
// The document a customer sent with an answer, for staff to read (Slice 3E).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsHoldAnswerDocumentRequest
{
    public Guid AnswerId { get; init; }
}
