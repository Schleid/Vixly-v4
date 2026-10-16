namespace Vixly.Contracts.Platform.Internal.Entitlements;

// =========================================================
// FILE: PlatformNonPaymentPartInfo.cs
//
// PURPOSE:
// Why access is limited for non-payment.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformNonPaymentPartInfo(DateTimeOffset? BlockedUtc, IReadOnlyList<PlatformMoneyInfo> AmountDue, bool CardPaymentsAvailable);
