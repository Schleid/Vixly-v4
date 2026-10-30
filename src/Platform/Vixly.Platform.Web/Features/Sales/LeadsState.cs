using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: LeadsState.cs
//
// PURPOSE:
// What the leads list shows.
//
// RESPONSIBILITIES:
// - Hold one page of leads and the paging numbers
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record LeadsState(IReadOnlyList<PlatformSalesLeadItem> Items, int Page, int TotalPages, int TotalCount, bool Loaded, Error? Error)
{
    public static LeadsState Initial { get; } = new(Array.Empty<PlatformSalesLeadItem>(), 1, 0, 0, false, null);
}
