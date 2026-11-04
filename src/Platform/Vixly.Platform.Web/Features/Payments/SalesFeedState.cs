using Vixly.Platform.Application.UseCases.SalesFeed;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Payments;

// =========================================================
// FILE: SalesFeedState.cs
//
// PURPOSE:
// What the sales feed page shows.
//
// RESPONSIBILITIES:
// - Hold the feed overview, whether the user may act, the last message and error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record SalesFeedState(PlatformSalesFeedOverviewItem? Feed, bool CanManage, bool Loaded, string? DoneKey, Error? Error)
{
    public static SalesFeedState Initial { get; } = new(null, false, false, null, null);
}
