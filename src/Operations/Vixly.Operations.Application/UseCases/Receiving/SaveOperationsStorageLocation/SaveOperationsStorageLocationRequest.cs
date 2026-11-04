using Vixly.Operations.Domain.Receiving;

namespace Vixly.Operations.Application.UseCases.Receiving.SaveOperationsStorageLocation;

// =========================================================
// FILE: SaveOperationsStorageLocationRequest.cs
//
// PURPOSE:
// Adds a shelf to a warehouse, or changes its zone or whether it is in use (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsStorageLocationRequest
{
    public Guid WarehouseId { get; init; }

    public Guid? LocationId { get; init; }

    public string? Code { get; init; }

    public OperationsStorageZone Zone { get; init; } = OperationsStorageZone.Storage;

    public bool Active { get; init; } = true;
}
