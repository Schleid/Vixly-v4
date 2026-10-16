using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformZelleClaimItem.cs
//
// PURPOSE:
// A Zelle report as the console lists it.
//
// RESPONSIBILITIES:
// - Carry the report, its invoice and the decision
//
// IMPORTANT NOTES:
// - Read model; no domain object leaves the query
// =========================================================
public sealed record PlatformZelleClaimItem(
    Guid ClaimId,
    Guid PlatformTenantId,
    string TenantName,
    Guid PlatformInvoiceId,
    string InvoiceNumber,
    string CurrencyCode,
    decimal Amount,
    decimal InvoiceBalance,
    DateTime SentOn,
    string SenderName,
    string Reference,
    PlatformZelleClaimSource Source,
    PlatformZelleClaimStatus Status,
    DateTimeOffset ReportedUtc,
    decimal? ReceivedAmount,
    DateTimeOffset? DecidedUtc,
    string? DecidedByName,
    string? RejectReason,
    string? StaffNote);
