namespace Vixly.Operations.Application.UseCases.Billing.DecideOperationsCredit;

// =========================================================
// FILE: DecideOperationsCreditRequest.cs
//
// PURPOSE:
// Approve or refuse a credit note or a void (Slice 5F, F-6, I-6 and I-7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class DecideOperationsCreditRequest
{
    public Guid CreditNoteId { get; init; }

    public bool Approve { get; init; }

    public string? Note { get; init; }
}
