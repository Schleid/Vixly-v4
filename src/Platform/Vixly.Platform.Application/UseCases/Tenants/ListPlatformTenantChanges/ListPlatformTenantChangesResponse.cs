using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Tenants.ListPlatformTenantChanges;

// =========================================================
// FILE: ListPlatformTenantChangesResponse.cs
//
// PURPOSE:
// A page of company change requests (B12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformTenantChangesResponse
{
    private ListPlatformTenantChangesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformTenantChangeItem>? Changes { get; private init; }

    public static ListPlatformTenantChangesResponse Success(PagedResult<PlatformTenantChangeItem>? changes)
    {
        return new ListPlatformTenantChangesResponse { Succeeded = true, Changes = changes };
    }

    public static ListPlatformTenantChangesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new ListPlatformTenantChangesResponse { Error = error };
    }
}
