using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformCardPaymentFailed.cs
//
// PURPOSE:
// A card charge failed or needs the cardholder (D-P13, D-P14).
//
// RESPONSIBILITIES:
// - Tell the tenant (email with a pay link) and Slice 6 (retries and dunning)
//
// IMPORTANT NOTES:
// - One message per failed attempt; Slice 5 never retries by itself
// =========================================================
public sealed record PlatformCardPaymentFailed(
    Guid PlatformInvoiceId,
    Guid PlatformTenantId,
    Guid TransactionId,
    PlatformCardTransactionOrigin Origin,
    string? FailureCode,
    bool ActionRequired,
    bool Retryable,
    DateTimeOffset OccurredUtc);
