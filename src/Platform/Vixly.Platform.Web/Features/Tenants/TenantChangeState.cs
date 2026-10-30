using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantChangeState.cs
//
// PURPOSE:
// State of one company change request screen (B12).
//
// RESPONSIBILITIES:
// - Hold the request, the decide right and the last outcome
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record TenantChangeState(PlatformTenantChangeItem? Change, bool CanDecide, bool Loaded, string? DoneKey, bool StepUpPending, Error? Error)
{
    public static TenantChangeState Initial { get; } = new(null, false, false, null, false, null);
}
