namespace Vixly.Contracts.Platform.Common;

// =========================================================
// FILE: PlatformPageInfo.cs
//
// PURPOSE:
// One page of a list.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Page starts at 1
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformPageInfo<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
