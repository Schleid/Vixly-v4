using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Payments;

// =========================================================
// FILE: ZelleClaimsState.cs
//
// PURPOSE:
// What the Zelle reports list shows.
//
// RESPONSIBILITIES:
// - Hold one page of reports and the paging numbers
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record ZelleClaimsState(IReadOnlyList<PlatformZelleClaimItem> Items, int Page, int TotalPages, int TotalCount, bool Loaded, Error? Error)
{
    public static ZelleClaimsState Initial { get; } = new(Array.Empty<PlatformZelleClaimItem>(), 1, 0, 0, false, null);
}
