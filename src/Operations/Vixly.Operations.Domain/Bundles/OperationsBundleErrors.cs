using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Bundles;

// =========================================================
// FILE: OperationsBundleErrors.cs
//
// PURPOSE:
// Error codes of bundles and repack boxes (Slice 6A).
//
// RESPONSIBILITIES:
// - Name every refusal; a scan refusal says why
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsBundleErrors
{
    public static Error NotFound { get; } = Error.Create("ops.bundles.not_found");

    public static Error DestinationRequired { get; } = Error.Create("ops.bundles.destination_required");

    public static Error ServiceRequired { get; } = Error.Create("ops.bundles.service_required");

    public static Error TypeInvalid { get; } = Error.Create("ops.bundles.type_invalid");

    public static Error TargetInvalid { get; } = Error.Create("ops.bundles.target_invalid");

    public static Error DivisorRequired { get; } = Error.Create("ops.bundles.divisor_required");

    public static Error NotOpen { get; } = Error.Create("ops.bundles.not_open");

    public static Error Empty { get; } = Error.Create("ops.bundles.empty");

    public static Error Full { get; } = Error.Create("ops.bundles.full");

    public static Error MeasurementsInvalid { get; } = Error.Create("ops.bundles.measurements_invalid");

    public static Error ReasonRequired { get; } = Error.Create("ops.bundles.reason_required");

    public static Error CannotVoid { get; } = Error.Create("ops.bundles.cannot_void");

    public static Error TargetReached { get; } = Error.Create("ops.bundles.target_reached");

    public static Error UnknownCode { get; } = Error.Create("ops.bundles.unknown_code");

    public static Error WrongWarehouse { get; } = Error.Create("ops.bundles.wrong_warehouse");

    public static Error WrongDestination { get; } = Error.Create("ops.bundles.wrong_destination");

    public static Error WrongService { get; } = Error.Create("ops.bundles.wrong_service");

    public static Error NotFinal { get; } = Error.Create("ops.bundles.not_final");

    public static Error NotPaid { get; } = Error.Create("ops.bundles.not_paid");

    // The bundle's list (owner, 2026-10-21): only what is on it goes in; it closes once everything on it is scanned.
    public static Error NotOnList { get; } = Error.Create("ops.bundles.not_on_list");

    public static Error OnAnotherList { get; } = Error.Create("ops.bundles.on_another_list");

    public static Error ItemsMissing { get; } = Error.Create("ops.bundles.items_missing");

    public static Error ItemScanned { get; } = Error.Create("ops.bundles.item_scanned");

    public static Error NothingChosen { get; } = Error.Create("ops.bundles.nothing_chosen");

    public static Error NotAvailable { get; } = Error.Create("ops.bundles.not_available");

    public static Error Blocked { get; } = Error.Create("ops.bundles.blocked");

    public static Error InAnotherBundle { get; } = Error.Create("ops.bundles.in_another_bundle");

    public static Error RepackFirst { get; } = Error.Create("ops.bundles.repack_first");

    public static Error KeptTogether { get; } = Error.Create("ops.bundles.kept_together");

    public static Error NotInBundle { get; } = Error.Create("ops.bundles.not_in_bundle");

    public static Error SettingsInvalid { get; } = Error.Create("ops.bundles.settings_invalid");

    public static Error NotConsolidated { get; } = Error.Create("ops.bundles.not_consolidated");

    public static Error WayLocked { get; } = Error.Create("ops.bundles.way_locked");

    public static Error NotRepacked { get; } = Error.Create("ops.bundles.not_repacked");

    public static Error BoxNotOpen { get; } = Error.Create("ops.bundles.box_not_open");

    public static Error BoxNotClosed { get; } = Error.Create("ops.bundles.box_not_closed");

    public static Error BoxIncomplete { get; } = Error.Create("ops.bundles.box_incomplete");

    public static Error WrongShipment { get; } = Error.Create("ops.bundles.wrong_shipment");

    public static Error InAnotherBox { get; } = Error.Create("ops.bundles.in_another_box");

    public static Error BoxOpenAlready { get; } = Error.Create("ops.bundles.box_open_already");

    public static Error KeptTogetherIncomplete { get; } = Error.Create("ops.bundles.kept_together_incomplete");
}
