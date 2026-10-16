using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.TenantAccount.ListTenantInvoices;

// =========================================================
// FILE: ListTenantInvoicesResponse.cs
//
// PURPOSE:
// The tenant's invoices, newest first (call 22).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListTenantInvoicesResponse
{
    private ListTenantInvoicesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformInvoiceListItem>? Invoices { get; private init; }

    public static ListTenantInvoicesResponse Success(PagedResult<PlatformInvoiceListItem>? invoices)
    {
        return new ListTenantInvoicesResponse { Succeeded = true, Invoices = invoices };
    }

    public static ListTenantInvoicesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListTenantInvoicesResponse { Error = error };
    }
}
