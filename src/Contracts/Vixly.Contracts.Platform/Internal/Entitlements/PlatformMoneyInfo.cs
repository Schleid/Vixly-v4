namespace Vixly.Contracts.Platform.Internal.Entitlements;

// =========================================================
// FILE: PlatformMoneyInfo.cs
//
// PURPOSE:
// An amount in one currency.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformMoneyInfo(string CurrencyCode, decimal Amount);
