namespace Vixly.Operations.Domain.Tracking;

// =========================================================
// FILE: OperationsParcelState.cs
//
// PURPOSE:
// Where a package stands, as its events say (ADR-0018 decision 4).
//
// RESPONSIBILITIES:
// - Carry the milestone, the flags and the open hold reasons
//
// IMPORTANT NOTES:
// - Computed by OperationsTrackingProjection; never set by hand
// =========================================================
public sealed record OperationsParcelState(OperationsMilestone? Milestone, OperationsParcelFlags Flags, OperationsHoldReasons HoldReasons, DateTimeOffset? LastOccurredUtc)
{
    public static OperationsParcelState Empty { get; } = new(null, OperationsParcelFlags.None, OperationsHoldReasons.None, null);

    // A hold or an inspection not yet finished blocks bundling, dispatch and handover (ADR-0022).
    public bool IsBlocked => (Flags & (OperationsParcelFlags.OnHold | OperationsParcelFlags.InspectionPending | OperationsParcelFlags.InspectionInProgress)) != 0;
}
