using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Leads.ListPlatformSalesLeads;

// =========================================================
// FILE: ListPlatformSalesLeadsResponse.cs
//
// PURPOSE:
// Contact requests for sales staff, newest first (Stage 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformSalesLeadsResponse
{
    private ListPlatformSalesLeadsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformSalesLeadItem>? Leads { get; private init; }

    public static ListPlatformSalesLeadsResponse Success(PagedResult<PlatformSalesLeadItem>? leads)
    {
        return new ListPlatformSalesLeadsResponse { Succeeded = true, Leads = leads };
    }

    public static ListPlatformSalesLeadsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformSalesLeadsResponse { Error = error };
    }
}
