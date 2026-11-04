namespace Vixly.Operations.Application.Audit;

// =========================================================
// FILE: OperationsAuditFilter.cs
//
// PURPOSE:
// What the audit log screen filters on (Slice 1E).
//
// RESPONSIBILITIES:
// - Carry the person, area and dates
//
// IMPORTANT NOTES:
// - Area is the second part of the action code (ops.<area>.<action>)
// =========================================================
public sealed record OperationsAuditFilter(Guid? ActorUserId, string? Area, DateTimeOffset? FromUtc, DateTimeOffset? ToUtc);
