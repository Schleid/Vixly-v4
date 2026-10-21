namespace Vixly.Operations.Domain.Tracking;

// =========================================================
// FILE: OperationsTrackingCodes.cs
//
// PURPOSE:
// The tracking event catalogue (ADR-0021 decision 7).
//
// RESPONSIBILITIES:
// - List every event code with its meaning
// - Find a code by name
//
// IMPORTANT NOTES:
// - Each slice adds its codes here; a name never changes once used
// - Customer-visible codes need customer wording in en, fr and es
// =========================================================
public static class OperationsTrackingCodes
{
    public const string PreAlerted = "ops.tracking.pre_alerted";

    public const string ReceivedAtOrigin = "ops.tracking.received_at_origin";

    public const string CustomerAssigned = "ops.tracking.customer_assigned";

    public const string ConditionRecorded = "ops.tracking.condition_recorded";

    public const string PhotoAdded = "ops.tracking.photo_added";

    public const string PutAway = "ops.tracking.put_away";

    // Weighed, measured and photographed at the inspection station (Slice 3B, owner 2026-10-18).
    public const string Checked = "ops.tracking.checked";

    public const string InspectionRequired = "ops.tracking.inspection_required";

    public const string InspectionStarted = "ops.tracking.inspection_started";

    public const string InspectionCompleted = "ops.tracking.inspection_completed";

    public const string HoldOpened = "ops.tracking.hold_opened";

    public const string HoldReleased = "ops.tracking.hold_released";

    public const string ReturnedToSender = "ops.tracking.returned_to_sender";

    public const string Disposed = "ops.tracking.disposed";

    // Slice 4D step 3: sold at auction after the company's days (Q-D4: recorded with a note; the company runs the auction).
    public const string Auctioned = "ops.tracking.auctioned";

    // Slice 4D step 3: staff recorded what the label of an undeclared package shows.
    public const string LabelRecorded = "ops.tracking.label_recorded";

    public const string Cancelled = "ops.tracking.cancelled";

    public const string EventVoided = "ops.tracking.event_voided";

    // Slice 5C: the shipment is final (all its parcels in, measured and clear); its amount is fixed.
    public const string Booked = "ops.tracking.booked";

    private const OperationsParcelFlags Inspection = OperationsParcelFlags.InspectionPending | OperationsParcelFlags.InspectionInProgress;

    // The results of an inspection (ADR-0022 outcomes).
    public static IReadOnlyList<string> InspectionResults { get; } = new[] { "passed", "passed_with_notes", "held", "repacked", "refused_returned", "refused_disposed" };

    // Why an inspection is required (ADR-0022): a tenant rule, the random share, or a staff flag.
    public static IReadOnlyList<string> InspectionReasons { get; } = new[] { "rule", "random", "flagged" };

    // The condition seen at the condition check (ADR-0022).
    public static IReadOnlyList<string> Conditions { get; } = new[] { "intact", "damaged", "wet", "opened", "contents_missing" };

    // The kinds of photo (design section 3).
    public static IReadOnlyList<string> PhotoKinds { get; } = new[] { "label", "package", "damage", "contents" };

    // Why an event was voided (ADR-0021 decision 2: a void names its reason).
    // What the customer gave to prove an unidentified package is theirs (owner, Q-R1).
    // Slice 4D step 3: or the customer's mailbox code written on the label, checked by staff.
    public static IReadOnlyList<string> VerificationMethods { get; } = new[] { "tracking_number", "sender", "order_number", "label_code" };

    public static IReadOnlyList<string> VoidReasons { get; } = new[] { "entered_in_error", "wrong_parcel", "wrong_step", "duplicate", "other" };

    public static IReadOnlyList<string> HoldReasonNames { get; } = Enum.GetNames<OperationsHoldReasons>().Where(name => name != nameof(OperationsHoldReasons.None)).ToArray();

