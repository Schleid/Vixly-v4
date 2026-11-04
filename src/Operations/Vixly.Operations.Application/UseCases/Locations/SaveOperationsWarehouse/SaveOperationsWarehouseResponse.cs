using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Locations.SaveOperationsWarehouse;

// =========================================================
// FILE: SaveOperationsWarehouseResponse.cs
//
// PURPOSE:
// Creates or changes a warehouse (Slice 1B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsWarehouseResponse
{
    private SaveOperationsWarehouseResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid WarehouseId { get; private init; }

    public static SaveOperationsWarehouseResponse Success(Guid warehouseId)
    {
        return new SaveOperationsWarehouseResponse { Succeeded = true, WarehouseId = warehouseId };
    }

    public static SaveOperationsWarehouseResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsWarehouseResponse { Error = error };
    }
}
