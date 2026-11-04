using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Tracking;

// =========================================================
// FILE: OperationsTrackingLine.cs
//
// PURPOSE:
// One line of a tracking history as a reader may see it (ADR-0021 decision 5).
//
// RESPONSIBILITIES:
// - Carry what the reader is allowed to see
//
// IMPORTANT NOTES:
// - Customer lines carry no actor, device, note or internal event; public lines only a milestone and a time
// =========================================================
public sealed record OperationsTrackingLine(
    Guid EventId,
    string Code,
    DateTimeOffset OccurredUtc,
    string? TimeZone,
    string? LocationCode,
    IReadOnlyDictionary<string, string> Arguments,
    OperationsMilestone? Milestone,
    bool Voided,
    Guid? VoidsEventId,
    string? Note,
    OperationsTrackingActor? Actor,
    Guid? ActorUserId);
