using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Legal.CreatePlatformLegalDraft;

// =========================================================
// FILE: CreatePlatformLegalDraftResponse.cs
//
// PURPOSE:
// Starts a new version of a legal document as a draft.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CreatePlatformLegalDraftResponse
{
    private CreatePlatformLegalDraftResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid DocumentId { get; private init; }

    public static CreatePlatformLegalDraftResponse Success(Guid documentId)
    {
        return new CreatePlatformLegalDraftResponse { Succeeded = true, DocumentId = documentId };
    }

    public static CreatePlatformLegalDraftResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CreatePlatformLegalDraftResponse { Error = error };
    }
}
