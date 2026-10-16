using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Payments;

// =========================================================
// FILE: ZelleClaimState.cs
//
// PURPOSE:
// What the Zelle report page shows.
//
// RESPONSIBILITIES:
// - Hold the report, the last result and a waiting step-up action
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record ZelleClaimState(PlatformZelleClaimDetail? Detail, bool Loaded, string? DoneKey, bool StepUpPending, Error? Error)
{
    public static ZelleClaimState Initial { get; } = new(null, false, null, false, null);
}
