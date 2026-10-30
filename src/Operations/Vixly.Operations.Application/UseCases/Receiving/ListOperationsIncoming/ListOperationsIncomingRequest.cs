using Vixly.Operations.Application.Receiving;

namespace Vixly.Operations.Application.UseCases.Receiving.ListOperationsIncoming;

// =========================================================
// FILE: ListOperationsIncomingRequest.cs
//
// PURPOSE:
// The incoming packages list: every pre-alert of the warehouses the staff member may see, filtered (Slice 4D, A-7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsIncomingRequest
{
    public Guid? WarehouseId { get; init; }

    public OperationsIncomingView View { get; init; }

    public DateOnly? From { get; init; }

    public DateOnly? To { get; init; }

    public string? Carrier { get; init; }

    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    // All rows for the CSV export (up to MaxExport), not one page.
    public bool Export { get; init; }
}
