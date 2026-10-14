namespace Vixly.Platform.Application.UseCases.Sales.ApprovePlatformOffer;

// =========================================================
// FILE: ApprovePlatformOfferRequest.cs
//
// PURPOSE:
// Approves an offer waiting for the sales manager and issues its link (D-S24).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ApprovePlatformOfferRequest
{
    public Guid OfferId { get; init; }

    public string? Reason { get; init; }
}
