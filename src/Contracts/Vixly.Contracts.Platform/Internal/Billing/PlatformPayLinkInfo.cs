namespace Vixly.Contracts.Platform.Internal.Billing;

// =========================================================
// FILE: PlatformPayLinkInfo.cs
//
// PURPOSE:
// A payment link for an open invoice (call 23).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Show or send the link to the tenant user; never log it
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformPayLinkInfo(string Url, DateTimeOffset ExpiresUtc);
