namespace Vixly.Operations.Application.UseCases.Insurance.RemoveOperationsPolicyDocument;

// =========================================================
// FILE: RemoveOperationsPolicyDocumentRequest.cs
//
// PURPOSE:
// Removes a document filed with an insurance policy (Slice 5B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RemoveOperationsPolicyDocumentRequest
{
    public Guid PolicyId { get; init; }

    public Guid DocumentId { get; init; }
}
