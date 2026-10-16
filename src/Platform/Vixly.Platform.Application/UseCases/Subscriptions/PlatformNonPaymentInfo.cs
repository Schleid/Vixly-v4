using Vixly.Platform.Application.Queries;

namespace Vixly.Platform.Application.UseCases.Subscriptions;

// =========================================================
// FILE: PlatformNonPaymentInfo.cs
//
// PURPOSE:
// Why a tenant must pay, for the Operations "payment required" page (Slice 6, D-C4).
//
// RESPONSIBILITIES:
// - Carry when the block started, the amount due per currency and whether a pay link can be offered
//
// IMPORTANT NOTES:
// - Set while unpaid (Pending payment or Blocked for non-payment)
// - Customer-facing features ignore it entirely (owner, 2026-10-16)
// =========================================================
public sealed record PlatformNonPaymentInfo(DateTimeOffset? BlockedUtc, IReadOnlyList<PlatformMoneyItem> AmountDue, bool CardPaymentsAvailable);
