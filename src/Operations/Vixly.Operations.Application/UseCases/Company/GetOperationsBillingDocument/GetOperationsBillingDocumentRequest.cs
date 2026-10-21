namespace Vixly.Operations.Application.UseCases.Company.GetOperationsBillingDocument;

// =========================================================
// FILE: GetOperationsBillingDocumentRequest.cs
//
// PURPOSE:
// Fetches one billing PDF for the Owner or an Administrator (Slice 1E).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsBillingDocumentRequest
{
    public string Kind { get; init; } = string.Empty;

    public Guid DocumentId { get; init; }
}
