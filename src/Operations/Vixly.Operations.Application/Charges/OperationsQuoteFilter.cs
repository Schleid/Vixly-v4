namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsQuoteFilter.cs
//
// PURPOSE:
// What the quotes list shows: one state or all, and the quotes of some customers or with a number like the one typed.
//
// RESPONSIBILITIES:
// - Carry the list's filter to the repository
//
// IMPORTANT NOTES:
// - Customers are null when nothing was typed; an empty list with a number finds by number only
// =========================================================
public sealed record OperationsQuoteFilter(OperationsQuoteState? State, string? Number, IReadOnlyCollection<Guid>? CustomerIds);
