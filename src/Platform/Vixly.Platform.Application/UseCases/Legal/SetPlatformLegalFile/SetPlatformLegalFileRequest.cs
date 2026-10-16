namespace Vixly.Platform.Application.UseCases.Legal.SetPlatformLegalFile;

// =========================================================
// FILE: SetPlatformLegalFileRequest.cs
//
// PURPOSE:
// Uploads or replaces the PDF of a draft in one language, with its summary.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SetPlatformLegalFileRequest
{
    public Guid DocumentId { get; init; }

    public string? Language { get; init; }

    public byte[]? Content { get; init; }

    public string? Summary { get; init; }
}
