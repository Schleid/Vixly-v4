namespace Vixly.Platform.Application.UseCases.Legal.GetPublicLegalFile;

// =========================================================
// FILE: GetPublicLegalFileRequest.cs
//
// PURPOSE:
// Downloads a published legal document in one language (public pages, owners).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPublicLegalFileRequest
{
    public Guid DocumentId { get; init; }

    public string? Language { get; init; }
}
