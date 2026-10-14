namespace Vixly.Platform.Application.UseCases.Sales.RejectPlatformOffer;

// =========================================================
// FILE: RejectPlatformOfferRequest.cs
//
// PURPOSE:
// Rejects an offer waiting for the sales manager, with a reason (D-S24).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RejectPlatformOfferRequest
{
    public Guid OfferId { get; init; }

    public string? Reason { get; init; }
}
