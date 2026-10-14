using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Tenants.ListPlatformTenants;

// =========================================================
// FILE: ListPlatformTenantsResponse.cs
//
// PURPOSE:
// Result of ListPlatformTenants.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformTenantsResponse
{
    private ListPlatformTenantsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformTenantListItem>? Tenants { get; private init; }

    public bool CanCreate { get; private init; }

    public static ListPlatformTenantsResponse Success(PagedResult<PlatformTenantListItem> tenants, bool canCreate)
    {
        return new ListPlatformTenantsResponse { Succeeded = true, Tenants = tenants, CanCreate = canCreate };
    }

    public static ListPlatformTenantsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformTenantsResponse { Error = error };
    }
}
