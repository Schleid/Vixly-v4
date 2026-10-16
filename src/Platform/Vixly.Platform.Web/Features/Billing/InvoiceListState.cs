using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Billing;

// =========================================================
// FILE: InvoiceListState.cs
//
// PURPOSE:
// State of the invoices list.
//
// RESPONSIBILITIES:
// - Hold one page of invoices
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record InvoiceListState(
    IReadOnlyList<PlatformInvoiceListItem> Invoices,
    int Page,
    int TotalPages,
    int TotalCount,
    bool Loaded,
    Error? Error)
{
    public static InvoiceListState Initial { get; } = new(Array.Empty<PlatformInvoiceListItem>(), 1, 0, 0, false, null);
}
