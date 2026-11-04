namespace Vixly.Operations.Application.UseCases.Company.SaveOperationsDocumentLogo;

// =========================================================
// FILE: SaveOperationsDocumentLogoRequest.cs
//
// PURPOSE:
// A new document logo: the PNG as uploaded (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Carry the input of the use case
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsDocumentLogoRequest
{
    public byte[] Content { get; init; } = Array.Empty<byte>();
}
