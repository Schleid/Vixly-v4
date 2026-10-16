using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Collections;

// =========================================================
// FILE: CollectionsOverviewState.cs
//
// PURPOSE:
// What the Collections page shows: the dashboard and one page of cases.
//
// RESPONSIBILITIES:
// - Hold the dashboard figures and a page of cases
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record CollectionsOverviewState(PlatformCollectionsDashboard? Dashboard, IReadOnlyList<PlatformCollectionCaseItem> Items, int Page, int TotalPages, int TotalCount, bool Loaded, Error? Error)
{
    public static CollectionsOverviewState Initial { get; } = new(null, Array.Empty<PlatformCollectionCaseItem>(), 1, 0, 0, false, null);
}
