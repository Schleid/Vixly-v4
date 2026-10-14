using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantHistoryState.cs
//
// PURPOSE:
// State of the history section.
//
// RESPONSIBILITIES:
// - Hold one page of history events
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record TenantHistoryState(
    IReadOnlyList<PlatformTenantEventItem> Events,
    int Page,
    int TotalPages,
    bool Loaded,
    Error? Error)
{
    public static TenantHistoryState Initial { get; } = new(Array.Empty<PlatformTenantEventItem>(), 1, 0, false, null);
}
