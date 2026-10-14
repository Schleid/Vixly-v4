using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Sales;

// =========================================================
// FILE: SalesActionState.cs
//
// PURPOSE:
// The state of an action panel: step-up, done message, error and a link shown once.
//
// RESPONSIBILITIES:
// - Hold what the component shows after an action
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record SalesActionState(bool StepUpPending, string? DoneKey, Error? Error, string? Link)
{
    public static SalesActionState Initial { get; } = new(false, null, null, null);
}
