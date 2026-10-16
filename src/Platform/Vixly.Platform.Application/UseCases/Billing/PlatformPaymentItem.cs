using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.UseCases.Billing;

// =========================================================
// FILE: PlatformPaymentItem.cs
//
// PURPOSE:
// One payment as shown.
//
// RESPONSIBILITIES:
// - Carry what the console shows
//
// IMPORTANT NOTES:
// - Plain record for the console
// =========================================================
public sealed record PlatformPaymentItem(
    Guid PaymentId,
    string ReceiptNumber,
    decimal Amount,
    PlatformPaymentMethod Method,
    DateTimeOffset ReceivedUtc,
    string? Reference,
    string? Note,
    string? RecordedByName,
    PlatformPaymentStatus Status,
    string? ReversalReason,
    string? ReversedByName);
