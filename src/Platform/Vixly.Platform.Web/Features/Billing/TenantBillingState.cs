using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Billing;

// =========================================================
// FILE: TenantBillingState.cs
//
// PURPOSE:
// What the Billing section of the tenant page shows.
//
// RESPONSIBILITIES:
// - Hold balances, tax details, step-up and messages
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record TenantBillingState(PlatformTenantBillingDetail? Billing, bool Loaded, int Version, bool StepUpPending, string? DoneKey, Error? Error)
{
    public static TenantBillingState Initial { get; } = new(null, false, 0, false, null, null);
}
