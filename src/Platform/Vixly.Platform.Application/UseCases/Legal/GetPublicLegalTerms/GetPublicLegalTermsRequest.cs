namespace Vixly.Platform.Application.UseCases.Legal.GetPublicLegalTerms;

// =========================================================
// FILE: GetPublicLegalTermsRequest.cs
//
// PURPOSE:
// Reads the current Terms in a language for the registration page (D-Lg9).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPublicLegalTermsRequest
{
    public string? Language { get; init; }
}
