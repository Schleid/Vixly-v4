namespace Vixly.Operations.Domain.Tracking;

// =========================================================
// FILE: OperationsTrackingCode.cs
//
// PURPOSE:
// One entry of the tracking event catalogue (ADR-0021 decision 7).
//
// RESPONSIBILITIES:
// - Say what an event means: visibility, the milestone it reaches, the flags and hold reasons it sets or clears, its arguments
//
// IMPORTANT NOTES:
// - Defined in code (OperationsTrackingCodes); staff texts are ui.tracking.{short name} in OperationsText, customer texts the same keys in CustomerText
// =========================================================
public sealed record OperationsTrackingCode(
    string Name,
    OperationsTrackingVisibility Visibility,
    OperationsMilestone? Milestone,
    OperationsParcelFlags Sets,
    OperationsParcelFlags Clears,
    IReadOnlyList<string> Arguments)
{
    // Arguments limited to a fixed list of values (never free text a customer might see).
    public IReadOnlyDictionary<string, IReadOnlyList<string>> Choices { get; init; } = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

    // Arguments a customer may see on their own history; every other argument is staff-only (review M1).
    public IReadOnlyList<string> CustomerArguments { get; init; } = Array.Empty<string>();

    // Slice 7 step 3: a milestone a parcel steps back to (a failed delivery: back at the warehouse), unless finished.
    public bool Rewinds { get; init; }

    // Slice 6C: arguments that may be left out.
    public IReadOnlyList<string> OptionalArguments { get; init; } = Array.Empty<string>();

    // The argument that names a hold reason (OperationsHoldReasons), for hold_opened and hold_released.
    public const string ReasonArgument = "reason";

    public bool OpensHold => Name == OperationsTrackingCodes.HoldOpened;

    public bool ReleasesHold => Name == OperationsTrackingCodes.HoldReleased;

    public bool Voids => Name == OperationsTrackingCodes.EventVoided;
}
