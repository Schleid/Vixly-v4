using Vixly.Operations.Domain.Billing;

namespace Vixly.Operations.Application.Billing;

// =========================================================
// FILE: OperationsCreditRequestRow.cs
//
// PURPOSE:
// One correction waiting for a decision (Slice 5F, I-6).
//
// RESPONSIBILITIES:
// - Carry what the approver sees
//
// IMPORTANT NOTES:
// - Never decided by the person who asked
// =========================================================
public sealed record OperationsCreditRequestRow(
    Guid CreditNoteId,
    Guid InvoiceId,
    string InvoiceNumber,
    string Customer,
    OperationsCreditNoteKind Kind,
    decimal Total,
    string CurrencyCode,
    string Reason,
    string RequestedBy,
    DateTimeOffset RequestedUtc,
    bool Own);
