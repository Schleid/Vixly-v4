using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Billing;

// =========================================================
// FILE: BillingSettingsState.cs
//
// PURPOSE:
// What the Billing settings page shows.
//
// RESPONSIBILITIES:
// - Hold the settings and profile, whether they can be changed, step-up and messages
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record BillingSettingsState(PlatformBillingSettingsItem? Settings, bool CanEdit, bool Loaded, int Version, bool StepUpPending, string? DoneKey, Error? Error)
{
    public static BillingSettingsState Initial { get; } = new(null, false, false, 0, false, null, null);
}
