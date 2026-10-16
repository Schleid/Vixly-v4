using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Billing.ListPlatformInvoices;

// =========================================================
// FILE: ListPlatformInvoicesResponse.cs
//
// PURPOSE:
// Lists invoices, paged, with status, overdue, tenant, number or company and date filters (D-B3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformInvoicesResponse
{
    private ListPlatformInvoicesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformInvoiceListItem>? Invoices { get; private init; }

    public PlatformInvoiceAbilities Abilities { get; private init; } = PlatformInvoiceAbilities.None;

    public static ListPlatformInvoicesResponse Success(PagedResult<PlatformInvoiceListItem>? invoices, PlatformInvoiceAbilities abilities)
    {
        return new ListPlatformInvoicesResponse { Succeeded = true, Invoices = invoices, Abilities = abilities };
    }

    public static ListPlatformInvoicesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformInvoicesResponse { Error = error };
    }
}
