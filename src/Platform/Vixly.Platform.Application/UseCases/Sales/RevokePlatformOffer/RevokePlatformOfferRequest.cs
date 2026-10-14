namespace Vixly.Platform.Application.UseCases.Sales.RevokePlatformOffer;

// =========================================================
// FILE: RevokePlatformOfferRequest.cs
//
// PURPOSE:
// Revokes an offer so its link stops working (D-S21).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RevokePlatformOfferRequest
{
    public Guid OfferId { get; init; }

    public string Reason { get; init; } = string.Empty;
}
