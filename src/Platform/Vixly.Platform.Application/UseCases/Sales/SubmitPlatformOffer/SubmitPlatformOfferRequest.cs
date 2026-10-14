namespace Vixly.Platform.Application.UseCases.Sales.SubmitPlatformOffer;

// =========================================================
// FILE: SubmitPlatformOfferRequest.cs
//
// PURPOSE:
// Issues the offer link at once when within the limits, otherwise asks the sales manager (D-S22, D-S24).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SubmitPlatformOfferRequest
{
    public Guid OfferId { get; init; }
}
