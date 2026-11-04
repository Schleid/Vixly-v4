using Vixly.Platform.Application.Maintenance;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Maintenance;

// =========================================================
// FILE: MaintenanceState.cs
//
// PURPOSE:
// What the maintenance page shows (Control backlog 2026-10-22, section 1).
//
// RESPONSIBILITIES:
// - Hold the open windows, one page of past ones, the right to manage, the pass link just made, and any message
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// - The pass link lives here only until the page is left: it is never stored in clear
// =========================================================
public sealed record MaintenanceState(
    bool Loaded,
    IReadOnlyList<PlatformMaintenanceWindowItem> Open,
    IReadOnlyList<PlatformMaintenanceWindowItem> Past,
    int Page,
    int TotalPages,
    bool CanManage,
    bool StepUpPending,
    string? PassToken,
    string? DoneKey,
    Error? Error)
{
    public static MaintenanceState Initial { get; } = new(false, Array.Empty<PlatformMaintenanceWindowItem>(), Array.Empty<PlatformMaintenanceWindowItem>(), 1, 0, false, false, null, null, null);
}
