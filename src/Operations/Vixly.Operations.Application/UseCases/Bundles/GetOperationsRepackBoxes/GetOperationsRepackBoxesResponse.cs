using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.GetOperationsRepackBoxes;

// =========================================================
// FILE: GetOperationsRepackBoxesResponse.cs
//
// PURPOSE:
// The consolidated shipments waiting to travel and their repack boxes (Slice 6A, D-4 and D-10).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsRepackBoxesResponse
{
    private GetOperationsRepackBoxesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsRepackShipmentRow> Shipments { get; private init; } = Array.Empty<OperationsRepackShipmentRow>();

    public IReadOnlyList<OperationsWarehouse> Warehouses { get; private init; } = Array.Empty<OperationsWarehouse>();

    public IReadOnlyList<Guid> ManageWarehouses { get; private init; } = Array.Empty<Guid>();

    public OperationsConsolidationWay CompanyWay { get; private init; }

    // Where the person may start and fill a box (fill bundles).
    public IReadOnlyList<Guid> FillWarehouses { get; private init; } = Array.Empty<Guid>();

    public static GetOperationsRepackBoxesResponse Success(IReadOnlyList<OperationsRepackShipmentRow> shipments, IReadOnlyList<OperationsWarehouse> warehouses, IReadOnlyList<Guid> manageWarehouses, OperationsConsolidationWay companyWay,
        IReadOnlyList<Guid> fillWarehouses)
    {
        return new GetOperationsRepackBoxesResponse
        {
            Succeeded = true, Shipments = shipments, Warehouses = warehouses, ManageWarehouses = manageWarehouses, CompanyWay = companyWay, FillWarehouses = fillWarehouses
        };
    }

    public static GetOperationsRepackBoxesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsRepackBoxesResponse { Error = error };
    }
}
