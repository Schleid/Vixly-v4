using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Holds.GetOperationsStockCount;

// =========================================================
// FILE: GetOperationsStockCountResponse.cs
//
// PURPOSE:
// One count of the undeclared parcels area, with what each parcel was found to be (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsStockCountResponse
{
    private GetOperationsStockCountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsStockCount? Count { get; private init; }

    public OperationsWarehouse? Warehouse { get; private init; }

    public bool CanClose { get; private init; }

    public bool IsManager { get; private init; }

    public static GetOperationsStockCountResponse Success(OperationsStockCount? count, OperationsWarehouse? warehouse, bool canClose, bool isManager)
    {
        return new GetOperationsStockCountResponse { Succeeded = true, Count = count, Warehouse = warehouse, CanClose = canClose, IsManager = isManager };
    }

    public static GetOperationsStockCountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsStockCountResponse { Error = error };
    }
}
