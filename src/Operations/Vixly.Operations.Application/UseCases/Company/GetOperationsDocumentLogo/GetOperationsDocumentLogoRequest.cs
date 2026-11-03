namespace Vixly.Operations.Application.UseCases.Company.GetOperationsDocumentLogo;

// =========================================================
// FILE: GetOperationsDocumentLogoRequest.cs
//
// PURPOSE:
// Which copy of the document logo to read: the logo, or its black and white label copy (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Carry the input of the use case
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsDocumentLogoRequest
{
    public bool Label { get; init; }
}
