using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Registrations;

// =========================================================
// FILE: RegistrationSummaryState.cs
//
// PURPOSE:
// State of the registration page.
//
// RESPONSIBILITIES:
// - Hold the registration, whether the actor may manage it, the
//   cancel form and the outcome
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record RegistrationSummaryState(
    PlatformTenantRegistrationDetail? Registration,
    bool CanManage,
    bool ConfirmingCancel,
    string? DoneKey,
    Error? Error)
{
    public static RegistrationSummaryState Initial { get; } = new(null, false, false, null, null);
}
