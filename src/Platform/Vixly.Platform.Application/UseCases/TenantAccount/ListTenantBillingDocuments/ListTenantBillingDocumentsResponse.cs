using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.TenantAccount.ListTenantBillingDocuments;

// =========================================================
// FILE: ListTenantBillingDocumentsResponse.cs
//
// PURPOSE:
// The tenant's invoice, credit note and receipt PDFs, newest first (call 22).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListTenantBillingDocumentsResponse
{
    private ListTenantBillingDocumentsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformBillingDocumentItem>? Documents { get; private init; }

    public static ListTenantBillingDocumentsResponse Success(PagedResult<PlatformBillingDocumentItem>? documents)
    {
        return new ListTenantBillingDocumentsResponse { Succeeded = true, Documents = documents };
    }

    public static ListTenantBillingDocumentsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListTenantBillingDocumentsResponse { Error = error };
    }
}
