using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: PromotionListState.cs
//
// PURPOSE:
// What the promotion list shows.
//
// RESPONSIBILITIES:
// - Hold what its component shows
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record PromotionListState(
    IReadOnlyList<PlatformPromotionListItem> Promotions,
    int Page,
    int TotalPages,
    int TotalCount,
    PlatformSalesAbilities Abilities,
    bool Loaded,
    Error? Error)
{
    public static PromotionListState Initial { get; } = new(Array.Empty<PlatformPromotionListItem>(), 1, 0, 0, PlatformSalesAbilities.None, false, null);
}
