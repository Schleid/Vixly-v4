namespace Vixly.Platform.Application.UseCases.Legal.DeletePlatformLegalDraft;

// =========================================================
// FILE: DeletePlatformLegalDraftRequest.cs
//
// PURPOSE:
// Deletes a draft and its files; published versions are never deleted.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class DeletePlatformLegalDraftRequest
{
    public Guid DocumentId { get; init; }
}
