using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Audit.ListAuditEntries;

// =========================================================
// FILE: ListAuditEntriesResponse.cs
//
// PURPOSE:
// Result of ListAuditEntries.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListAuditEntriesResponse
{
    private ListAuditEntriesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformAuditListItem>? Entries { get; private init; }

    public static ListAuditEntriesResponse Success(PagedResult<PlatformAuditListItem>? entries)
    {
        return new ListAuditEntriesResponse { Succeeded = true, Entries = entries };
    }

    public static ListAuditEntriesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListAuditEntriesResponse { Error = error };
    }
}
