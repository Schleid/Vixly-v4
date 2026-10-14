using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: OfferEditState.cs
//
// PURPOSE:
// What the offer form and page show.
//
// RESPONSIBILITIES:
// - Hold what its component shows
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record OfferEditState(
    PlatformOfferDetail? Offer,
    PlatformSalesAbilities Abilities,
    IReadOnlyList<PlanChoice> Plans,
    bool Loaded,
    int Version,
    string? DoneKey,
    Error? Error)
{
    public static OfferEditState Initial { get; } = new(null, PlatformSalesAbilities.None, Array.Empty<PlanChoice>(), false, 0, null, null);
}
