namespace Vixly.Operations.Application.UseCases.Company.RemoveOperationsDocumentLogo;

// =========================================================
// FILE: RemoveOperationsDocumentLogoRequest.cs
//
// PURPOSE:
// Remove the document logo; documents show the company name again (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Carry the input of the use case
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RemoveOperationsDocumentLogoRequest
{
    public bool Confirmed { get; init; }
}
