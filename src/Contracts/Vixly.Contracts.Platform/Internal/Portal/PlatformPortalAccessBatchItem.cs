namespace Vixly.Contracts.Platform.Internal.Portal;

// =========================================================
// FILE: PlatformPortalAccessBatchItem.cs
//
// PURPOSE:
// One address of a batch and its answer.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformPortalAccessBatchItem(string Slug, PlatformPortalAccessInfo Access);
