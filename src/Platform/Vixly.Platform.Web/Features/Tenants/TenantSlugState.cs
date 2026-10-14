using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantSlugState.cs
//
// PURPOSE:
// State of the portal address correction.
//
// RESPONSIBILITIES:
// - Hold whether the form is open, the step-up prompt and outcome
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record TenantSlugState(bool Editing, bool StepUpPending, string? DoneKey, Error? Error)
{
    public static TenantSlugState Initial { get; } = new(false, false, null, null);
}
