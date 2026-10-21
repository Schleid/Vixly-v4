using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Application.UseCases.Site.UploadOperationsSiteImage;

// =========================================================
// FILE: UploadOperationsSiteImageRequest.cs
//
// PURPOSE:
// Adds a logo, favicon or hero image to the draft site (Slice 1B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class UploadOperationsSiteImageRequest
{
    public OperationsSiteImageKind Kind { get; init; }

    public byte[] Content { get; init; } = Array.Empty<byte>();
}
