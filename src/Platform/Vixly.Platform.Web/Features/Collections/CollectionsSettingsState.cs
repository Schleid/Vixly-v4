using Vixly.Platform.Application.UseCases.Collections;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Collections;

// =========================================================
// FILE: CollectionsSettingsState.cs
//
// PURPOSE:
// What the Collections settings page shows.
//
// RESPONSIBILITIES:
// - Hold the settings, step-up and messages
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record CollectionsSettingsState(PlatformCollectionsSettingsItem? Settings, bool Loaded, int Version, bool StepUpPending, string? DoneKey, Error? Error)
{
    public static CollectionsSettingsState Initial { get; } = new(null, false, 0, false, null, null);
}
