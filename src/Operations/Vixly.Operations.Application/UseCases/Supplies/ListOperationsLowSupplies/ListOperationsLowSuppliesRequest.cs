namespace Vixly.Operations.Application.UseCases.Supplies.ListOperationsLowSupplies;

// =========================================================
// FILE: ListOperationsLowSuppliesRequest.cs
//
// PURPOSE:
// The supplies at or below their reorder level in the warehouses a person keeps the stock of (Slice 5E, S-8), for the staff home page.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsLowSuppliesRequest
{
    // The reader's language for the supply names.
    public string? Language { get; init; }
}
