using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Collections.ListPlatformCollectionCases;

// =========================================================
// FILE: ListPlatformCollectionCasesResponse.cs
//
// PURPOSE:
// Lists collections cases, paged, with stage, status, currency and extension filters (D-C14).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformCollectionCasesResponse
{
    private ListPlatformCollectionCasesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformCollectionCaseItem>? Cases { get; private init; }

    public static ListPlatformCollectionCasesResponse Success(PagedResult<PlatformCollectionCaseItem>? cases)
    {
        return new ListPlatformCollectionCasesResponse { Succeeded = true, Cases = cases };
    }

    public static ListPlatformCollectionCasesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformCollectionCasesResponse { Error = error };
    }
}
