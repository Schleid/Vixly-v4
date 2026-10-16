using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Payments;

// =========================================================
// FILE: TenantCardsState.cs
//
// PURPOSE:
// What the Saved cards part of the tenant Billing section shows.
//
// RESPONSIBILITIES:
// - Hold the cards, consent, step-up and messages
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record TenantCardsState(PlatformTenantCardsItem? Cards, bool Loaded, bool StepUpPending, string? DoneKey, Error? Error)
{
    public static TenantCardsState Initial { get; } = new(null, false, false, null, null);
}
