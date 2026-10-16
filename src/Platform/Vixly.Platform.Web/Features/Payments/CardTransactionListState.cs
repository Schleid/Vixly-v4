using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Payments;

// =========================================================
// FILE: CardTransactionListState.cs
//
// PURPOSE:
// State of the card transactions list.
//
// RESPONSIBILITIES:
// - Hold one page of card transactions
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record CardTransactionListState(IReadOnlyList<PlatformCardTransactionItem> Items, int Page, int TotalPages, int TotalCount, bool Loaded, Error? Error)
{
    public static CardTransactionListState Initial { get; } = new(Array.Empty<PlatformCardTransactionItem>(), 1, 0, 0, false, null);
}
