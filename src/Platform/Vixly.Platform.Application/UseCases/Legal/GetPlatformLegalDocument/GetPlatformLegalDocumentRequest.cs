namespace Vixly.Platform.Application.UseCases.Legal.GetPlatformLegalDocument;

// =========================================================
// FILE: GetPlatformLegalDocumentRequest.cs
//
// PURPOSE:
// Reads a legal document version with its files, included versions and the reader's actions.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformLegalDocumentRequest
{
    public Guid DocumentId { get; init; }
}
