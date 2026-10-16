using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformCardTransactionItem.cs
//
// PURPOSE:
// One card transaction as listed.
//
// RESPONSIBILITIES:
// - Carry what the console shows
//
// IMPORTANT NOTES:
// - Plain record; no card data beyond what the domain keeps
// =========================================================
public sealed record PlatformCardTransactionItem(
    Guid TransactionId,
    Guid PlatformTenantId,
    string TenantName,
    Guid PlatformInvoiceId,
    string InvoiceNumber,
    PlatformCardTransactionKind Kind,
    PlatformCardTransactionOrigin Origin,
    PlatformCardTransactionStatus Status,
    decimal Amount,
    string CurrencyCode,
    string? FailureCode,
    PlatformDisputeStatus? Dispute,
    string? ProviderReference,
    DateTimeOffset CreatedUtc,
    DateTimeOffset? CompletedUtc,
    string? CreatedByName);
