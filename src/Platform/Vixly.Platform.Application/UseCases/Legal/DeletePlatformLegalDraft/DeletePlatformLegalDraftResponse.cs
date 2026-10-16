using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Legal.DeletePlatformLegalDraft;

// =========================================================
// FILE: DeletePlatformLegalDraftResponse.cs
//
// PURPOSE:
// Deletes a draft and its files; published versions are never deleted.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DeletePlatformLegalDraftResponse
{
    private DeletePlatformLegalDraftResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DeletePlatformLegalDraftResponse Success()
    {
        return new DeletePlatformLegalDraftResponse { Succeeded = true };
    }

    public static DeletePlatformLegalDraftResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DeletePlatformLegalDraftResponse { Error = error };
    }
}
