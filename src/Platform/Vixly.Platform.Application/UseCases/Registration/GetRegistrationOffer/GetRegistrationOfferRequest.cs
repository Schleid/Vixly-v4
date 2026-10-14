namespace Vixly.Platform.Application.UseCases.Registration.GetRegistrationOffer;

// =========================================================
// FILE: GetRegistrationOfferRequest.cs
//
// PURPOSE:
// Shows the terms of an offer link to the prospect (D-S21).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetRegistrationOfferRequest
{
    public string Token { get; init; } = string.Empty;

    public string? Language { get; init; }
}
