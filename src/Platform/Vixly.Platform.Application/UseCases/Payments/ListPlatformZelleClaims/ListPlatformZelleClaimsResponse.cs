using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Payments.ListPlatformZelleClaims;

// =========================================================
// FILE: ListPlatformZelleClaimsResponse.cs
//
// PURPOSE:
// Lists Zelle reports, paged; the waiting ones are staff's to-do list.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformZelleClaimsResponse
{
    private ListPlatformZelleClaimsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformZelleClaimItem>? Claims { get; private init; }

    public static ListPlatformZelleClaimsResponse Success(PagedResult<PlatformZelleClaimItem>? claims)
    {
        return new ListPlatformZelleClaimsResponse { Succeeded = true, Claims = claims };
    }

    public static ListPlatformZelleClaimsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformZelleClaimsResponse { Error = error };
    }
}
