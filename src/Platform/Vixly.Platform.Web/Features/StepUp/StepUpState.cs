using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.StepUp;

// =========================================================
// FILE: StepUpState.cs
//
// PURPOSE:
// What the step-up dialog shows.
//
// RESPONSIBILITIES:
// - Carry the chosen method, the error and whether the session
//   ended
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record StepUpState(bool UseRecoveryCode, Error? Error, bool SessionEnded)
{
    public static StepUpState Initial { get; } = new(false, null, false);
}
