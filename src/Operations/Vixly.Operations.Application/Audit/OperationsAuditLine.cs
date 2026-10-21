namespace Vixly.Operations.Application.Audit;

// =========================================================
// FILE: OperationsAuditLine.cs
//
// PURPOSE:
// One audit entry as the screen shows it (Slice 1E).
//
// RESPONSIBILITIES:
// - Carry the entry with the name of who did it
//
// IMPORTANT NOTES:
// - Read-only
// =========================================================
public sealed record OperationsAuditLine(Guid EntryId, DateTimeOffset OccurredUtc, Guid? ActorUserId, string? ActorName, string ActionCode, string TargetType, Guid? TargetId, string? Reason, string? IpAddress, string? Details);
