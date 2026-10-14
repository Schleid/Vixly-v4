using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: OfferListState.cs
//
// PURPOSE:
// What the offer list shows.
//
// RESPONSIBILITIES:
// - Hold what its component shows
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record OfferListState(
    IReadOnlyList<PlatformOfferListItem> Offers,
    int Page,
    int TotalPages,
    int TotalCount,
    PlatformSalesAbilities Abilities,
    bool Loaded,
    Error? Error)
{
    public static OfferListState Initial { get; } = new(Array.Empty<PlatformOfferListItem>(), 1, 0, 0, PlatformSalesAbilities.None, false, null);
}
