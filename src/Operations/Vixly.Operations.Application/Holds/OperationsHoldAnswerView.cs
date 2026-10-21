using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Holds;

// =========================================================
// FILE: OperationsHoldAnswerView.cs
//
// PURPOSE:
// What a customer sees of their answer to a hold (Slice 3E).
//
// RESPONSIBILITIES:
// - Carry the hold, what was sent, where it stands and the note when declined
//
// IMPORTANT NOTES:
// - Never the staff member who reviewed it
// =========================================================
public sealed record OperationsHoldAnswerView(OperationsHoldReasons Reason, OperationsHoldAnswerKind Kind, OperationsHoldAnswerStatus Status, DateTimeOffset SentUtc, string? DeclinedBecause)
{
    public static OperationsHoldAnswerView Of(OperationsHoldAnswer answer) =>
        new(answer.Reason, answer.Kind, answer.Status, answer.CreatedUtc, answer.Status == OperationsHoldAnswerStatus.Declined ? answer.ReviewNote : null);
}
