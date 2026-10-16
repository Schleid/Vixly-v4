using System.Text.Json;

namespace Vixly.Contracts.Platform.Events;

// =========================================================
// FILE: PlatformEventInfo.cs
//
// PURPOSE:
// One event in the feed.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Data is one of the ...Event records of this folder, chosen by Type
// - Sequence grows; read again after the last one seen
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformEventInfo(long Sequence, string Type, Guid? TenantId, DateTimeOffset OccurredUtc, JsonElement Data);
