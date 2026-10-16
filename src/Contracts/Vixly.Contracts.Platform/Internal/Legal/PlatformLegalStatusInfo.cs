using Vixly.Contracts.Platform.Legal;

namespace Vixly.Contracts.Platform.Internal.Legal;

// =========================================================
// FILE: PlatformLegalStatusInfo.cs
//
// PURPOSE:
// The tenant's Terms status and the Terms to show (call 12).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Terms FileUrl values are relative to the internal base address
// - Cache 5 minutes; drop on legal events
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformLegalStatusInfo(string Status, DateTimeOffset? DeadlineUtc, bool ReadOnly, PlatformLegalTermsInfo? Terms);
