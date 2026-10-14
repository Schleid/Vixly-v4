using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: ApprovalListState.cs
//
// PURPOSE:
// What the Approvals page shows.
//
// RESPONSIBILITIES:
// - Hold the requests waiting for the current person
//
// IMPORTANT NOTES:
// - Immutable
// =========================================================
public sealed record ApprovalListState(IReadOnlyList<PlatformApprovalItem> Items, bool Loaded, Error? Error)
{
    public static ApprovalListState Initial { get; } = new(Array.Empty<PlatformApprovalItem>(), false, null);
}
