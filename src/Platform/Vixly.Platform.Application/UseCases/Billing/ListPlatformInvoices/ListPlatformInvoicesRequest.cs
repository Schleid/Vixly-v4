using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.UseCases.Billing.ListPlatformInvoices;

// =========================================================
// FILE: ListPlatformInvoicesRequest.cs
//
// PURPOSE:
// Lists invoices, paged, with status, overdue, tenant, number or company and date filters (D-B3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListPlatformInvoicesRequest
{
    public PlatformInvoiceStatus? Status { get; init; }

    public bool OverdueOnly { get; init; }

    public Guid? PlatformTenantId { get; init; }

    public string? Search { get; init; }

    public DateTimeOffset? IssuedFromUtc { get; init; }

    public DateTimeOffset? IssuedToUtc { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
