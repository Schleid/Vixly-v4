namespace Vixly.Platform.Application.UseCases.Sales.GetPlatformOffer;

// =========================================================
// FILE: GetPlatformOfferRequest.cs
//
// PURPOSE:
// Reads one offer link for its console page.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformOfferRequest
{
    public Guid OfferId { get; init; }

    public string? Language { get; init; }
}
