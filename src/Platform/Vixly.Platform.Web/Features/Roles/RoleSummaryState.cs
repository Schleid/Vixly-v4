using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Roles;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Roles;

// =========================================================
// FILE: RoleSummaryState.cs
//
// PURPOSE:
// What the role summary shows.
//
// RESPONSIBILITIES:
// - Hold the role, the allowed actions, which form is open, the
//   last result and any error
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// - DoneKey is a text key (UI-10)
// =========================================================
public sealed record RoleSummaryState(
    PlatformRoleDetail? Role,
    PlatformRoleActions Actions,
    bool Renaming,
    bool ConfirmingDeactivate,
    bool StepUpPending,
    string? DoneKey,
    Error? Error)
{
    public static RoleSummaryState Initial { get; } = new(null, PlatformRoleActions.None, false, false, false, null, null);
}
