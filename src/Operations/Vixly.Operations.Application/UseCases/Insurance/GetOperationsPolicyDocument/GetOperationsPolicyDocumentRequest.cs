namespace Vixly.Operations.Application.UseCases.Insurance.GetOperationsPolicyDocument;

// =========================================================
// FILE: GetOperationsPolicyDocumentRequest.cs
//
// PURPOSE:
// A document filed with an insurance policy, for staff (Slice 5B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsPolicyDocumentRequest
{
    public Guid PolicyId { get; init; }

    public Guid DocumentId { get; init; }
}
