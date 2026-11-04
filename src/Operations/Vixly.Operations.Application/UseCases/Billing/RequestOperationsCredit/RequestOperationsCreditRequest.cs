namespace Vixly.Operations.Application.UseCases.Billing.RequestOperationsCredit;

// =========================================================
// FILE: RequestOperationsCreditRequest.cs
//
// PURPOSE:
// Ask for a credit note or a void of an invoice, with a reason (Slice 5F, F-6 and I-6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RequestOperationsCreditRequest
{
    public Guid InvoiceId { get; init; }

    // True: the whole invoice, while nothing is paid or credited.
    public bool Void { get; init; }

    // The amount before tax to take back from each invoice line.
    public IReadOnlyDictionary<Guid, decimal> Amounts { get; init; } = new Dictionary<Guid, decimal>();

    public string? Reason { get; init; }

    // The form's key (ADR-0019): sent twice, one request.
    public Guid RequestKey { get; init; }
}
