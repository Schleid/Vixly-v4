namespace Vixly.Operations.Application.UseCases.Receiving.ListOperationsParcels;

// =========================================================
// FILE: ListOperationsParcelsRequest.cs
//
// PURPOSE:
// Lists the parcels a staff member may see (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListOperationsParcelsRequest
{
    public string? Search { get; init; }

    public Guid? WarehouseId { get; init; }

    public bool UnidentifiedOnly { get; init; }

    public bool HeldOnly { get; init; }

    public int Page { get; init; } = 1;
}
