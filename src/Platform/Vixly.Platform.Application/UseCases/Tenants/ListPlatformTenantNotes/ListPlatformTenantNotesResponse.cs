using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Tenants.ListPlatformTenantNotes;

// =========================================================
// FILE: ListPlatformTenantNotesResponse.cs
//
// PURPOSE:
// Result of ListPlatformTenantNotes.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformTenantNotesResponse
{
    private ListPlatformTenantNotesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformTenantNoteItem>? Notes { get; private init; }

    public static ListPlatformTenantNotesResponse Success(PagedResult<PlatformTenantNoteItem> notes)
    {
        return new ListPlatformTenantNotesResponse { Succeeded = true, Notes = notes };
    }

    public static ListPlatformTenantNotesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformTenantNotesResponse { Error = error };
    }
}
