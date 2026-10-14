namespace Vixly.Platform.Application.UseCases.Sales.ReplacePlatformOfferLink;

// =========================================================
// FILE: ReplacePlatformOfferLinkRequest.cs
//
// PURPOSE:
// Issues a new link for an issued offer; the old link stops working.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ReplacePlatformOfferLinkRequest
{
    public Guid OfferId { get; init; }
}
