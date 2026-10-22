namespace Vixly.Operations.Domain.Tracking;

// =========================================================
// FILE: OperationsTrackingDraft.cs
//
// PURPOSE:
// The facts of one tracking event before it is recorded (ADR-0021 decision 1).
//
// RESPONSIBILITIES:
// - Carry what, when, where, who, how and why
//
// IMPORTANT NOTES:
// - Validated by OperationsTrackingEvent.Record; the idempotency key makes a repeat harmless (decision 4)
// =========================================================
public sealed record OperationsTrackingDraft(
    OperationsTrackingSubject SubjectKind,
    Guid SubjectId,
    string Code,
    IReadOnlyDictionary<string, string> Arguments,
    DateTimeOffset OccurredUtc,
    string IdempotencyKey,
    OperationsTrackingActor Actor,
    OperationsTrackingSource Source)
{
    public string? TimeZone { get; init; }

    public string? LocationCode { get; init; }

    public Guid? ActorUserId { get; init; }

    public Guid? DeviceId { get; init; }

    public string? Note { get; init; }

    // Slice 6C: the source event of a copy on a parcel.
    public Guid? CausedByEventId { get; init; }

    public Guid? VoidsEventId { get; init; }
}
