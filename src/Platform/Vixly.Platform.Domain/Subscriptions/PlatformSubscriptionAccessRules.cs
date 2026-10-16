namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionAccessRules.cs
//
// PURPOSE:
// How long an unpaid subscription stays read-only before payment is required (D-S14, D-C4, D-C5).
//
// RESPONSIBILITIES:
// - Carry the two periods from Collections settings to the access check
//
// IMPORTANT NOTES:
// - BlockedReadOnly null: a blocked subscription stays read-only until paid
// =========================================================
public sealed record PlatformSubscriptionAccessRules(TimeSpan UnpaidReadOnly, TimeSpan? BlockedReadOnly);
