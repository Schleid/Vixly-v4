using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Licensing;

// =========================================================
// FILE: DeviceState.cs
//
// PURPOSE:
// What the device page shows.
//
// RESPONSIBILITIES:
// - Hold the device, its history, the last result and a waiting step-up action
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record DeviceState(PlatformDeviceDetail? Detail, bool Loaded, string? DoneKey, string? StepUpAction, Error? Error)
{
    public static DeviceState Initial { get; } = new(null, false, null, null, null);
}
