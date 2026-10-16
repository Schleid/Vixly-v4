namespace Vixly.Platform.Application.UseCases.Legal.AcceptPlatformLegalTerms;

// =========================================================
// FILE: AcceptPlatformLegalTermsRequest.cs
//
// PURPOSE:
// Records the owner's acceptance of the current Terms, sent by Operations (D-Lg5, D-Lg11).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AcceptPlatformLegalTermsRequest
{
    public Guid DocumentId { get; init; }

    public string? Sha256 { get; init; }

    public string? Language { get; init; }
}
