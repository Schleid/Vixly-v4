using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: SalesSettingsState.cs
//
// PURPOSE:
// What the Sales settings page shows.
//
// RESPONSIBILITIES:
// - Hold the settings, whether they can be changed, step-up and messages
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record SalesSettingsState(PlatformSalesSettingsItem? Settings, bool CanEdit, bool Loaded, int Version, bool StepUpPending, string? DoneKey, Error? Error)
{
    public static SalesSettingsState Initial { get; } = new(null, false, false, 0, false, null, null);
}
