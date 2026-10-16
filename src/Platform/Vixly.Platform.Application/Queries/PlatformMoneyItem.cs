namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformMoneyItem.cs
//
// PURPOSE:
// An amount in one currency.
//
// RESPONSIBILITIES:
// - Carry an amount with its currency for lists and totals
//
// IMPORTANT NOTES:
// - Never added across currencies
// =========================================================
public sealed record PlatformMoneyItem(string CurrencyCode, decimal Amount);
