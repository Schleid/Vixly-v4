using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Application.UseCases.Site.RemoveOperationsSiteImage;

// =========================================================
// FILE: RemoveOperationsSiteImageRequest.cs
//
// PURPOSE:
// Removes a logo, favicon or hero image from the draft site (Slice 1B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RemoveOperationsSiteImageRequest
{
    public OperationsSiteImageKind Kind { get; init; }
}
