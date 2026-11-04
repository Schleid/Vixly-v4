using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Domain.Inspection;

// =========================================================
// FILE: OperationsInspectionOutcome.cs
//
// PURPOSE:
// What the inspector found and decided (Slice 3C design section 5).
//
// RESPONSIBILITIES:
// - Carry the result, the listed items found, notes, what the customer must do, the fee and the call before a disposal
//
// IMPORTANT NOTES:
// - Checked by OperationsInspection.Finish
// - Result is one of OperationsTrackingCodes.InspectionResults
// =========================================================
public sealed record OperationsInspectionOutcome(
    string Result,
    IReadOnlyList<string> ItemsFound,
    string? Notes,
    string? CustomerAction,
    OperationsHoldReasons HoldReason,
    decimal? Fee,
    DateTimeOffset? CalledCustomerUtc,
    string? CallNotes);
