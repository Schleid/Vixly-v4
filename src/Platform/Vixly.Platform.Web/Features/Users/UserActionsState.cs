using Vixly.Platform.Application.UseCases.Users;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Users;

// =========================================================
// FILE: UserActionsState.cs
//
// PURPOSE:
// What an administrator action panel shows.
//
// RESPONSIBILITIES:
// - Carry the actions on offer, the one being confirmed, the last
//   one done, the error, and whether a step-up is pending
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// - PendingReason keeps the typed reason while the step-up
//   dialog is open, so the user does not type it twice
// =========================================================
public sealed record UserActionsState(
    IReadOnlyCollection<PlatformUserAdminAction> Available,
    PlatformUserAdminAction? Selected,
    PlatformUserAdminAction? Done,
    Error? Error,
    bool StepUpPending,
    string PendingReason)
{
    public static UserActionsState Initial { get; } = new(Array.Empty<PlatformUserAdminAction>(), null, null, null, false, string.Empty);
}
