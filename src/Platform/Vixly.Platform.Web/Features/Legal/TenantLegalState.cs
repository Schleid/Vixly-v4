using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Legal;

// =========================================================
// FILE: TenantLegalState.cs
//
// PURPOSE:
// What the Legal section of the tenant page shows.
//
// RESPONSIBILITIES:
// - Hold the status, acceptances, reminders, the last result and a waiting step-up
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record TenantLegalState(PlatformTenantLegalDetail? Detail, bool Loaded, string? DoneKey, bool StepUpPending, Error? Error)
{
    public static TenantLegalState Initial { get; } = new(null, false, null, false, null);
}
