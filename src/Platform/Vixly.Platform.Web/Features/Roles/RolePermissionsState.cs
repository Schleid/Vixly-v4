using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Roles;

// =========================================================
// FILE: RolePermissionsState.cs
//
// PURPOSE:
// What the permission editor shows.
//
// RESPONSIBILITIES:
// - Hold the saved permissions, whether they may be edited, the
//   step-up state, the last result and any error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// - The ticked boxes live in the component until saved
// =========================================================
public sealed record RolePermissionsState(
    bool Loaded,
    bool IsSystem,
    IReadOnlySet<string> Saved,
    bool CanEdit,
    bool StepUpPending,
    IReadOnlyList<string> Pending,
    bool Done,
    Error? Error)
{
    public static RolePermissionsState Initial { get; } =
        new(false, false, new HashSet<string>(StringComparer.Ordinal), false, false, Array.Empty<string>(), false, null);
}
