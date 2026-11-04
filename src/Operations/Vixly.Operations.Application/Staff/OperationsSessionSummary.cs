namespace Vixly.Operations.Application.Staff;

// =========================================================
// FILE: OperationsSessionSummary.cs
//
// PURPOSE:
// One of my sessions (Slice 1D, My account).
//
// RESPONSIBILITIES:
// - Carry when, from where and whether it is this one
//
// IMPORTANT NOTES:
// - Read model
// =========================================================
public sealed record OperationsSessionSummary(Guid SessionId, DateTimeOffset StartedUtc, DateTimeOffset LastSeenUtc, string? IpAddress, string? UserAgent, bool IsCurrent);
