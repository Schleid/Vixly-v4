using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: PromotionEditState.cs
//
// PURPOSE:
// What the promotion form shows.
//
// RESPONSIBILITIES:
// - Hold what its component shows
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record PromotionEditState(
    PlatformPromotionDetail? Promotion,
    PlatformSalesAbilities Abilities,
    IReadOnlyList<PlanChoice> Plans,
    bool Loaded,
    int Version,
    Guid? CreatedId,
    string? DoneKey,
    Error? Error)
{
    public static PromotionEditState Initial { get; } = new(null, PlatformSalesAbilities.None, Array.Empty<PlanChoice>(), false, 0, null, null, null);
}
