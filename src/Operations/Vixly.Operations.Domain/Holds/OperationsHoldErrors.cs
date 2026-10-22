using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Holds;

// =========================================================
// FILE: OperationsHoldErrors.cs
//
// PURPOSE:
// Error codes of hold answers and unclaimed packages (Slice 3E).
//
// RESPONSIBILITIES:
// - Name every refusal
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsHoldErrors
{
    public static Error NotAnswerable { get; } = Error.Create("ops.holds.not_answerable");

    public static Error DocumentRequired { get; } = Error.Create("ops.holds.document_required");

    public static Error DocumentInvalid { get; } = Error.Create("ops.holds.document_invalid");

    public static Error ContentsRequired { get; } = Error.Create("ops.holds.contents_required");

    public static Error ValueInvalid { get; } = Error.Create("ops.holds.value_invalid");

    public static Error ChoiceInvalid { get; } = Error.Create("ops.holds.choice_invalid");

    public static Error AnswerNotFound { get; } = Error.Create("ops.holds.answer_not_found");

    public static Error AnswerNotOpen { get; } = Error.Create("ops.holds.answer_not_open");

    // The hold ended another way before the answer was reviewed (released by hand, the parcel left or was decided).
    public static Error AnswerStale { get; } = Error.Create("ops.holds.answer_stale");

    public static Error ReviewNoteRequired { get; } = Error.Create("ops.holds.review_note_required");

    public static Error TextInvalid { get; } = Error.Create("ops.holds.text_invalid");

    public static Error ApproverNotAllowed { get; } = Error.Create("ops.holds.approver_not_allowed");

    public static Error NotUnclaimed { get; } = Error.Create("ops.holds.not_unclaimed");

    public static Error DecisionInvalid { get; } = Error.Create("ops.holds.decision_invalid");

    public static Error DaysInvalid { get; } = Error.Create("ops.holds.days_invalid");

    // Slice 4D step 3: undeclared parcels.
    public static Error StorageFeeInvalid { get; } = Error.Create("ops.holds.storage_fee_invalid");

    public static Error LabelTextInvalid { get; } = Error.Create("ops.holds.label_text_invalid");

    public static Error CountNotFound { get; } = Error.Create("ops.holds.count_not_found");

    public static Error CountOpen { get; } = Error.Create("ops.holds.count_open");

    public static Error CountClosed { get; } = Error.Create("ops.holds.count_closed");

    public static Error CountIncomplete { get; } = Error.Create("ops.holds.count_incomplete");

    public static Error CountMissingNote { get; } = Error.Create("ops.holds.count_missing_note");
}
