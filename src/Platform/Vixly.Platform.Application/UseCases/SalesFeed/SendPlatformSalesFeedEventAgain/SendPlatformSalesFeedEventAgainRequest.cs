namespace Vixly.Platform.Application.UseCases.SalesFeed.SendPlatformSalesFeedEventAgain;

// =========================================================
// FILE: SendPlatformSalesFeedEventAgainRequest.cs
//
// PURPOSE:
// Sends a parked sale event to TechOS again, unchanged.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SendPlatformSalesFeedEventAgainRequest
{
    public Guid EventId { get; init; }
}
