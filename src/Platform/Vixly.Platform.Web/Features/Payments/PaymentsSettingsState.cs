using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Payments;

// =========================================================
// FILE: PaymentsSettingsState.cs
//
// PURPOSE:
// What the Payments settings page shows.
//
// RESPONSIBILITIES:
// - Hold the merchant account, whether it can be changed, step-up and messages
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record PaymentsSettingsState(PlatformMerchantAccountItem? Account, bool CanEdit, bool Loaded, int Version, bool StepUpPending, string? DoneKey, Error? Error)
{
    public static PaymentsSettingsState Initial { get; } = new(null, false, false, 0, false, null, null);
}
