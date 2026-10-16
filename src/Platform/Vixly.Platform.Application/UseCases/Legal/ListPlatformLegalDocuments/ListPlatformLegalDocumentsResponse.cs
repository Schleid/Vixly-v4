using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Legal;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Legal.ListPlatformLegalDocuments;

// =========================================================
// FILE: ListPlatformLegalDocumentsResponse.cs
//
// PURPOSE:
// Lists legal document versions, newest first, optionally of one kind.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformLegalDocumentsResponse
{
    private ListPlatformLegalDocumentsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformLegalDocumentItem>? Documents { get; private init; }

    public PlatformLegalAbilities Abilities { get; private init; } = PlatformLegalAbilities.None;

    public static ListPlatformLegalDocumentsResponse Success(PagedResult<PlatformLegalDocumentItem>? documents, PlatformLegalAbilities abilities)
    {
        return new ListPlatformLegalDocumentsResponse { Succeeded = true, Documents = documents, Abilities = abilities };
    }

    public static ListPlatformLegalDocumentsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformLegalDocumentsResponse { Error = error };
    }
}
