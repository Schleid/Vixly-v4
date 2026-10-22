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

    // Slice 6A: a parcel put in a bundle (customer: packed for departure; the bundle is internal), or in a consolidated shipment's repack box.
    public const string Packed = "ops.tracking.packed";

    public const string Boxed = "ops.tracking.boxed";

    // Slice 6A: on the bundle itself (ADR-0021: the company sees them all).
    public const string BundleCreated = "ops.tracking.bundle_created";

    public const string BundleClosed = "ops.tracking.bundle_closed";

    public const string BundleVoided = "ops.tracking.bundle_voided";

    // Slice 6B (D-5): each parcel follows its dispatch; the customer sees departed, in transit and arrived in the destination country.
    public const string Manifested = "ops.tracking.manifested";

    public const string DepartedOrigin = "ops.tracking.departed_origin";

    public const string InTransit = "ops.tracking.in_transit";

    public const string ArrivedDestinationCountry = "ops.tracking.arrived_destination_country";

    // On the dispatch itself.
    public const string DispatchClosed = "ops.tracking.dispatch_closed";

    public const string DispatchDeparted = "ops.tracking.dispatch_departed";

    public const string DispatchArrived = "ops.tracking.dispatch_arrived";

    public const string DispatchCancelled = "ops.tracking.dispatch_cancelled";

    // Slice 6C (E-1, E-2): recorded by hand. A note is internal; a delay or an exception shows the customer its cause and an optional line.
    public const string Note = "ops.tracking.note";

    public const string Delayed = "ops.tracking.delayed";

    public const string ExceptionRaised = "ops.tracking.exception";

    // The causes' wording: a standard cause is worded by ui.event_reason.<cause>; a company's own carries its words in each language.
    public static IReadOnlyList<string> CauseArguments { get; } = new[] { "label_en", "label_fr", "label_es", "message", "source" };

    public static IReadOnlyList<string> ManualCodes { get; } = new[] { Note, Delayed, ExceptionRaised };

    // Slice 7 (R-1, R-3): at the destination.
    public const string ArrivedDestinationWarehouse = "ops.tracking.arrived_destination_warehouse";

    public const string InCustoms = "ops.tracking.in_customs";

    public const string CustomsCleared = "ops.tracking.customs_cleared";

    public const string CustomsHeld = "ops.tracking.customs_held";

    public const string ReadyForPickup = "ops.tracking.ready_for_pickup";

    // Slice 7 step 3 (R-7, R-12 to R-15): home delivery on a route.
    public const string OutForDelivery = "ops.tracking.out_for_delivery";

    public const string Delivered = "ops.tracking.delivered";

    public const string DeliveryFailed = "ops.tracking.delivery_failed";

    public const string DeliveryReturning = "ops.tracking.delivery_returning";

    // The reasons a delivery fails (a fixed list: the customer sees it).
    public static IReadOnlyList<string> DeliveryFailures { get; } = new[] { "not_home", "refused", "address_wrong", "no_access", "not_paid", "other" };

    // Slice 7 step 2 (R-5, R-9): collected at the pickup counter, after payment and with the company's proof.
    public const string PickedUp = "ops.tracking.picked_up";

    public const string BundleArrived = "ops.tracking.bundle_arrived";

    public const string BundleOpened = "ops.tracking.bundle_opened";

    public const string DispatchReceived = "ops.tracking.dispatch_received";

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

    // Slice 6A: a parcel taken out of a bundle or a box, or freed when its bundle or box is voided.
    public static IReadOnlyList<string> VoidReasons { get; } = new[] { "entered_in_error", "wrong_parcel", "wrong_step", "duplicate", "other", "taken_out", "bundle_voided", "box_voided", "dispatch_cancelled" };

    // Slice 6B: how a parcel left its origin (the customer sees by air, by sea or by road).
    public static IReadOnlyList<string> Modes { get; } = new[] { "air", "ocean", "ground" };

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
        Code(ReturnedToSender, OperationsTrackingVisibility.Customer, OperationsMilestone.ReturnedToSender, clears: OperationsParcelFlags.ReturnPending),
        Code(Disposed, OperationsTrackingVisibility.Customer, OperationsMilestone.Disposed),
        Code(Auctioned, OperationsTrackingVisibility.Customer, OperationsMilestone.Disposed),
        Code(Cancelled, OperationsTrackingVisibility.Customer, OperationsMilestone.Cancelled),
        Code(Booked, OperationsTrackingVisibility.Customer, OperationsMilestone.Booked),
        Code(Packed, OperationsTrackingVisibility.Customer, OperationsMilestone.Consolidated, arguments: new[] { "bundle" }),
        Code(Boxed, OperationsTrackingVisibility.Internal, null, arguments: new[] { "box" }),
        Code(BundleCreated, OperationsTrackingVisibility.Internal, null),
        Code(BundleClosed, OperationsTrackingVisibility.Internal, null, arguments: new[] { "weight", "size" }),
        Code(BundleVoided, OperationsTrackingVisibility.Internal, null),
        Code(Manifested, OperationsTrackingVisibility.Customer, OperationsMilestone.Manifested, arguments: new[] { "dispatch" }) with { CustomerArguments = Array.Empty<string>() },
        Code(DepartedOrigin, OperationsTrackingVisibility.Customer, OperationsMilestone.DepartedOrigin, arguments: new[] { "mode" }) with { Choices = Choice("mode", Modes), CustomerArguments = new[] { "mode" } },
        Code(InTransit, OperationsTrackingVisibility.Customer, OperationsMilestone.InTransit, arguments: new[] { "place" }) with { CustomerArguments = new[] { "place" } },
        Code(ArrivedDestinationCountry, OperationsTrackingVisibility.Customer, OperationsMilestone.ArrivedDestinationCountry, arguments: new[] { "country" }) with { CustomerArguments = new[] { "country" } },
        Code(DispatchClosed, OperationsTrackingVisibility.Internal, null),
        Code(DispatchDeparted, OperationsTrackingVisibility.Internal, null, arguments: new[] { "leg" }),
        Code(DispatchArrived, OperationsTrackingVisibility.Internal, null, arguments: new[] { "leg" }),
        Code(DispatchCancelled, OperationsTrackingVisibility.Internal, null),
        Code(Note, OperationsTrackingVisibility.Internal, null),
        Code(ArrivedDestinationWarehouse, OperationsTrackingVisibility.Customer, OperationsMilestone.ArrivedDestinationWarehouse, arguments: new[] { "place" }) with { CustomerArguments = new[] { "place" } },
        Code(InCustoms, OperationsTrackingVisibility.Customer, OperationsMilestone.InCustoms),
        Code(CustomsCleared, OperationsTrackingVisibility.Customer, OperationsMilestone.CustomsCleared, clears: OperationsParcelFlags.CustomsHeld),
        Code(CustomsHeld, OperationsTrackingVisibility.Customer, null, sets: OperationsParcelFlags.CustomsHeld) with { OptionalArguments = new[] { "message" }, CustomerArguments = new[] { "message" } },
        Code(ReadyForPickup, OperationsTrackingVisibility.Customer, OperationsMilestone.ReadyForPickup, arguments: new[] { "place" }) with { CustomerArguments = new[] { "place" } },
        Code(PickedUp, OperationsTrackingVisibility.Customer, OperationsMilestone.Delivered, arguments: new[] { "place" }) with { CustomerArguments = new[] { "place" } },
        Code(OutForDelivery, OperationsTrackingVisibility.Customer, OperationsMilestone.OutForDelivery),
        Code(Delivered, OperationsTrackingVisibility.Customer, OperationsMilestone.Delivered),
        Code(DeliveryFailed, OperationsTrackingVisibility.Customer, OperationsMilestone.ArrivedDestinationWarehouse, arguments: new[] { "failure" }) with
        {
            Rewinds = true, CustomerArguments = new[] { "failure" }, Choices = Choice("failure", DeliveryFailures)
        },
        Code(DeliveryReturning, OperationsTrackingVisibility.Customer, null, sets: OperationsParcelFlags.ReturnPending),
        Code(BundleArrived, OperationsTrackingVisibility.Internal, null, arguments: new[] { "warehouse" }),
        Code(BundleOpened, OperationsTrackingVisibility.Internal, null, arguments: new[] { "warehouse" }),
        Code(DispatchReceived, OperationsTrackingVisibility.Internal, null, arguments: new[] { "warehouse" }),
        Code(Delayed, OperationsTrackingVisibility.Customer, null, arguments: new[] { "cause" }) with
            { OptionalArguments = CauseArguments, CustomerArguments = new[] { "cause", "label_en", "label_fr", "label_es", "message" } },
        Code(ExceptionRaised, OperationsTrackingVisibility.Customer, null, arguments: new[] { "cause" }) with
            { OptionalArguments = CauseArguments, CustomerArguments = new[] { "cause", "label_en", "label_fr", "label_es", "message" } },
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
