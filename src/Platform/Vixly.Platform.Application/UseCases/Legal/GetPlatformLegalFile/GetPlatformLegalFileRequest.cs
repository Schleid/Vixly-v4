namespace Vixly.Platform.Application.UseCases.Legal.GetPlatformLegalFile;

// =========================================================
// FILE: GetPlatformLegalFileRequest.cs
//
// PURPOSE:
// Downloads one language file of a legal document for staff.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformLegalFileRequest
{
    public Guid DocumentId { get; init; }

    public string? Language { get; init; }
}
