namespace Vixly.Platform.Application.UseCases.Legal.PublishPlatformLegalDocument;

// =========================================================
// FILE: PublishPlatformLegalDocumentRequest.cs
//
// PURPOSE:
// Publishes a draft: it becomes the current version of its kind, never changed again (D-Lg3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class PublishPlatformLegalDocumentRequest
{
    public Guid DocumentId { get; init; }
}
