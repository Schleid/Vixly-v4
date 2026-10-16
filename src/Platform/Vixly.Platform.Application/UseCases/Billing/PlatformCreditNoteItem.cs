using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.UseCases.Billing;

// =========================================================
// FILE: PlatformCreditNoteItem.cs
//
// PURPOSE:
// One credit note as shown.
//
// RESPONSIBILITIES:
// - Carry what the console shows
//
// IMPORTANT NOTES:
// - Plain record for the console
// =========================================================
public sealed record PlatformCreditNoteItem(
    Guid CreditId,
    string? Number,
    decimal Amount,
    decimal TaxPart,
    bool IsVoid,
    string Reason,
    PlatformCreditNoteStatus Status,
    string? RequestedByName,
    DateTimeOffset RequestedUtc,
    string? DecidedByName,
    DateTimeOffset? DecidedUtc,
    string? DecisionReason,
    bool CanDecide);
