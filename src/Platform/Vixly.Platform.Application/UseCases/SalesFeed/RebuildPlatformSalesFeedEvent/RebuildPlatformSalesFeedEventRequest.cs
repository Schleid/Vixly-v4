namespace Vixly.Platform.Application.UseCases.SalesFeed.RebuildPlatformSalesFeedEvent;

// =========================================================
// FILE: RebuildPlatformSalesFeedEventRequest.cs
//
// PURPOSE:
// Input of RebuildPlatformSalesFeedEvent.
//
// RESPONSIBILITIES:
// - Carry the rejected event to write again
//
// IMPORTANT NOTES:
// - Only a rejected (parked) event can be rebuilt
// =========================================================
public sealed class RebuildPlatformSalesFeedEventRequest
{
    public Guid EventId { get; init; }
}
