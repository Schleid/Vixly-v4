namespace Vixly.Contracts.Platform.Common;

// =========================================================
// FILE: PlatformApiError.cs
//
// PURPOSE:
// An error on the wire: a stable code, its arguments, text in the caller's language and a correlation id (D-K7).
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and its callers
//
// IMPORTANT NOTES:
// - The code never changes; the message follows Accept-Language (en, fr, es)
// - CorrelationId matches the X-Correlation-Id header and the Platform logs
// - Moved from Registration in Slice 9 (ADR-0013)
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformApiError(string Code, string Message, IReadOnlyDictionary<string, string>? Arguments = null, string? CorrelationId = null);
