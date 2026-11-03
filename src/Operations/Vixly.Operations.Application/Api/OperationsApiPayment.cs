namespace Vixly.Operations.Application.Api;

// =========================================================
// FILE: OperationsApiPayment.cs
//
// PURPOSE:
// A payment as the company API shows it (Slice 10).
//
// RESPONSIBILITIES:
// - Carry what the company API and its webhooks show
//
// IMPORTANT NOTES:
// - Ids, codes and figures only: never a password, a protected value or staff notes
// - A field is only ever added, never renamed or removed (receivers depend on it)
// =========================================================
public sealed record OperationsApiPayment(
    Guid Id,
    string Number,
    Guid InvoiceId,
    Guid CustomerId,
    string CurrencyCode,
    decimal Total,
    bool Pending,
    IReadOnlyList<string> Methods,
    DateTimeOffset TakenUtc,
    DateTimeOffset CreatedUtc);
