namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformAgeingItem.cs
//
// PURPOSE:
// Overdue money in one ageing bucket and currency (D-C13).
//
// RESPONSIBILITIES:
// - Carry bucket (1: 1-30, 2: 31-60, 3: 61-90, 4: over 90 days), currency and amount
//
// IMPORTANT NOTES:
// - Never added across currencies
// =========================================================
public sealed record PlatformAgeingItem(int Bucket, string CurrencyCode, decimal Amount);
