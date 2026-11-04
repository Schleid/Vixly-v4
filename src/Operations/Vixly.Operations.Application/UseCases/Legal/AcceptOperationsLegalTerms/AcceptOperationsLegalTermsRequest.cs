namespace Vixly.Operations.Application.UseCases.Legal.AcceptOperationsLegalTerms;

// =========================================================
// FILE: AcceptOperationsLegalTermsRequest.cs
//
// PURPOSE:
// Records the Owner's acceptance of the Terms version shown (Slice 1C).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AcceptOperationsLegalTermsRequest
{
    public Guid DocumentId { get; init; }

    public string Sha256 { get; init; } = string.Empty;

    public string Language { get; init; } = "en";
}
