namespace Vixly.Operations.Application.UseCases.Insurance.AttachOperationsPolicyDocument;

// =========================================================
// FILE: AttachOperationsPolicyDocumentRequest.cs
//
// PURPOSE:
// Files a document with an insurance policy: the policy itself, an endorsement, a certificate (Slice 5B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AttachOperationsPolicyDocumentRequest
{
    public Guid PolicyId { get; init; }

    public string Kind { get; init; } = "policy";

    public string? FileName { get; init; }

    public byte[] Content { get; init; } = Array.Empty<byte>();
}
