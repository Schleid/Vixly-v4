namespace Vixly.Operations.Application.UseCases.Company.CreateOperationsPayLink;

// =========================================================
// FILE: CreateOperationsPayLinkRequest.cs
//
// PURPOSE:
// Asks Vixly for a link to pay one invoice online (Slice 1E).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CreateOperationsPayLinkRequest
{
    public Guid InvoiceId { get; init; }
}
