using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Audit.ListPlatformSignIns;

// =========================================================
// FILE: ListPlatformSignInsResponse.cs
//
// PURPOSE:
// Result of ListPlatformSignIns.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformSignInsResponse
{
    private ListPlatformSignInsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformSignInItem>? SignIns { get; private init; }

    public static ListPlatformSignInsResponse Success(PagedResult<PlatformSignInItem>? signIns)
    {
        return new ListPlatformSignInsResponse { Succeeded = true, SignIns = signIns };
    }

    public static ListPlatformSignInsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformSignInsResponse { Error = error };
    }
}
