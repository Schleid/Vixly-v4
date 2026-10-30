namespace Vixly.Operations.Application.Receiving;

// =========================================================
// FILE: OperationsIncomingFilter.cs
//
// PURPOSE:
// What the incoming packages list shows (Slice 4D, A-7).
//
// RESPONSIBILITIES:
// - Warehouses, view, the company's today, expected days, carrier and search
//
// IMPORTANT NOTES:
// - The warehouses are those the staff member may see
// =========================================================
public sealed record OperationsIncomingFilter(
    IReadOnlyCollection<Guid> Warehouses,
    OperationsIncomingView View,
    DateOnly Today,
    DateOnly? From,
    DateOnly? To,
    string? Carrier,
    string? Search);
