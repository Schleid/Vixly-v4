using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Domain.Inspection;

// =========================================================
// FILE: OperationsInspectionErrors.cs
//
// PURPOSE:
// Error codes of compliance inspection and holds (Slice 3C).
//
// RESPONSIBILITIES:
// - Name every refusal about the restricted list, the rules, inspections and holds
//
// IMPORTANT NOTES:
// - Codes, never text (L-1)
// =========================================================
public static class OperationsInspectionErrors
{
    public static Error NameRequired { get; } = Error.Create("ops.inspection.name_required");

    public static Error WordsRequired { get; } = Error.Create("ops.inspection.words_required");

    public static Error TextInvalid { get; } = Error.Create("ops.inspection.text_invalid");

    public static Error CountryInvalid { get; } = Error.Create("ops.inspection.country_invalid");

    public static Error RuleValueInvalid { get; } = Error.Create("ops.inspection.rule_value_invalid");

    public static Error RuleKindInvalid { get; } = Error.Create("ops.inspection.rule_kind_invalid");

    public static Error ItemNotFound { get; } = Error.Create("ops.inspection.item_not_found");

    public static Error RuleNotFound { get; } = Error.Create("ops.inspection.rule_not_found");

    public static Error TooMany { get; } = Error.Create("ops.inspection.too_many");

    public static Error ShareInvalid { get; } = Error.Create("ops.inspection.share_invalid");

    public static Error NotFound { get; } = Error.Create("ops.inspection.not_found");

    public static Error NotWaiting { get; } = Error.Create("ops.inspection.not_waiting");

    public static Error NotInProgress { get; } = Error.Create("ops.inspection.not_in_progress");

    public static Error ResultInvalid { get; } = Error.Create("ops.inspection.result_invalid");

    public static Error HoldReasonInvalid { get; } = Error.Create("ops.inspection.hold_reason_invalid");

    public static Error CustomerActionRequired { get; } = Error.Create("ops.inspection.customer_action_required");

    public static Error FeeInvalid { get; } = Error.Create("ops.inspection.fee_invalid");

    public static Error NotesRequired { get; } = Error.Create("ops.inspection.notes_required");

    public static Error CallRequired { get; } = Error.Create("ops.inspection.call_required");

    public static Error NotAwaitingApproval { get; } = Error.Create("ops.inspection.not_awaiting_approval");

    public static Error ApproverNotAllowed { get; } = Error.Create("ops.inspection.approver_not_allowed");

    public static Error SameApprover { get; } = Error.Create("ops.inspection.same_approver");

    public static Error FlagReasonRequired { get; } = Error.Create("ops.inspection.flag_reason_required");

    public static Error HoldNotReleasable { get; } = Error.Create("ops.inspection.hold_not_releasable");

    public static Error ReleaseNoteRequired { get; } = Error.Create("ops.inspection.release_note_required");

    public static Error ListNotEmpty { get; } = Error.Create("ops.inspection.list_not_empty");

    public static Error ParcelNotHere { get; } = Error.Create("ops.inspection.parcel_not_here");

    public static Error ItemDuplicate { get; } = Error.Create("ops.inspection.item_duplicate");

    public static Error AlreadyChosen { get; } = Error.Create("ops.inspection.already_chosen");
}
