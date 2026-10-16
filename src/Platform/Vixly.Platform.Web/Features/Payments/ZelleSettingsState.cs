using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Payments;

// =========================================================
// FILE: ZelleSettingsState.cs
//
// PURPOSE:
// What the Zelle part of the payments settings page shows.
//
// RESPONSIBILITIES:
// - Hold the Zelle details, the QR image, the last result and a waiting step-up
//
// IMPORTANT NOTES:
// - Immutable; a new state per change; Version changes on every load so the form refills
// =========================================================
public sealed record ZelleSettingsState(PlatformZelleAccountItem? Account, PlatformZelleImage? QrCode, bool Loaded, int Version, string? DoneKey, bool StepUpPending, Error? Error)
{
    public static ZelleSettingsState Initial { get; } = new(null, null, false, 0, null, false, null);
}
