namespace Vixly.Contracts.Platform.Events;

// =========================================================
// FILE: PlatformEventPageInfo.cs
//
// PURPOSE:
// A page of the event feed.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Next is the cursor for the next call (after=Next); unchanged when the page is empty
// - More: call again at once
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformEventPageInfo(IReadOnlyList<PlatformEventInfo> Items, long Next, bool More);
