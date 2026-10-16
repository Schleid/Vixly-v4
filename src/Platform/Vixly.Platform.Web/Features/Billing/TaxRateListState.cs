using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Billing;

// =========================================================
// FILE: TaxRateListState.cs
//
// PURPOSE:
// What the Tax rates page shows.
//
// RESPONSIBILITIES:
// - Hold the rates, whether they can be changed, step-up and messages
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record TaxRateListState(IReadOnlyList<PlatformTaxRateItem> Rates, bool CanEdit, bool Loaded, bool StepUpPending, string? DoneKey, Error? Error)
{
    public static TaxRateListState Initial { get; } = new(Array.Empty<PlatformTaxRateItem>(), false, false, false, null, null);
}
