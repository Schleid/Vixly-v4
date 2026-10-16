using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Billing;

// =========================================================
// FILE: InvoiceState.cs
//
// PURPOSE:
// What the invoice page shows.
//
// RESPONSIBILITIES:
// - Hold the invoice, what the signed-in user may do with it, and the load outcome
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record InvoiceState(PlatformInvoiceDetail? Invoice, PlatformInvoiceAbilities Abilities, bool Loaded, int Version, Error? Error)
{
    public static InvoiceState Initial { get; } = new(null, PlatformInvoiceAbilities.None, false, 0, null);
}