    public static IReadOnlyList<OperationsTrackingCode> All { get; } = new[]
    {
        Code(PreAlerted, OperationsTrackingVisibility.Customer, OperationsMilestone.PreAlerted),
        Code(ReceivedAtOrigin, OperationsTrackingVisibility.Customer, OperationsMilestone.ReceivedAtOrigin, sets: OperationsParcelFlags.AwaitingCheck, arguments: new[] { "warehouse" }) with { CustomerArguments = new[] { "warehouse" } },
        Code(Checked, OperationsTrackingVisibility.Internal, null, clears: OperationsParcelFlags.AwaitingCheck, arguments: new[] { "weight", "size" }),
        Code(CustomerAssigned, OperationsTrackingVisibility.Internal, null, arguments: new[] { "customer", "verified_by" }) with { Choices = Choice("verified_by", VerificationMethods) },
        Code(ConditionRecorded, OperationsTrackingVisibility.Internal, null, arguments: new[] { "condition" }) with { Choices = Choice("condition", Conditions) },
        Code(PhotoAdded, OperationsTrackingVisibility.Internal, null, arguments: new[] { "kind" }) with { Choices = Choice("kind", PhotoKinds) },
        Code(PutAway, OperationsTrackingVisibility.Internal, null, arguments: new[] { "location" }),
        Code(LabelRecorded, OperationsTrackingVisibility.Internal, null),
        Code(InspectionRequired, OperationsTrackingVisibility.Internal, null, sets: OperationsParcelFlags.InspectionPending, arguments: new[] { "why" }) with { Choices = Choice("why", InspectionReasons) },
        Code(InspectionStarted, OperationsTrackingVisibility.Internal, null, sets: OperationsParcelFlags.InspectionInProgress, clears: OperationsParcelFlags.InspectionPending),
        Code(InspectionCompleted, OperationsTrackingVisibility.Customer, null, clears: Inspection, arguments: new[] { "result" }) with { Choices = Choice("result", InspectionResults), CustomerArguments = new[] { "result" } },
        Code(HoldOpened, OperationsTrackingVisibility.Customer, null, arguments: new[] { OperationsTrackingCode.ReasonArgument }) with { Choices = Choice(OperationsTrackingCode.ReasonArgument, HoldReasonNames), CustomerArguments = new[] { OperationsTrackingCode.ReasonArgument } },
        Code(HoldReleased, OperationsTrackingVisibility.Customer, null, arguments: new[] { OperationsTrackingCode.ReasonArgument }) with { Choices = Choice(OperationsTrackingCode.ReasonArgument, HoldReasonNames), CustomerArguments = new[] { OperationsTrackingCode.ReasonArgument } },
        Code(ReturnedToSender, OperationsTrackingVisibility.Customer, OperationsMilestone.ReturnedToSender),
        Code(Disposed, OperationsTrackingVisibility.Customer, OperationsMilestone.Disposed),
        Code(Auctioned, OperationsTrackingVisibility.Customer, OperationsMilestone.Disposed),
        Code(Cancelled, OperationsTrackingVisibility.Customer, OperationsMilestone.Cancelled),
        Code(Booked, OperationsTrackingVisibility.Customer, OperationsMilestone.Booked),
        Code(EventVoided, OperationsTrackingVisibility.Internal, null, arguments: new[] { "why" }) with { Choices = Choice("why", VoidReasons) }
    };

    private static readonly Dictionary<string, OperationsTrackingCode> ByName = All.ToDictionary(code => code.Name, StringComparer.Ordinal);

    public static OperationsTrackingCode? Find(string? name) => name is not null && ByName.TryGetValue(name, out OperationsTrackingCode? code) ? code : null;

    private static Dictionary<string, IReadOnlyList<string>> Choice(string argument, IReadOnlyList<string> values) =>
        new(StringComparer.Ordinal) { [argument] = values };

    private static OperationsTrackingCode Code(string name, OperationsTrackingVisibility visibility, OperationsMilestone? milestone,
        OperationsParcelFlags sets = OperationsParcelFlags.None, OperationsParcelFlags clears = OperationsParcelFlags.None, string[]? arguments = null) =>
        new(name, visibility, milestone, sets, clears, arguments ?? Array.Empty<string>());
}
